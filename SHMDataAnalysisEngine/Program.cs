using influxConnect.Calculation;
using influxConnect;
using InfluxDB.Client.Api.Domain;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.IO.Compression;
using System.IO.Enumeration;
using System.Linq;
using System.Numerics;
using System.Reactive;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using String = System.String;

Matrix<double> govModeShape = DenseMatrix.OfArray(new double[,]
                                                {{32.3, -25.16, 3.78},
                                                {30.5, 22.69, -5.1},
                                                {28.0, -15.24, 0.27},
                                                {27.7, 14.49, 0.79},
                                                {25.8, -15.26, 3.15},
                                                {27.8, 15.68, -4.32}});

Matrix<double> locMatrix = DenseMatrix.OfArray(new double[,]
                                              {{-23.3,-8.23,-31.87},
                                              {-23.81,-8.44,30.39},
                                              {-23.18,-9.76,-31.65},
                                              {15.43,-9.72,14.45},
                                              {21.34,-12.37,18.48},
                                              {27.05,-13.8,-12.8}});

Matrix<double> periMatrix = DenseMatrix.OfArray(new double[,]
                                                { {1.22173*Math.Pow(10,08)  , 2.72283*Math.Pow(10,09) ,6.14388*Math.Pow(10,08) },
                                                {-1.98405*Math.Pow(10,08) ,-8.19779*Math.Pow(10,09) ,-2.3343*Math.Pow(10,09)} ,
                                                {-7.33602*Math.Pow(10,07) ,2.3651*Math.Pow(10,09)   ,1.2708*Math.Pow(10,09)}  ,
                                                {-1.43419*Math.Pow(10,07) ,-2.15911*Math.Pow(10,09) ,-5.30439*Math.Pow(10,08)},
                                                { 1.0271*Math.Pow(10,08)   ,5.35747*Math.Pow(10,09)  ,1.56445*Math.Pow(10,09)} ,
                                                { 6.09484*Math.Pow(10,07)  ,-1.90278*Math.Pow(10,09) ,-1.09243*Math.Pow(10,09)},
                                                {  -6.84213*Math.Pow(10,06),2.93805*Math.Pow(10,09)  ,7.45167*Math.Pow(10,08 )}});

string addressTB = "http://18.134.227.139:8086";
string secretKeyTB = "Fv1ObDbjnQ1SragMGlnxnx3O2rnjrpehKxxHCTMrTvAlQ76DK5k9POjhd_3jciV8K89fz564jcfcpX3XXV4tvQ==";
string orgTB = "SMS";
string bucketTB = "SMS_Data";
string sensorsTB = "";

DateTime EpochToTimestamp (double epochTimestamp)
{
    DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0,0, DateTimeKind.Utc).AddMilliseconds(epochTimestamp);
    //Console.WriteLine(dateTime); // Output: 7/22/2021 12:00:00 AM
    return dateTime;
}

List<double> findTurningPoints(List<double> data)
{
    for (int i=1; i < data.Count()-1;i++)
    {
        if (!(data.ElementAt(i - 1) < data.ElementAt(i) && data.ElementAt(i + 1) < data.ElementAt(i)) ||
            !(data.ElementAt(i - 1) > data.ElementAt(i) && data.ElementAt(i + 1) > data.ElementAt(i)))
        {
            data.RemoveAt(i);
        }
    }

    return data;
}

List<double> removeDuplicates(List<Matrix<double>> data, int row)
{
    List<double> dataOut = new List<double>();

    for (int num = 0; num < data.Count()-1; num++)
    if (data.ElementAt(num)[row,0] != data.ElementAt(num + 1)[row,0])
    {
            dataOut.Add(data.ElementAt(num)[row, 0]);
    }

    return dataOut;
}

LoadAndCycle LoadAndCycleCount(List<double> data)
{

    LoadAndCycle Load;

    Load.CycleRange = new List<double>();
    Load.CycleCount = 0;

    Matrix<double> PlotMatrix = DenseMatrix.Create(1,data.Count(),0);

    for(int i =0;i<data.Count();i++)
    {
        PlotMatrix[0, i] = data.ElementAt(i);
    }

    //PlotData(PlotMatrix, "Pre-count plot");

    //Convert to stress now or after for the SN Curve?

    int j = 0;

    while (true)
    {
        try
        {

            if ((data.Count <= 3) || ((j + 2) >= data.Count))
            {
                Load.CycleRange.Add(Math.Abs(data.ElementAt(0) - data[data.Count() - 1]));
                break;
            }

            if (Math.Abs(data.ElementAt(j + 2) - data.ElementAt(j + 1)) >= Math.Abs(data.ElementAt(j) - data.ElementAt(j + 1)))
            {
                Load.CycleRange.Add(Math.Abs(data.ElementAt(j + 1) - data.ElementAt(j)));
                data.RemoveAt(j + 1);
                data.RemoveAt(j);

                Load.CycleCount += 1;

                if (j - 2 < 0)
                {
                    j = 0;
                }
                else
                {
                    j = j - 2;
                }
            }
            else
            {
                j++;
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine("Rainflow error" + ex.Message + " " + ex.StackTrace);
            Console.WriteLine("Counts" + j + " Array Count" + Load.CycleRange.Count());
            break;
        }

    }

    return Load;

}

List<double> AbsoluteRearrange(List<double> data)
{
    double abs = 0.0;
    int cutpoint = 0;
    List<double> cut = new List<double>();
    List<double> join = new List<double>();

    if (Math.Abs(data.Max()) > Math.Abs(data.Min()))
    {
        abs = Math.Abs(data.Max());
        cutpoint = data.IndexOf(data.Max());
    }
    else
    {
        abs = Math.Abs(data.Min());
        cutpoint = data.IndexOf(data.Min());
    }

    //slice and rearrange the list
    for (int i = 0; i < cutpoint + 1; i++)
    {
        cut.Add(data.ElementAt(i));
    }
    for (int i = cutpoint; i < data.Count(); i++)
    {
        join.Add(data.ElementAt(i));
    }
    join.AddRange(cut);

    return join;
}

double FindFirstTimestamp(IEnumerable<string> filename)
{
    for (int i = 0; i < filename.Count(); i++)
    {
        string testElement = filename.ElementAt(i).Split(',')[0];

        bool digitCheck = (testElement.All(char.IsDigit));
        double numOut;
        if (digitCheck)
        {
            //Console.WriteLine(testElement + " is numeric");
            double.TryParse(testElement, out numOut); ;
            return numOut;
        }
    }
    return 0;
}

string FindMasterFile(DateTime targetDate, IEnumerable<FileInfo> folderFiles)
{
    //find the master file for a single day

    string masterFormat = String.Format("compiledData{0}.csv",targetDate.ToString("ddMMyy"));
    string masterFormatPath = Directory.GetParent(folderFiles.ElementAt(0).FullName) + "\\" + masterFormat;
    foreach (FileInfo file in folderFiles)
    {
        if (file.Name.Contains(masterFormat))
        {
            Console.WriteLine(masterFormat + " file found in directory");
            return masterFormatPath;
        }
    }

    double firstTime = FindFirstTimestamp(System.IO.File.ReadLines(masterFormatPath));
    DateTime dt = EpochToTimestamp(firstTime);

    Console.WriteLine(masterFormat + " not found, creating at " + masterFormatPath);
    System.IO.File.Create(masterFormatPath);
    //Thread.Sleep(50);
    return masterFormatPath;
}

SortedList<int, string> getSensorsAndIndices(FileInfo dataFile)
{
    SortedList<int, string> indexSensor = new SortedList<int, string>();

    IEnumerable<string> rFile = System.IO.File.ReadLines(dataFile.FullName);

    if (rFile.Count() == 0) { return indexSensor; }

    int counter = 0;

    //indexSensor.Add(, rFile.Where(rFile => rFile.Contains("Sensor Name")));



    foreach (string line in rFile)
    {      

        if (line.Contains("Sensor Name"))
        {
            indexSensor.Add(counter, line.Split(":")[1]);
        }

        counter++;
    }


    //Console.WriteLine(indexSensor.ElementAt(0));

    return indexSensor;
}
static void PrintDataTable(DataTable table)
{
    foreach (DataColumn column in table.Columns)
    {
        Console.Write($"{column.ColumnName}\t");
    }
    Console.WriteLine();

    foreach (DataRow row in table.Rows)
    {
        foreach (var item in row.ItemArray)
        {
            Console.Write($"{item}\t");
        }
        Console.WriteLine();
    }
}


int WriteSensorFile(FileInfo dataFile, SortedList<int, string> sIndex)
{
    
    IEnumerable<string> rFile = System.IO.File.ReadLines(dataFile.FullName);

    //Find the first instance of the repeat of the sensor count
    int sensorCount = 1;

    string firstSensor = sIndex.ElementAt(0).Value;

    for (int i = 1; i <= sIndex.Count() / 2; i++)
    {
        string item = sIndex.ElementAt(i).Value;
        if (item == firstSensor)
        {
            break;
        }
        else
        {
            //count the header twice to account for splitting the time and the data
            sensorCount++;
        }

    }

    int columnCount = 0;
    int skipCount = 0;

    for (int z = 0; z <= sensorCount; z++)
    {
        Console.Write("\rProcessed " + z + " of " + sensorCount + " sensor records. Skipped " + skipCount + " of " + sensorCount + " sensor records.");
        var firstFrog = sIndex.Where(pair => pair.Value == sIndex.ElementAt(z).Value)
                    .Select(pair => pair.Key);

        var secondFrog = sIndex.Where(pair => pair.Value == sIndex.ElementAt(z + 1).Value)
                    .Select(pair => pair.Key);

        //foreach (var item in firstFrog) { Console.WriteLine(item); }
        //foreach (var item in secondFrog) { Console.WriteLine(item); }

        List<string> strings = new List<string>();

        string sensorName = rFile.ElementAt(firstFrog.ElementAt(0)).Split(':')[1];
        
        /*if (sensorName.Contains("pery") || sensorName.Contains("wave"))
        {
            skipCount++;            
            continue;
        }*/

        for (int i = 0; i < firstFrog.Count() - 1; i++)
        {
            string[] input = rFile.Skip(firstFrog.ElementAt(i) + 1).Take(secondFrog.ElementAt(i) - firstFrog.ElementAt(i) - 2).ToArray();

            /*try
            {

                //split string into timestamp and value
                for (int j = 0; j < input.Count(); j++)
                {
                    string[] output = input[j].Split(',');
                    output[0] = EpochToTimestamp(Double.Parse(output[0])).ToString();
                    input[j] = String.Join(',', output);
                }

            } catch (System.Exception e) {
                Console.WriteLine("Exception during time conversion " + e.Message + "\r\nInput value:" + string.Join(":", input[0..1]));
                columnCount++;
                continue;
            }*/

            strings.AddRange(input);
        }

        using (DataTable dTable = new DataTable())
        {
            dTable.Columns.Add(sensorName);

            DataRow workRow = dTable.NewRow();

            foreach (string line in strings)
            {
                dTable.Rows.Add(line);
            }

            //PrintDataTable(dTable);
            string filePath = "D:\\Compressed Data\\CompiledTestdata.csv";
            string tmpfilePath = "D:\\Compressed Data\\tmpCompiledTestdata.csv";

            // Read all lines from the CSV file
            if (!System.IO.File.Exists(filePath))
            {
                System.IO.File.Create(filePath).Close();
            }

            // Read all lines from the CSV file
            if (!System.IO.File.Exists(tmpfilePath))
            {
                System.IO.File.Create(tmpfilePath).Close();
            }

            using (var sr = new StreamReader(filePath))
            using (var sw = new StreamWriter(tmpfilePath))
            {
                string? line = sr.ReadLine();

                if (line != null)
                {
                    sw.WriteLine($"{line}" + $"{sensorName},,");
                }
                else
                {
                    sw.WriteLine($"{sensorName},,");
                }

                //var lines = File.ReadAllLines(filePath).ToList();

                // Add the new column header to the first line
                /*if (lines.Any())
                {
                    lines[0] += $"{sensorName},,";
                } else
                {
                    lines.Add($"{sensorName},,");
                }*/

                //int x = 1;
                // Append new column data to each subsequent line
                foreach (DataRow row in dTable.Rows)
                {
                    IEnumerable<string?> fields = row.ItemArray.Select(field => field.ToString());

                    line = sr.ReadLine();

                    if (line != null)
                    {
                        //check the number of commas matches the column count
                        char ch = ',';
                        int count = line.Count(c => c == ch);
                        if (count < (columnCount))
                        {
                            sw.WriteLine(line + string.Concat(Enumerable.Repeat(",", (count) * 2)) + string.Join(" ", fields) + ",");
                            //lines[x] += string.Concat(Enumerable.Repeat(",", (columnCount-count)*2)) + string.Join(" ", fields) + ",";
                        }
                        else
                        {
                            sw.WriteLine(line + string.Join(" ", fields) + ",");
                            //lines[x] += string.Join(" ", fields) + ",";
                        }
                    }
                    else
                    {
                        sw.WriteLine(line + string.Concat(Enumerable.Repeat(",", columnCount * 2)) + string.Join(" ", fields) + ",");
                        //string outstring = string.Concat(Enumerable.Repeat(",", columnCount*2)) + string.Join(" ", fields) + ",";
                        //lines.Add(outstring);
                    }
                    //x++;

                }

                // Write the updated lines back to the file
                //File.WriteAllLines(filePath, lines);
                


                columnCount++;

                //StringBuilder sb = new StringBuilder();

                // Add column names
                //IEnumerable<string> columnNames = dTable.Columns.Cast<DataColumn>().Select(column => column.ColumnName);
                //sb.AppendLine(string.Join(",", columnNames));

                // Add rows
                /*foreach (DataRow row in dTable.Rows)
                {
                    IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                    sb.AppendLine(string.Join(",", fields));
                }*/

                // Write to file
                //File.AppendAllText(filePath, sb.ToString());
            }


            FileInfo fi = new FileInfo(filePath);
            FileInfo fifi = new FileInfo(tmpfilePath);

            if (fifi.Length < fi.Length)
            {
                Console.WriteLine("File size error at repetition " + $"{z} {sensorName} tmp {fifi.Length} bytes real {fi.Length} bytes. Discarding record.");
                skipCount++;
            }else
            {
                System.IO.File.Delete(filePath);
                System.IO.File.Copy(tmpfilePath, filePath);
            }

        }
    }

    return 0;
}


void unzip(IEnumerable<FileInfo> dataFiles)
{    

    foreach (FileInfo datafile in dataFiles)
    {
        bool breakLoop = false;

        if (datafile.Extension == ".txt")
        {
            Console.WriteLine(datafile.Name + " is not a zip file");
            break;
        }

        foreach (FileInfo file in dataFiles)
        {
            string x = file.Name.Substring(4, file.Name.Length - 8);
            string y = datafile.Name.Substring(4, datafile.Name.Length - 8);
            string z = file.Extension;

            if (y.Contains(x) && z == ".txt")
            {
                Console.WriteLine(datafile.Name + "File already unzipped");
                breakLoop = true;
                break;
            }

        }

        if (!breakLoop)
        {
            ZipFile.ExtractToDirectory(datafile.FullName, Directory.GetParent(datafile.FullName).ToString());
            Console.WriteLine("Zipfile extracted " + datafile.Name);
        }            

    }

}

FileInfo FindAndRemoveHeaders(FileInfo dataFile)
{
    StreamReader sr = new StreamReader(dataFile.FullName);
    StringBuilder sb = new StringBuilder();

    string fH = sr.ReadLine();

    string sensorName = fH.Substring(fH.IndexOf(":") + 1);

    if(!sensorName.Contains(":"))
    {
        return null;
    }
    sensorName = sensorName.Substring(0, sensorName.IndexOf(":"));

    Console.WriteLine("Sensor Name: " + sensorName);

    sb.AppendLine(sensorName);

    string line;
    while((line = sr.ReadLine()) !=null)
    {
        if (!line.Contains("Sensor Name"))
        {
            sb.AppendLine(line);
        }
    }

    sr.Dispose();

    System.IO.File.WriteAllText(Directory.GetParent(dataFile.FullName)+"\\"+sensorName+".txt", sb.ToString());

    return dataFile;
}

async Task<List<dataPoint>> GetStrainData()
{
    int sleepBetweenReadJump = 5000;

    int hourJumps = 4;

    int minuteJumps = 15;

    DateTime fromDTP = DateTime.Now.AddHours(-1);
    DateTime toDTP = DateTime.Now;

    List<dataPoint> data = new List<dataPoint>();

    influxConnect.InfluxDB infDB = new influxConnect.InfluxDB(addressTB, secretKeyTB);


    Console.WriteLine("Started to pull data from: " + fromDTP + " to: " + toDTP + ". The data is pulled in" + minuteJumps + " minute jumps. A message will be diplayed when data is pulled from the DB.");

    //change date to unix
    long unixFrom = ((DateTimeOffset)fromDTP).ToUnixTimeSeconds() * 1000000000;
    
    long unixTo = ((DateTimeOffset)toDTP).ToUnixTimeSeconds() * 1000000000;


    //split up the data pull to XX minute chunks
    var totalMinutes = toDTP.Subtract(fromDTP).TotalMinutes;
    DateTime toHelper = toDTP;
    DateTime fromHelper = fromDTP;
    var hourJump = 0;
    for (int i = 0; i < totalMinutes; i += minuteJumps)
    {
        //release DB isntance every X hours and re create it after 5s delay
        if (i != 0 && i % 60 == 0 && (i / 60) % hourJumps == 0)
        {
            Console.WriteLine("Wait " + sleepBetweenReadJump / 1000 + " seconds  to release DB memory usage, wait is every " + hourJumps + "h data pull");
            infDB.releaseDataBase();

            //zip data
            
            //zipData(mainForm);

            Thread.Sleep(sleepBetweenReadJump);
            hourJump++;
            infDB = new influxConnect.InfluxDB(addressTB, secretKeyTB);
        }

        if (toDTP.Subtract(fromHelper).TotalMinutes > minuteJumps)
            toHelper = fromHelper.AddMinutes(minuteJumps);
        else
            toHelper = toDTP;


        unixTo = ((DateTimeOffset)toHelper).ToUnixTimeSeconds() * 1000000000;
        List<dataPoint> dataHelper = await infDB.returnData(unixFrom, unixTo, orgTB, bucketTB, sensorsTB.Split(','), i / minuteJumps + 1, (int)(totalMinutes / minuteJumps) + 1);
        if (dataHelper == null)
        {
            Console.WriteLine("Influx Data retreive Error");
            break;
        }
        if (dataHelper.Count > 0)
            data.AddRange(dataHelper);

        //show data pulled messgae
        Console.WriteLine("Influx Data Retreived from: " + fromHelper + " to: " + toHelper);

        //jump in timer for the enxt pull
        fromHelper = fromHelper.AddMinutes(minuteJumps);
        unixFrom = ((DateTimeOffset)fromHelper).ToUnixTimeSeconds() * 1000000000;
    }

    Console.WriteLine("A total of " + data.Count() + " sensor data was retreived");

    return data;
}

List<Matrix<double>> getModeShapeMatrix(IEnumerable<FileInfo> dataFiles, out List<Matrix<double>> strainList, out List<long> timeData)
{
    List<Matrix<double>> modeList = new List<Matrix<double>>();
    strainList = new List<Matrix<double>>();
    timeData = new List<long>();

    //find all strain files

    //create list? of strain data

    StreamReader sr1 = new StreamReader("D:\\Compressed Data\\L1-SG-1.txt");
    StreamReader sr2 = new StreamReader("D:\\Compressed Data\\L2-SG-1.txt");
    StreamReader sr3 = new StreamReader("D:\\Compressed Data\\L3-SG-5.txt");
    StreamReader sr4 = new StreamReader("D:\\Compressed Data\\L4-SG-1.txt");
    StreamReader sr5 = new StreamReader("D:\\Compressed Data\\L5-SG-1.txt");
    StreamReader sr6 = new StreamReader("D:\\Compressed Data\\L6-SG-1.txt");

    // Read the first line of each file to dispose of the header
    sr1.ReadLine(); sr2.ReadLine(); sr3.ReadLine(); sr4.ReadLine(); sr5.ReadLine(); sr6.ReadLine();

    string Line;
    long time;

    while ((Line = sr1.ReadLine() )!= null && Line != "")
    {
        time = Int64.Parse(Line.Split(",")[0]);

        if (timeData.Count < 1)
        {
            timeData.Add(time);
        }

        if (time > timeData.Last() && timeData.Count > 1)
        {
            timeData.RemoveAt(timeData.Count - 1);
            timeData.Add(time);
        }
        else
        {
            timeData.Add(time);
        }


        //read a single line of each file and use this to create the gov mode shape for the boat.
        Matrix<double> strainMatrix = DenseMatrix.OfArray(new double[,]
                                                    {{Double.Parse(Line.Split(',')[1])},
                                                    {Double.Parse(sr2.ReadLine().Split(',')[1])},
                                                    {Double.Parse(sr3.ReadLine().Split(',')[1])},
                                                    {Double.Parse(sr4.ReadLine().Split(',')[1])},
                                                    {Double.Parse(sr5.ReadLine().Split(',')[1])},
                                                    {Double.Parse(sr6.ReadLine().Split(',')[1])}});


        //translate for the critical locations
        Matrix<double> modeShape = govModeShape.TransposeThisAndMultiply(govModeShape).Inverse() * (govModeShape.Transpose() * strainMatrix);

        modeList.Add(modeShape);
        strainList.Add(strainMatrix);

    }

    Console.WriteLine(modeList.ElementAt(0)[0,0]);
    Console.WriteLine(modeList.ElementAt(0)[1,0]);
    Console.WriteLine(modeList.ElementAt(0)[2,0]);

    return modeList;
}

Matrix<double> eqAmpModeShape(Matrix<double>eqAmpMatrix)
{
    //translate for the critical locations
    Matrix<double> modeShape = govModeShape.TransposeThisAndMultiply(govModeShape).Inverse() * (govModeShape.Transpose() * eqAmpMatrix);

    return modeShape;
}

void PeriDynamics(Matrix<double> LoadVector, LoadAndCycle Load, int location, List<long> timeData)
{
    double damage = periMatrix[0, location] + (periMatrix[1, location] * LoadVector[0, 0]) + (periMatrix[2, location] * LoadVector[1, 0]) + (periMatrix[3, location] * LoadVector[2, 0])
                    + (periMatrix[4, location] * Math.Pow(LoadVector[0, 0],2)) + (periMatrix[5, location] * Math.Pow(LoadVector[1, 0],2)) + (periMatrix[6, location] * Math.Pow(LoadVector[2, 0],2));

    damage = Load.CycleCount / damage;

    //estabish damage time period 

    DateTime start_time = EpochToTimestamp(timeData.ElementAt(0));
    DateTime end_time = EpochToTimestamp(timeData.Last());

    double year_factor = (8736*60) / (end_time - start_time).Minutes;

    Console.WriteLine("Location " + (location + 1) + " Peridynamics damage count " + damage+". \tEquivalent yearly damage " + year_factor * damage);
}

List<Matrix<double>> getCriticalStrains(List<Matrix<double>> modeList)
{
    List<Matrix<double>> criticalStrainMatrix = new List<Matrix<double>>();


    List<double> Critical3 = new List<double>();
    List<double> Critical4 = new List<double>();
    List<double> Critical5 = new List<double>();
    List<double> Critical193 = new List<double>();
    List<double> Critical194 = new List<double>();
    List<double> Critical195 = new List<double>();


    for (int i = 0; i < locMatrix.ColumnCount; i++)
    {

        for (int j = 0; j < modeList.Count() - 1; j++)
            {

            //Console.WriteLine(locMatrix);
            //Console.WriteLine(modeList.ElementAt(j));

            /*Critical3.Add((locMatrix.Row(0) * modeList.ElementAt(j))[0]);
            Critical4.Add((locMatrix.Row(1) * modeList.ElementAt(j))[0]);
            Critical5.Add((locMatrix.Row(2) * modeList.ElementAt(j))[0]);
            Critical193.Add((locMatrix.Row(3) * modeList.ElementAt(j))[0]);
            Critical194.Add((locMatrix.Row(4) * modeList.ElementAt(j))[0]);
            Critical195.Add((locMatrix.Row(5) * modeList.ElementAt(j))[0]);*/

            Matrix<double> strainMatrix = DenseMatrix.OfArray(new double[,]
                                                            {{(locMatrix.Row(0) * modeList.ElementAt(j))[0]},
                                                            {(locMatrix.Row(1) * modeList.ElementAt(j))[0]},
                                                            {(locMatrix.Row(2) * modeList.ElementAt(j))[0]},
                                                            {(locMatrix.Row(3) * modeList.ElementAt(j))[0]},
                                                            {(locMatrix.Row(4) * modeList.ElementAt(j))[0]},
                                                            {(locMatrix.Row(5) * modeList.ElementAt(j))[0]}});

            criticalStrainMatrix.Add(strainMatrix);

            //Console.WriteLine(locMatrix.Row(0) * modeList.ElementAt(j));

            //Console.WriteLine(modeList.ElementAt(j));

            }

    }

    /*foreach(Matrix<double> j in criticalStrainMatrix)
    {
        Console.WriteLine(j);
    }*/

    return criticalStrainMatrix;
}

void RainflowAndAmplitudes(List<Matrix<double>> strainList, List<Matrix<double>> modeShape, List<long> timeData)
{
    //Establish the equivalent strains for each of the critical locations across the boat before calculating their respective rainfalls.

    //List<Matrix<double>> critStrain = getCriticalStrains(modeList);

    List<double> Location1 = new List<double>();
    List<double> Location2 = new List<double>();
    List<double> Location3 = new List<double>();
    List<double> Location4 = new List<double>();
    List<double> Location5 = new List<double>();
    List<double> Location6 = new List<double>();

    Console.WriteLine("Rainflow Start " + strainList.Count());

    Location1 = removeDuplicates(strainList, 0);
    Location2 = removeDuplicates(strainList, 1);
    Location3 = removeDuplicates(strainList, 2);
    Location4 = removeDuplicates(strainList, 3);
    Location5 = removeDuplicates(strainList, 4);
    Location6 = removeDuplicates(strainList, 5);

    Console.WriteLine("Remove Duplicates\r\n" + Location1.Count + " " + Location2.Count + " " + Location3.Count + " " + Location4.Count + " " + Location5.Count + " " + Location6.Count);

    //ensure that only turning points are left in the data

    Location1 = findTurningPoints(Location1);
    Location2 = findTurningPoints(Location2);
    Location3 = findTurningPoints(Location3);
    Location4 = findTurningPoints(Location4);
    Location5 = findTurningPoints(Location5);
    Location6 = findTurningPoints(Location6);

    Console.WriteLine("Find Turning Points\r\n" + Location1.Count + " " + Location2.Count + " " + Location3.Count + " " + Location4.Count + " " + Location5.Count + " " + Location6.Count);
    //Rearrange the data to begin and end with the largest absolute value (cut and splice)

    Location1 = AbsoluteRearrange(Location1);
    Location2 = AbsoluteRearrange(Location2);
    Location3 = AbsoluteRearrange(Location3);
    Location4 = AbsoluteRearrange(Location4);
    Location5 = AbsoluteRearrange(Location5);
    Location6 = AbsoluteRearrange(Location6);

    Console.WriteLine("Absolute rearrange Location1 " + Location1.ElementAt(0) + " " + Location1.ElementAt(Location1.Count() - 1));
    Console.WriteLine("Absolute rearrange Location2 " + Location2.ElementAt(0) + " " + Location2.ElementAt(Location2.Count() - 1));
    Console.WriteLine("Absolute rearrange Location3 " + Location3.ElementAt(0) + " " + Location3.ElementAt(Location3.Count() - 1));
    Console.WriteLine("Absolute rearrange Location4 " + Location4.ElementAt(0) + " " + Location4.ElementAt(Location4.Count() - 1));
    Console.WriteLine("Absolute rearrange Location5 " + Location5.ElementAt(0) + " " + Location5.ElementAt(Location5.Count() - 1));
    Console.WriteLine("Absolute rearrange Location6 " + Location6.ElementAt(0) + " " + Location6.ElementAt(Location6.Count() - 1));

    //Load Range and Cycle counts
    LoadAndCycle Load1 = LoadAndCycleCount(Location1);
    LoadAndCycle Load2 = LoadAndCycleCount(Location2);
    LoadAndCycle Load3 = LoadAndCycleCount(Location3);
    LoadAndCycle Load4 = LoadAndCycleCount(Location4);
    LoadAndCycle Load5 = LoadAndCycleCount(Location5);
    LoadAndCycle Load6 = LoadAndCycleCount(Location6);

    Thread.Sleep(50);

   /* Matrix<double> PlotMatrix = DenseMatrix.Create(6, Load3.CycleRange.Count(), 0);

    for (int i = 0; i < Load3.CycleRange.Count(); i++)
    {
        PlotMatrix[0, i] = Load3.CycleRange.ElementAt(i);
        PlotMatrix[1, i] = Load4.CycleRange.ElementAt(i);
        PlotMatrix[2, i] = Load5.CycleRange.ElementAt(i);
        PlotMatrix[3, i] = Load193.CycleRange.ElementAt(i);
        PlotMatrix[4, i] = Load194.CycleRange.ElementAt(i);
        PlotMatrix[5, i] = Load195.CycleRange.ElementAt(i);

    }

    PlotData(PlotMatrix, "Pre-count plot");*/

    //PlotData(PlotMatrix, "Post-count plot");

    Console.WriteLine("Cycles 1 " + Load1.CycleCount + " Cycles 2 " + Load2.CycleCount + " Cycles 3 " + Load3.CycleCount+" Cycles 4 " + Load4.CycleCount+" Cycles 5 " + Load5.CycleCount+" Cycles 6 " + Load6.CycleCount);


    double eqAmpRangeLocation1 = EquivalentAmplitudeRange(Load1);
    double eqAmpRangeLocation2 = EquivalentAmplitudeRange(Load2);
    double eqAmpRangeLocation3 = EquivalentAmplitudeRange(Load3);
    double eqAmpRangeLocation4 = EquivalentAmplitudeRange(Load4);
    double eqAmpRangeLocation5 = EquivalentAmplitudeRange(Load5);
    double eqAmpRangeLocation6 = EquivalentAmplitudeRange(Load6);

    
    Matrix<double> eqAmpMatrix = DenseMatrix.OfArray(new[,]{{ eqAmpRangeLocation1},
                                                            { eqAmpRangeLocation2},
                                                            { eqAmpRangeLocation3},
                                                            { eqAmpRangeLocation4},
                                                            { eqAmpRangeLocation5},
                                                            { eqAmpRangeLocation6}});



    Matrix<double> LoadVector = eqAmpModeShape(eqAmpMatrix);

    Console.WriteLine(LoadVector);
    List<LoadAndCycle> LoadRangeList = new List<LoadAndCycle>();

    LoadRangeList.Add(Load1); LoadRangeList.Add(Load2); LoadRangeList.Add(Load3); LoadRangeList.Add(Load4); LoadRangeList.Add(Load5); LoadRangeList.Add(Load6);

    for (int j = 0; j < periMatrix.ColumnCount; j++)
    {
        PeriDynamics(LoadVector, LoadRangeList.ElementAt(j), j, timeData);
    }

    for (int j=0; j<eqAmpMatrix.RowCount; j++)
    {
        SNDamage(eqAmpMatrix[j, 0], LoadRangeList.ElementAt(j), j, timeData);
    }
   

}

double EquivalentAmplitudeRange(LoadAndCycle Load)
{
    //determine the equivalent amplitude range

    List<double> eqAmpRange = new List<double>();

    for (int i=0;i<Load.CycleRange.Count();i++)
    {
        eqAmpRange.Add(Math.Pow(((i / Load.CycleCount) * Math.Pow(Load.CycleRange.ElementAt(i), 5)), 0.2));
        //Console.WriteLine(eqAmpRange.ElementAt(i));
    }    

    Console.WriteLine("Equivalent Amplitude  Range Sum " + eqAmpRange.Sum());


    return eqAmpRange.Sum();

}

void SNDamage(double eqAmp, LoadAndCycle Load, int location, List<long> timeData)
{
    //material properties
    double thickness = 25.0;
    double thick_ref = 25.6;

    //constants
    double k = 0.2;

    //less than 10e7 cycles
    double a = 12.164;
    double m = 3.0;
    double damage = 0.0;

    DateTime start_time = EpochToTimestamp(timeData.ElementAt(0));
    DateTime end_time = EpochToTimestamp(timeData.Last());

    //Conversion of strain to stress
    //eqAmp = eqAmp * Math.Pow(10, -6) * (210*Math.Pow(10, 9));

    double year_factor = (8736 * 60) / (end_time - start_time).Minutes;

    damage = Math.Pow((a - m * Math.Log10(eqAmp * Math.Pow((thickness / thick_ref), k))),10);  
    damage = Load.CycleCount / damage;

    Console.WriteLine("Location "+(location+1)+" SN damage <10e7: " + damage+".\t\tEquivalent yearly damage " + year_factor * damage);

    //> 10e7 cycles
    a = 15.606;
    m = 5.0;

    damage = Math.Pow((a - m * Math.Log10(eqAmp * Math.Pow((thickness / thick_ref), k))), 10);
    damage = Load.CycleCount / damage;

    Console.WriteLine("Location " + (location + 1) + " SN damage >10e7: " + damage + ".\t\tEquivalent yearly damage " + year_factor * damage);

    
}

void PlotData(Matrix<double> PlotMatrix, string title)
{
    ScottPlot.Plot myPlot = new();


    for (int i =0; i < PlotMatrix.RowCount; i++)
    {
        myPlot.Add.Signal(PlotMatrix.Row(i).ToArray());
    }

    myPlot.Title(title);

    string fname = title + ".png";

    myPlot.SavePng(fname, 1000, 800);

    try
    {
        Process.Start(new ProcessStartInfo { FileName = fname, UseShellExecute = true });
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }

    
}


//Matrix<double> modeShape = govModeShape.TransposeThisAndMultiply(govModeShape).Inverse() * (govModeShape.Transpose() * strainData);

StrainData DataHandler(List<dataPoint> datapoints)
{
    //extract time range

    List<StrainData> dataList = new List<StrainData>();

    //get list of name versus values

    StrainData sd = new StrainData();
    sd.strain = new List<double>();
    sd.time = new List<long>();

    int j = 0;

    for (int i=j;i<datapoints.Count();i++)
    {
        sd.name = datapoints[i].name;
        foreach (infPoint inf in datapoints[i].points)
        {
            sd.strain.Add(inf.value);
            sd.time.Add(inf.date);
        }

        while (i < datapoints.Count)
        {
            Console.WriteLine(sd.name + " " + datapoints[i].name + " " + i);
            if (datapoints[i].name == sd.name)
            {
                foreach (infPoint inf in datapoints[i].points)
                {
                    sd.strain.Add(inf.value);
                    sd.time.Add(inf.date);
                }
                break;
            }
        }

    }

    return sd;
}

int Main(string[] args)
{
    // See https://aka.ms/new-console-template for more information
    Console.WriteLine("Hello, World!");

    Console.WriteLine("Enter the path to the data folder");
    string? datafolder = args[0];

    if (!Directory.Exists(datafolder))
    {
        Console.WriteLine(datafolder + " is not a valid folder");
        return -1;
    }
    
    Console.WriteLine("Enter the sensor data to retrieve");
    sensorsTB =  Console.ReadLine();

    List<dataPoint> data = GetStrainData().Result; 

    Task.WaitAll();

    List<StrainData> straindataList= DataHandler(data);

    Console.WriteLine(straindataList.name);

    string masterFile = "";

    DirectoryInfo fi = new DirectoryInfo(datafolder);

    IEnumerable<FileInfo> dataFiles = fi.EnumerateFiles();

    unzip(dataFiles);

    List<double> strainData = new List<double>();

    List<List<double>> LocStrainData = new List<List<double>>();
    List<long> timeData = new List<long>();

    foreach (FileInfo dataFile in dataFiles)
    {
        //ignore zip files
        if(dataFile.Extension == ".zip")
        {
            continue;
        }

        FileInfo noheaddataFile = FindAndRemoveHeaders(dataFile);
        if (noheaddataFile == null)
        {
            continue;
        }


        //strainData = GetStrainData(noheaddataFile, out timeData);

        //Console.WriteLine("Time Data " + timeData.Last() + " " + timeData.ElementAt(0) + " " + timeData.Count());

        //LocStrainData.Add(strainData);

        //Console.WriteLine(dataFile.Name);
        //break;
        //match the datafile to a pottential master file        
    }


    List<Matrix<double>> strainList = new List<Matrix<double>>();
    

    List<Matrix<double>> modeShape = getModeShapeMatrix(dataFiles, out strainList, out timeData);

    RainflowAndAmplitudes(strainList, modeShape, timeData);

    return 0;
}

string[] input = ["D:\\Compressed Data"];

Main(input);


public struct LoadAndCycle
{
    public List<double> CycleRange;
    public int CycleCount;
}

public struct StrainData
{
    public String name;
    public List<long> time;
    public List<double> strain;
}