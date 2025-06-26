using influxConnect;
using influxConnect.Calculation;
using InfluxDB.Client.Api.Domain;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using NodaTime.Calendars;
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
using static System.Net.Mime.MediaTypeNames;
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


string Location1 = "L1-LG,L1-DG,L1-TG,L1-SG-1,L1-SG-2,L1-SG-3,L1-SG-4,L1-SG-5";
string Location1SG = "L1-SG-1,L1-SG-2,L1-SG-3,L1-SG-4,L1-SG-5";
string Location2 = "L2-LG,L2-DG,L2-TG,L2-SG-1,L2-SG-2,L2-SG-3,L2-SG-4,L2-SG-5";
string Location2SG = "L2-SG-1,L2-SG-2,L2-SG-3,L2-SG-4,L2-SG-5";
string Location3 = "L3-LG,L3-DG,L3-TG,L3-SG-1,L3-SG-2,L3-SG-3,L3-SG-4,L3-SG-5";
string Location3SG = "L3-SG-1,L3-SG-2,L3-SG-3,L3-SG-4,L3-SG-5";
string Location4 = "L4-LG,L4-DG,L4-TG,L4-SG-1,L4-SG-2,L4-SG-3,L4-SG-4,L4-SG-5";
string Location4SG = "L4-SG-1,L4-SG-2,L4-SG-3,L4-SG-4,L4-SG-5";
string Location5 = "L5-LG,L5-DG,L5-TG,L5-SG-1,L5-SG-2,L5-SG-3,L5-SG-4,L5-SG-5";
string Location5SG = "L5-SG-1,L5-SG-2,L5-SG-3,L5-SG-4,L5-SG-5";
string Location6 = "L6-LG,L6-DG,L6-TG,L6-SG-1,L6-SG-2,L6-SG-3,L6-SG-4,L6-SG-5";
string Location6SG = "L6-SG-1,L6-SG-2,L6-SG-3,L6-SG-4,L6-SG-5";

string Rosette1 = "R1-1A,R1-1B,R1-1C,R1-2A,R1-2B,R1-2C,R1-3A,R1-3B,R1-3C,R1-4A,R1-4B,R1-4C,R1-5A,R1-5B,R1-5C,R1-1-VM_ES,R1-2-VM_ES,R1-3-VM_ES,R1-4-VM_ES,R1-5-VM_ES,R1_AVG";
string Rosette2 = "R2-1A,R2-1B,R2-1C,R2-2A,R2-2B,R2-2C,R2-3A,R2-3B,R2-3C,R2-4A,R2-4B,R2-4C,R2-5A,R2-5B,R2-5C,R2-1-VM_ES,R2-2-VM_ES,R2-3-VM_ES,R2-4-VM_ES,R2-5-VM_ES,R2_AVG";
string Rosette3 = "R3-1A,R3-1B,R3-1C,R3-2A,R3-2B,R3-2C,R3-3A,R3-3B,R3-3C,R3-4A,R3-4B,R3-4C,R3-5A,R3-5B,R3-5C,R3-1-VM_ES,R3-2-VM_ES,R3-3-VM_ES,R3-4-VM_ES,R3-5-VM_ES,R3_AVG";

string CriticalDetails = "Detail-3,Detail-4,Detail-5,Detail-193,Detail-194,Detail-195";

string Calcuations = "Peri-SG1,Peri-SG2,Peri-SG3,Peri-SG4,Peri-SG5,Peri-SG6,SN-SG1,SN-SG2,SN-SG3,SN-SG4,SN-SG5,SN-SG6,Snd1,Snd2,Snd3,Snd4,Snd5,Snd6,loc1,loc2,loc3";

double yield_stress = 250000000.0;

DateTime EpochToTimestamp (double epochTimestamp)
{
    DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0,0, DateTimeKind.Utc).AddMilliseconds(epochTimestamp);
    //Console.WriteLine(dateTime); // Output: 7/22/2021 12:00:00 AM
    return dateTime;
}

List<StrainData> findTurningPoints(List<StrainData> data)
{
    foreach (StrainData sd in data)
    {
        for (int i = 1; i < data.Count() - 1; i++)
        {
            if (!(sd.strain[i - 1] < sd.strain[i] && sd.strain[i + 1] < sd.strain[i]) ||
                !(sd.strain[i - 1] > sd.strain[i] && sd.strain[i + 1] > sd.strain[i]))
            {
                sd.strain.RemoveAt(i); sd.time.RemoveAt(i);
            }
        }
    }

    return data;
}


List<StrainData> removeDuplicates(List<StrainData> data)
{

    foreach (StrainData sd in data)
    {
        for (int num = 0; num < sd.strain.Count() - 1; num++)
        {
            if (sd.strain[num] == sd.strain[num + 1])
            {
                sd.strain.RemoveAt(num); sd.time.RemoveAt(num);
            }
        }
    }

    return data;
}

void ShowComparison(List<CorrelationData> cData)
{

    StringBuilder sb = new StringBuilder();
    int count = 0;

    List<StrainData> cStrain = new List<StrainData>();

    Matrix<double> mAmplitude = Matrix.Build.Dense(6,1);

    for (int i =0; i<cData.Count;i++)
    {
        if (cData[i].name.Contains("R")) { cStrain.Add(cData[i].CriticalStrain); mAmplitude = cData[i].EquivalentRangeMatrix; }

        
        double[] results = new double[cData.Count];

        for (int j =0; j < cData[i].EquivalentRangeMatrix.RowCount;j++)
        {
            double result = cData[i].EquivalentRangeMatrix[j,0] * (1 / mAmplitude[j,0]);
            results[j] = result*100;
        }

        if (cData[i].name.Contains("1")) { count = 0; }
        if (cData[i].name.Contains("2")) { count = 1; }
        if (cData[i].name.Contains("3")) { count = 2; }

        sb.AppendLine(cData[i].name + "\tMax Strain "+ cData[i].CriticalStrain.strain.Max().ToString("0.000") + "\tEquivalent Amplitude " + cData[i].EquivalentRangeMatrix[count,0].ToString("0.00") + 
                        $"\tStrain comparison {((cData[i].CriticalStrain.strain.Max() / cStrain?[count].strain?.Max() ?? 1)*100).ToString("0.00")} %"
                        +$"\tAmplitude comparison {results[count].ToString("0.00")} %");

    }
    sb.AppendLine();

    Console.WriteLine(sb.ToString());
}

List<CorrelationData> CorrelationRainflow(List<StrainData> strainList)
{
    List<StrainData> LocationData = new List<StrainData>(6);
    List<CorrelationData> correlationDatas = new List<CorrelationData>();

    CorrelationData rosetteData = new CorrelationData();

    foreach (StrainData sd in strainList)
    {
        if (sd.name.Contains("R1")) { LocationData.Add(sd); rosetteData.name = sd.name; correlationDatas.Add(rosetteData);  }
        if (sd.name.Contains("R2")) { LocationData.Add(sd); rosetteData.name = sd.name; correlationDatas.Add(rosetteData); }
        if (sd.name.Contains("R3")) { LocationData.Add(sd); rosetteData.name = sd.name; correlationDatas.Add(rosetteData); }
    }

    for (int i = 0; i < correlationDatas.Count; i++)
    {
        rosetteData = correlationDatas[i];
        rosetteData.CriticalStrain = LocationData[i];
        correlationDatas[i] = rosetteData;
    }

    Console.WriteLine("Rainflow Start " + strainList.Count());

    LocationData = removeDuplicates(LocationData);
    LocationData = findTurningPoints(LocationData);
    LocationData = AbsoluteRearrange(LocationData);

    List<LoadAndCycle> LocationLoads = LoadAndCycleCount(LocationData);

    Thread.Sleep(50);

    foreach (LoadAndCycle ld in LocationLoads)
    {
        Console.WriteLine("Load and Cycle for " + ld.Name + "Cycles " + ld.CycleCount + " Loads " + ld.CycleRange.Max());
        int count = 0;
        rosetteData = correlationDatas[count];
        rosetteData.CriticalLoads = ld;
        correlationDatas[count] = rosetteData;
        count++;
    }

    Matrix<double> eqAmpMatrix = EquivalentAmplitudeRange(LocationLoads);
    for (int i = 0; i < correlationDatas.Count; i++)
    {
        rosetteData = correlationDatas[i];
        rosetteData.EquivalentRangeMatrix = eqAmpMatrix;
        correlationDatas[i] = rosetteData;
    }

    Matrix<double> LoadVector = eqAmpModeShape(eqAmpMatrix);
    for (int i = 0; i < correlationDatas.Count; i++)
    {
        rosetteData = correlationDatas[i];
        rosetteData.LoadVector = LoadVector;
        correlationDatas[i] = rosetteData;
    }

    Console.WriteLine("Load Vector " + LoadVector);

    List<long> timeData = new List<long>();

    timeData.Add(LocationData[0].time.Min());
    timeData.Add(LocationData[0].time.Max());

    Console.WriteLine(strainList.ElementAt(0).name + " results");
    //convert the monitored to unmonitored stress for the case of the SN curve - does peridynamics not require this approach?

    PDdamage pd = new PDdamage();
    pd.location = new List<int>();
    pd.damage = new List<double>();

    for (int j = 0; j < periMatrix.ColumnCount; j++)
    {
        var (location, damage) = PeriDynamics(LoadVector, LocationLoads.ElementAt(j), j, timeData);
        pd.location.Add(location);
        pd.damage.Add(damage);
    }

    for (int i = 0; i < correlationDatas.Count; i++)
    {
        rosetteData = correlationDatas[i];
        rosetteData.Peridynamics = pd;
        correlationDatas[i] = rosetteData;
    }

    SNcurve sn = new SNcurve();
    sn.location = new List<int>();
    sn.damage = new List<double>();

    for (int j = 0; j < LocationLoads.Count(); j++)
    {
        var (location, damage1, damage2) = SNDamage(eqAmpMatrix[j, 0], LocationLoads.ElementAt(j), j+3, timeData);
        sn.location.Add(location);
        sn.damage.Add(damage1);
        sn.damage.Add(damage2);

    }
    //}
    for (int i = 0; i < correlationDatas.Count; i++)
    {
        rosetteData = correlationDatas[i];
        rosetteData.SNTotal = sn;
        correlationDatas[i] = rosetteData;
    }

    return correlationDatas;
}

List<LoadAndCycle> LoadAndCycleCount(List<StrainData> data)
{

    LoadAndCycle Load = new LoadAndCycle();
    List<LoadAndCycle> LocationLoads = new List<LoadAndCycle>(); ;



    /*Matrix<double> PlotMatrix = DenseMatrix.Create(1,data.Count(),0);

    for(int i =0;i<data.Count();i++)
    {
        PlotMatrix[0, i] = data.ElementAt(i);
    }*/

    //PlotData(PlotMatrix, "Pre-count plot");

    //Convert to stress now or after for the SN Curve?

    int j = 0;

    foreach (StrainData sd in data)
    {
        Load.Name = sd.name;
        Load.CycleRange = new List<double>();
        Load.CycleCount = 0;

        if (LocationLoads.Any(item => item.Name == sd.name)) { LocationLoads.Add(LocationLoads.ElementAt(0)); continue; } ;

        while (true)
        {
            try
            {

                if ((sd.strain.Count <= 3) || ((j + 2) >= sd.strain.Count))
                {
                    Load.CycleRange.Add(Math.Abs(sd.strain[0] - sd.strain[sd.strain.Count() - 1]));
                    break;
                }

                if (Math.Abs(sd.strain[j + 2] - sd.strain[j + 1]) >= Math.Abs(sd.strain[j] - sd.strain[j + 1]))
                {
                    Load.CycleRange.Add(Math.Abs(sd.strain[j + 1] - sd.strain[j]));
                    sd.strain.RemoveAt(j + 1);
                    sd.strain.RemoveAt(j);

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

        LocationLoads.Add(Load);

    }

    return LocationLoads;

}

List<StrainData> AbsoluteRearrange(List<StrainData> data)
{
    //double abs = 0.0;
    int cutpoint = 0;
    List<double> cut = new List<double>();
    List<double> join = new List<double>();

    foreach (StrainData sd in data)
    {
        if (Math.Abs(sd.strain.Max()) > Math.Abs(sd.strain.Min()))
        {
            //abs = Math.Abs(sd.strain.Max());
            cutpoint = sd.strain.IndexOf(sd.strain.Max());
        }
        else
        {
            //abs = Math.Abs(sd.strain.Min());
            cutpoint = sd.strain.IndexOf(sd.strain.Min());
        }

        //slice and rearrange the list
        for (int i = 0; i < cutpoint + 1; i++)
        {
            cut.Add(sd.strain[i]);
        }
        for (int i = cutpoint; i < sd.strain.Count(); i++)
        {
            join.Add(sd.strain[i]);
        }
        join.AddRange(cut);

        sd.strain.RemoveRange(0, sd.strain.Count());
        sd.strain.AddRange(join);

        cut.Clear();
        join.Clear();
    }
    return data;
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

    Console.WriteLine("Enter the start time");
    //Console.ReadLine();
    Console.WriteLine("Enter the end time");
    //
    //Console.ReadLine();

    DateTime fromDTP = DateTime.Parse("19/06/25 09:00:00");
    DateTime toDTP = DateTime.Parse("19/06/25 10:00:00"); 

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
    Matrix<double> modeShape;

    if (eqAmpMatrix.RowCount < 6)
    {
        modeShape = govModeShape.SubMatrix(3,3,0,3).TransposeThisAndMultiply(govModeShape.SubMatrix(3, 3, 0, 3).Inverse() * (govModeShape.SubMatrix(3, 3, 0, 3).Transpose() * eqAmpMatrix));
    }else
    {
        modeShape = govModeShape.TransposeThisAndMultiply(govModeShape).Inverse() * (govModeShape.Transpose() * eqAmpMatrix);
    }

        //translate for the critical locations
        

    return modeShape;
}

(int, double) PeriDynamics(Matrix<double> LoadVector, LoadAndCycle Load, int location, List<long> timeData)
{
    double damage = periMatrix[0, location] + (periMatrix[1, location] * LoadVector[0, 0]) + (periMatrix[2, location] * LoadVector[1, 0]) + (periMatrix[3, location] * LoadVector[2, 0])
                    + (periMatrix[4, location] * Math.Pow(LoadVector[0, 0],2)) + (periMatrix[5, location] * Math.Pow(LoadVector[1, 0],2)) + (periMatrix[6, location] * Math.Pow(LoadVector[2, 0],2));

    damage = Load.CycleCount / damage;

    //estabish damage time period 

    DateTime start_time = EpochToTimestamp(timeData.ElementAt(0));
    DateTime end_time = EpochToTimestamp(timeData.Last());

    double year_factor = (8760*60) / (end_time - start_time).Minutes;

    Console.WriteLine($"Location {location + 1} Peridynamics count {damage.ToString("E")} \tEquivalent yearly damage {(year_factor * damage).ToString("E")}.\t\tLifetime damage {((17 * year_factor) * damage).ToString("E")}");

    return ((location+1),damage);
}

List<CorrelationData> RainflowAndAmplitudes(List<StrainData> strainList)
{
    //Establish the equivalent strains for each of the critical locations across the boat before calculating their respective rainfalls.

    //Identify the sensors for which data has been provided. This may not be the case that all
    //locations have data.

    //List<Matrix<double>> critStrain = getCriticalStrains(modeList);

    List<StrainData> LocationData = new List<StrainData>(6);
    //StrainData blankSD = new StrainData();

    List<CorrelationData> correlationDatas = new List<CorrelationData>();

    CorrelationData corData = new CorrelationData();

    foreach(StrainData sd in strainList)
    {
        if (sd.name.Contains("L1") || sd.name.Contains("R1")) { LocationData.Add(sd); corData.name = sd.name; correlationDatas.Add(corData); } 
        if (sd.name.Contains("L2") || sd.name.Contains("R2")) { LocationData.Add(sd); corData.name = sd.name; correlationDatas.Add(corData); } 
        if (sd.name.Contains("L3") || sd.name.Contains("R3")) { LocationData.Add(sd); corData.name = sd.name; correlationDatas.Add(corData); } 
        if (sd.name.Contains("L4"))  { LocationData.Add(sd); corData.name = sd.name; correlationDatas.Add(corData); } 
        if (sd.name.Contains("L5"))  { LocationData.Add(sd); corData.name = sd.name; correlationDatas.Add(corData); } 
        if (sd.name.Contains("L6"))  { LocationData.Add(sd); corData.name = sd.name; correlationDatas.Add(corData); } 
    }

    
    List<Matrix<double>> modeShapeMatrix = new List<Matrix<double>>();

        for (int i = 0; i < LocationData[0].strain.Count(); i++)
        {
            //validate data and it's location within the array
            if (LocationData.Count < 6)
            {
                //find all names in location data
                List<int> locNames = new List<int>();

                for (int j = 0; j < LocationData.Count; j++)
                {
                    int temp = Int32.Parse(LocationData[j].name.Split("-")[0].Where(char.IsDigit).ToArray()[0].ToString());
                    locNames.Add(temp);
                }

                for (int j = 0; j < (6 - locNames.Count()); j++)
                {
                    LocationData.Insert(locNames[j], strainList.ElementAt(0));
                }

            }

            Matrix<double> strainMatrix = DenseMatrix.OfArray(new double[,]
                                                    {{(LocationData.ElementAt(0).strain[i])},
                                                {LocationData.ElementAt(1).strain[i]},
                                                {LocationData.ElementAt(2).strain[i]},
                                                {LocationData.ElementAt(3).strain[i]},
                                                {LocationData.ElementAt(4).strain[i]},
                                                {LocationData.ElementAt(5).strain[i]}});

            Matrix<double> modeShape = govModeShape.TransposeThisAndMultiply(govModeShape).Inverse() * (govModeShape.Transpose() * strainMatrix);

            modeShapeMatrix.Add(modeShape);
        }


        //Rearrange critical data into strains by location - formatting will help.



        //Covert to the critical location data

        List<StrainData> criticalLocation = new List<StrainData>();

        foreach (string critName in (CriticalDetails.Split(",")))
        {
            StrainData strainData = new StrainData();
            strainData.name = critName;
            strainData.strain = new List<double>();
            strainData.time = new List<long>();

            criticalLocation.Add(strainData);
        }

    for (int i = 0; i < modeShapeMatrix.Count; i++)
    {
        for (int j =0;j<6;j++)
        {
            MathNet.Numerics.LinearAlgebra.Vector<double> details = locMatrix.Row(j) * modeShapeMatrix.ElementAt(i);
            criticalLocation.ElementAt(j).strain.Add(details[0]);
            criticalLocation.ElementAt(j).time.Add(LocationData.ElementAt(0).time[i]);
        }
    }

    for (int i = 0; i < correlationDatas.Count; i++)
    {
        corData = correlationDatas[i];
        corData.CriticalStrain = criticalLocation[i];
        correlationDatas[i] = corData;
    }


    Console.WriteLine("Rainflow Start " + strainList.Count());

    /*foreach (Matrix<double> ms in modeShapeMatrix)
    {
        StrainData criticalStrain = new StrainData();
        criticalStrain.name = "Critical Strain";
        criticalStrain.strain = new List<double>();
        criticalStrain.time = new List<long>();
        for(int i =0; i< locMatrix.RowCount; i++)
        {
            criticalStrain.strain.AddRange(locMatrix.Row(i) * ms);
            criticalStrain.time.Add(strainList.ElementAt(0).time[i]);
        }
        
        criticalLocation.Add(criticalStrain);
    }*/

    //LocationData = removeDuplicates(LocationData);
    criticalLocation = removeDuplicates(criticalLocation);

    /*foreach (StrainData ld in LocationData)
    {
        Console.WriteLine("Remove Duplicates "+ ld.name + " "+ld.strain.Count() );
    }
    foreach (StrainData cd in criticalLocation)
    {
        Console.WriteLine("Remove Duplicates critical "+ cd.strain.Count());
    }*/

    //ensure that only turning points are left in the data

    //LocationData = findTurningPoints(LocationData);
    criticalLocation = findTurningPoints(criticalLocation);

    /*foreach (StrainData ld in LocationData)
    {
        Console.WriteLine("Turning Points " + ld.name + " " + ld.strain.Count());
    }
    foreach (StrainData cd in criticalLocation)
    {
        Console.WriteLine("Turning Points " + cd.name + " " + cd.strain.Count());
    }*/

    //Rearrange the data to begin and end with the largest absolute value (cut and splice)

    LocationData = AbsoluteRearrange(LocationData);
    criticalLocation = AbsoluteRearrange(criticalLocation);

    /*foreach (StrainData ld in LocationData)
    {
        Console.WriteLine("Absolute rearrange "+ ld.name + " max " + ld.strain[ld.strain.Count-1] + " min " + ld.strain[0]);
    }
    foreach (StrainData cd in criticalLocation)
    {
        Console.WriteLine("Absolute rearrange " + cd.name + " max " + cd.strain[cd.strain.Count - 1] + " min " + cd.strain[0]);
    }*/

    //Load Range and Cycle counts
    //List<LoadAndCycle> LocationLoads = LoadAndCycleCount(LocationData);
    List<LoadAndCycle> CriticalLocationLoads = LoadAndCycleCount(criticalLocation);

    Thread.Sleep(50);

    List<StressIntensity> CriticalIntensities = deriveStressIntensity(CriticalLocationLoads);
    //foreach(StressIntensity si in CriticalIntensities)
    //{
        //int count = 0;
        //corData = correlationDatas[count];
        //corData.stressIntensity = si;
        //correlationDatas[count] = corData;
        //count++;
    //}
    

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


    foreach (LoadAndCycle ld in CriticalLocationLoads)
    {
        Console.WriteLine("Critical Load and Cycle for " + ld.Name + " Cycles " + ld.CycleCount + " Loads " + ld.CycleRange.Max());
        int count = 0;
        corData = correlationDatas[count];
        corData.CriticalLoads = ld;
        correlationDatas[count] = corData;
        count++;
    }


    //Matrix<double> eqAmpMatrix = EquivalentAmplitudeRange(LocationLoads);
    Matrix<double> eqAmpMatrixCritical = EquivalentAmplitudeRange(CriticalLocationLoads);
    corData.EquivalentRangeMatrix = eqAmpMatrixCritical;
    for(int i =0;i<correlationDatas.Count;i++)
    {
        corData = correlationDatas[i];
        corData.EquivalentRangeMatrix = eqAmpMatrixCritical;
        correlationDatas[i] = corData;
    }

    //if (!(strainList.ElementAt(0).name.Contains("R1") || strainList.ElementAt(0).name.Contains("R2") || strainList.ElementAt(0).name.Contains("R3")))
    //{
    //Matrix<double> LoadVector = eqAmpModeShape(eqAmpMatrix);
    Matrix<double> CriticalLoadVector = eqAmpModeShape(eqAmpMatrixCritical);

    for (int i = 0; i < correlationDatas.Count; i++)
    {
        corData = correlationDatas[i];
        corData.LoadVector = CriticalLoadVector;
        correlationDatas[i] = corData;
    }

    //Console.WriteLine("Load Vector "+LoadVector);
    Console.WriteLine("Critical Vector " +CriticalLoadVector);

        /*List<LoadAndCycle> LoadRangeList = new List<LoadAndCycle>();

        LoadRangeList.Add(Load1); LoadRangeList.Add(Load2); LoadRangeList.Add(Load3); LoadRangeList.Add(Load4); LoadRangeList.Add(Load5); LoadRangeList.Add(Load6);*/

        List<long> timeData = new List<long>();

        timeData.Add(LocationData[0].time.Min());
        timeData.Add(LocationData[0].time.Max());

        Console.WriteLine(strainList.ElementAt(0).name + " results");

    PDdamage pd = new PDdamage();
    pd.location = new List<int>();
    pd.damage = new List<double>();

    for (int j = 0; j < periMatrix.ColumnCount; j++)
    {
        //PeriDynamics(LoadVector, LocationLoads.ElementAt(j), j, timeData);
        var (location, damage) = PeriDynamics(CriticalLoadVector, CriticalLocationLoads.ElementAt(j), j, timeData);
        pd.location.Add(location);
        pd.damage.Add(damage);
    }

    for (int i = 0; i < correlationDatas.Count; i++)
    {
        corData = correlationDatas[i];
        corData.Peridynamics = pd;
        correlationDatas[i] = corData;
    }

    //convert the monitored to unmonitored stress for the case of the SN curve - does peridynamics not require this approach?

    SNcurve sn = new SNcurve();
    sn.location = new List<int>();
    sn.damage = new List<double>();

    for (int j = 0; j < CriticalLocationLoads.Count(); j++)
        {
        //SNDamage(eqAmpMatrix[j, 0] * locMatrix[j,0], LocationLoads.ElementAt(j), j, timeData);
        var(location, damage1, damage2) =  SNDamage(eqAmpMatrixCritical[j, 0], CriticalLocationLoads.ElementAt(j), j, timeData);
        sn.location.Add(location);
        sn.damage.Add(damage1);
        sn.damage.Add(damage2);
    }
    //}

    for (int i = 0; i < correlationDatas.Count; i++)
    {
        corData = correlationDatas[i];
        corData.SNTotal = sn;
        correlationDatas[i] = corData;
    }

    return correlationDatas;
}

Matrix<double> EquivalentAmplitudeRange(List<LoadAndCycle> Loads)
{
    //determine the equivalent amplitude range

    List<double> eqAmpRange = new List<double>();
    int count = 0;

    Matrix<double> eqAmpMatrix = Matrix<double>.Build.Dense(Loads.Count,1);

    foreach (LoadAndCycle ld in Loads)
    {

        for (int i = 0; i < ld.CycleRange.Count(); i++)
        {
            //E is only multipled by 3 to account for the microstrain 10^-6 conversion of the strain input
            eqAmpRange.Add(((i / ld.CycleCount) * Math.Pow(ld.CycleRange[i], 5)));
            //Console.WriteLine(eqAmpRange.ElementAt(i));
        }

        Console.WriteLine("Equivalent Amplitude Range"+ld.Name + " \t" + (Math.Pow(eqAmpRange.Sum(),0.2)).ToString("E"));

        eqAmpMatrix[count, 0] = Math.Pow(eqAmpRange.Sum(),0.2);
        count++;

        eqAmpRange.Clear();
    }

    return eqAmpMatrix;

}

(int, double, double) SNDamage(double eqAmp, LoadAndCycle Load, int location, List<long> timeData)
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
    double damage1 = Load.CycleCount / damage;

    Console.WriteLine($"Location {location + 1} <10e7 damage count {damage1.ToString("E")} \tEquivalent yearly damage {(year_factor * damage1).ToString("E")}.\t\tLifetime damage {((17 * year_factor) * damage1).ToString("E")}");

    //> 10e7 cycles
    a = 15.606;
    m = 5.0;

    damage = Math.Pow((a - m * Math.Log10(eqAmp * Math.Pow((thickness / thick_ref), k))), 10);
    double damage2 = Load.CycleCount / damage;

    Console.WriteLine($"Location {location + 1} >10e7 damage count {damage2.ToString("E")} \tEquivalent yearly damage {(year_factor * damage2).ToString("E")}.\t\tLifetime damage {((17 * year_factor) * damage2).ToString("E")}");
    //presumed 17 years service life

    return ((location + 1), damage1, damage2);
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

List<StrainData> DataHandler(List<dataPoint> datapoints)
{
    //extract time range

    List<StrainData> dataList = new List<StrainData>();

    //get list of name versus values

    
    List<string> nameArray = new List<String>();

    int j = 0;

    for (int i=0;i<datapoints.Count();i++)
    {
        StrainData sd = new StrainData();
        sd.strain = new List<double>();
        sd.time = new List<long>();

        sd.name = datapoints[i].name;

        if (nameArray.Contains(sd.name)) { Console.WriteLine("data already counted"); continue; }

        for (int z = i;z< datapoints.Count();z++)
        {
            if (datapoints[z].name == sd.name && !(nameArray.Contains(sd.name)))
            {

                    foreach (infPoint inf in datapoints[z].points)
                    {
                        sd.strain.Add(inf.value);
                        sd.time.Add(inf.date);
                    }

                

                //datapoints.Remove(datapoint);
            }
        }

        dataList.Add(sd);
        nameArray.Add(sd.name);

    }

    return dataList;
}


/*Matrix<double> StraindataToMatrix(List<StrainData> data)

{
    //Matrix<double> DataOut = DenseMatrix.OfRows(data.Count(), data.ElementAt(0).strain.Count(), data.ElementAt(0).strain.ToArray());


    

    return DataOut;
}*/

void ZipData(StrainData sData)
{
    //Create data directory
    string dataFolder = "D:\\SHM_Data\\" +  sData.name;

    if (!Directory.Exists(dataFolder))
    {
        Directory.CreateDirectory(dataFolder);
    }

    StreamWriter sw = new StreamWriter(dataFolder + "\\"+ sData.name + ".txt");

    sw.WriteLine(sData.name);

    for(int i=0; i<sData.strain.Count();i++)
    {
        sw.WriteLine(String.Join(",", sData.time[i], sData.strain[i]));
    }

    sw.Dispose();

    Thread.Sleep(300);

    string ZipPath = Directory.GetParent(dataFolder) + "\\" + sData.name + ".zip";

    if (System.IO.File.Exists(ZipPath))
    {
        System.IO.File.Delete(ZipPath);
    }

    ZipFile.CreateFromDirectory(dataFolder, ZipPath);   


}

void DataToFile()
{
    Console.WriteLine("Enter the sensor data to retrieve");
    sensorsTB = Console.ReadLine();

    List<dataPoint> data = GetStrainData().Result;

    Task.WaitAll();

    List<StrainData> straindataList = DataHandler(data);

    foreach (StrainData sd in straindataList)
    {
        Console.WriteLine(sd.name);
        //PlotData(DenseMatrix.OfArray(straindataList.ElementAt(0).strain.ToArray()), straindataList.ElementAt(0).name);
        ZipData(sd);
    }
}

List<StrainData> AverageLocationData (List<StrainData> tmpData)
{
    List<StrainData> avgData = new List<StrainData>();

    avgData.Add(tmpData.ElementAt(0));

    for (int i = 0; i < tmpData[0].strain.Count(); i++)
    {

        List<double> avgList = new List<double>();

        foreach (StrainData sd in tmpData)
        {
            avgList.Add(sd.strain[i]);


        }

        avgData.ElementAt(0).strain[i] = avgList.Average();
    }

    return avgData;
}

void PerformCalculation()
{
    //Gather data from the topside sensors - later modify the data get function with a time range
    List<CorrelationData> comparator = new List<CorrelationData>();

    List<dataPoint> dataOut = new List<dataPoint>();

    List<StrainData> LongGauge = new List<StrainData> ();
    List<StrainData> ShortGauge = new List<StrainData>();
    List<StrainData> DispGauge = new List<StrainData>();
    List<StrainData> CorrGauge = new List<StrainData>();


    //von mises correlation gauges
    try
    {
        //string[] rosArray = (string[])Rosette1.Split(",").ToArray().Concat(Rosette2.Split(",").ToArray().Concat(Rosette3.Split(",")));
        List<string> tmpList = new List<string>();

        List<StrainData> tmpData = new List<StrainData>();

        foreach (string array in (string.Join(",",Rosette1,Rosette2,Rosette3)).Split(","))
        {
            if (array.Contains("VM_ES") || array.Contains("TG") || array.Contains("AVG") || array.Contains("B") || array.Contains("C"))
            {
                continue;
            } else
            {
                tmpList.Add(array);
            }
        }

        sensorsTB = String.Join(",", tmpList.ToArray());
        dataOut = GetStrainData().Result;
        Task.WaitAll();

        tmpData = DataHandler(dataOut);

        for (int i =0; i<tmpData.Count();i=i+5)
        {
            //average rosette's in sets of 3
            List<StrainData> avgData = new List<StrainData>(3);

            avgData = AverageLocationData(tmpData.GetRange(i,5));

            CorrGauge.AddRange(avgData);
        }

        

    } catch (System.Exception ex)
    {
        Console.WriteLine("Correlation get error " + ex.Message);
    }

    //Location SG
    
    try
    {
        string[] SGarray = new string[6] { Location1SG, Location2SG, Location3SG, Location4SG, Location5SG, Location6SG };
        foreach (string SG in SGarray)
        {
            sensorsTB = SG;
            dataOut = GetStrainData().Result;
            Task.WaitAll();
            List<StrainData> tmpData = DataHandler(dataOut);

            //------------------------------------------------------------//
            //Averaging of the sensors should take place somewhere here
            //------------------------------------------------------------//
            List<StrainData> avgData = new List<StrainData>();

            avgData = AverageLocationData(tmpData);

            ShortGauge.Add(avgData.ElementAt(0));
        }

        //ShortGauge = DataHandler(dataOut);

    }catch (Exception ex)
    {
        Console.WriteLine("Short Gauge get failed " + ex.Message);
    }


    try
    {
        string LG_array = "L1-LG,L2-LG,L3-LG,L4-LG,L5-LG,L6-LG";

        sensorsTB = LG_array;
        dataOut = GetStrainData().Result;

        LongGauge = DataHandler(dataOut);

        Console.WriteLine("Long gauge data retrieved");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Failed to get long gauge " + ex.Message);
    }

    try
    {
        string DG_array = "L1-DG,L2-DG,L3-DG,L4-DG,L5-DG,L6-DG";

        sensorsTB = DG_array;
        dataOut = GetStrainData().Result;

        DispGauge = DataHandler(dataOut);

        Console.WriteLine("Displacement data retrieved");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Failed to get long gauge " + ex.Message);
    }

    List<CorrelationData> tmpOut = new List<CorrelationData>();

    //Gather data from the correlation sensors

    Console.WriteLine("Correlation Gauge Calculation beggins");
    tmpOut = CorrelationRainflow(CorrGauge);
    comparator.AddRange(tmpOut);

    Console.WriteLine("Short Gauge Calculation begins");
    tmpOut = RainflowAndAmplitudes(ShortGauge);
    comparator.AddRange(tmpOut);

    Console.WriteLine("Long Gauge Calculation begins");
    tmpOut = RainflowAndAmplitudes(LongGauge);
    comparator.AddRange(tmpOut);

    Console.WriteLine("Displacement Gauge Calculation beings");
    tmpOut = RainflowAndAmplitudes(DispGauge);
    comparator.AddRange(tmpOut);


    Console.WriteLine("Calculation Stages Complete");
    Console.WriteLine("Comparison Statistics");

    ShowComparison(comparator);
}

void RosetteVonMises()
{
    //Get the von Mises for a given time period
    List<string> vmData = new List<string>();

    
    string[] vmStrings = new string[0];
    foreach (string s in (string.Join(",", Rosette1, Rosette2, Rosette3)).Split(","))
    {
        if (s.Contains("VM"))
        {
            vmData.Add(s);
            vmStrings.Append(s);
        }
    }

    sensorsTB = String.Join(",", vmData.ToArray());

    List < dataPoint > vmRawData = GetStrainData().Result;
    Task.WaitAll();

    List<StrainData> vmStrainData = DataHandler(vmRawData);

    foreach (StrainData sd in vmStrainData)
    {
        Console.WriteLine($"{sd.name} has a maximum recorded von Mises of {sd.strain.Max().ToString("0.00")} MPa\t" +
            $"or\t{(((sd.strain.Max()*Math.Pow(10,6)) / yield_stress)*100).ToString("0.00") } % of the yield stress");
    }

    //Take both the strain outputs and the VM_ES calculated value

    //Calculate the principal stresses in all directions


    //Derive the von Mises stress and compare with the yield criterion

    //stress in each direciton is taken by the multiplication of the strain with the young's modulus

    double YoungE = 206E9;
    double ShearMod = 79E9;
    double PoissonV = 0.3;

    vmData.Clear();

    foreach (string s in (string.Join(",", Rosette1, Rosette2, Rosette3)).Split(","))
    {
        if (!(s.Contains("VM") || s.Contains("AVG") || s.Contains("TG")))
        {
            vmData.Add(s);
            vmStrings.Append(s);
        }
    }

    sensorsTB = String.Join(",", vmData.ToArray());

    List<dataPoint> vmCalcData = GetStrainData().Result;
    Task.WaitAll();

    List<StrainData> vmStrainCalcData = DataHandler(vmCalcData);

    //ex = ea
    //ey = 1/3(2*eb+2c-ea)
    //yxy = 2/sqrt3 (eb-ec)

    //principal stress equations

    List<StrainData> vonMisesList = new List<StrainData>();

    for (int i = 0; i < vmStrainCalcData.Count; i = i + 3)
    {

        double e_x = 0.0;
        double e_y = 0.0;
        double y_xy = 0.0;

        double e_a = 0.0, e_b = 0.0, e_c = 0.0;

        StrainData vonMisesStrain = new StrainData();
        vonMisesStrain.name = vmStrainCalcData.ElementAt(i).name;
        vonMisesStrain.strain = new List<double>();
        vonMisesStrain.time = vmStrainCalcData.ElementAt(i).time;

        //take reading for a chunk of time and presume the absolute amplitude is the difference?

        

        for (int j = 0; j < vmStrainCalcData.ElementAt(i).strain.Count-1; j++)
        {
            e_a = (vmStrainCalcData.ElementAt(i).strain[j]) * Math.Pow(10, -6);
            e_b = (vmStrainCalcData.ElementAt(i + 1).strain[j]) * Math.Pow(10, -6);
            e_c = (vmStrainCalcData.ElementAt(i + 2).strain[j]) * Math.Pow(10, -6);

            e_x = e_a;
            e_y = (2 / 3) * (e_b + e_c - (e_a/2));
            //y_xy = (2 / Math.Sqrt(3)) * (e_b - e_c);

            //double s_x = e_x * YoungE;
            //double s_y = e_y * YoungE;
            //double t_xy = y_xy * ShearMod;


            double principal1 = (YoungE/1-PoissonV)*((e_a+e_b+e_c)/3) + ((YoungE / 1 - PoissonV) *(Math.Sqrt(Math.Pow((2*e_a-e_b-e_c)/3,2)+ (1/3)*Math.Pow(e_b-e_c,2))));
            double principal2 = (YoungE / 1 - PoissonV) * ((e_a + e_b + e_c) / 3) - ((YoungE / 1 - PoissonV) * (Math.Sqrt(Math.Pow((2 * e_a - e_b - e_c) / 3, 2) + (1 / 3) * Math.Pow(e_b - e_c, 2))));

            //Hooke's Law
            //principal1 = (YoungE / 1 - Math.Pow(PoissonV, 2)) * (e_x + (PoissonV * e_y));
            //principal2 = ((YoungE / 1 - Math.Pow(PoissonV, 2)) * (e_y + (PoissonV * e_x)));

            double vMises = Math.Sqrt((Math.Pow(principal1,2) * Math.Pow(principal1,2)) - (principal1 * principal2));
            //double vMises = (1 / Math.Sqrt(2)) * Math.Sqrt(Math.Pow((principal1 - principal2), 2) + Math.Pow(principal2, 2) + Math.Pow(principal1, 2));

            vonMisesStrain.strain.Add(vMises);
        }

        vonMisesList.Add(vonMisesStrain);
    }

    List<LoadAndCycle> vmStrainCalcLoads = LoadAndCycleCount(vmStrainCalcData);

    Matrix<double> CalcLoads = EquivalentAmplitudeRange(vmStrainCalcLoads);

    int count = 0;

    foreach (StrainData sd in vonMisesList)
    {
        double vmCalcAverage = ((CalcLoads[count, 0] + CalcLoads[count + 1, 0] + CalcLoads[count + 2, 0])/3)*Math.Pow(206,3);

        //double e_a = CalcLoads[count, 0];
        //double e_b = CalcLoads[count+1, 0];
        //double e_c = CalcLoads[count+2, 0];

        //double principal1 = (YoungE / 1 - PoissonV) * ((e_a + e_b + e_c) / 3) + ((YoungE / 1 - PoissonV) * (Math.Sqrt(Math.Pow((2 * e_a - e_b - e_c) / 3, 2) + (1 / 3) * Math.Pow(e_b - e_c, 2))));
        //double principal2 = (YoungE / 1 - PoissonV) * ((e_a + e_b + e_c) / 3) - ((YoungE / 1 - PoissonV) * (Math.Sqrt(Math.Pow((2 * e_a - e_b - e_c) / 3, 2) + (1 / 3) * Math.Pow(e_b - e_c, 2))));

        //double vMises = Math.Sqrt((Math.Pow(principal1, 2) * Math.Pow(principal1, 2)) - (principal1 * principal2));

        Console.WriteLine($"{sd.name} has a maximum calculated von Mises of {(sd.strain.Max()/Math.Pow(10,6)).ToString("0.00")} MPa\t" +
            $"or\t{(((sd.strain.Max()) / yield_stress) * 100).ToString("0.00")} % of the yield stress" +
            $"\t{(vmCalcAverage / Math.Pow(10,6)).ToString("0.00")} Mpa" +
            $" or\t{((vmCalcAverage / yield_stress) * 100).ToString("0.00")} % of the yield stress");
        count = count + 3;

    }
    //Compare
}

List<StressIntensity> deriveStressIntensity(List<LoadAndCycle> Loads)
{
    List<StressIntensity> Intensities = new List<StressIntensity>();

    foreach (LoadAndCycle ld in Loads)
    {

        StressIntensity dataOut = new StressIntensity();
        dataOut.Intensity_5mm_inf = new List<double>();
        dataOut.Intensity_10mm_inf = new List<double>();
        dataOut.Intensity_20mm_inf = new List<double>();
        dataOut.Intensity_5mm_1M = new List<double>();
        dataOut.Intensity_10mm_1M = new List<double>();
        dataOut.Intensity_20mm_1M = new List<double>();
        dataOut.name = ld.Name;

        //dependant on geometry and crack size
        double StressIntensityCorrectionFactor = 0.0;

        double StressIntensityThresholdValue = 0.5;

        for (int i = 0; i < ld.CycleRange.Count(); i++)
        {
            //BS7910 Table 8.4, values for steel freely corroding in marine environment
            //R>=0.5 Mean + 2SD for welded joints
            double m = 3.42;
            double A = 1.72 * Math.Pow(10, -13);

            double crack = 0.0;
            double width = 0.0;

            double K_5mm_inf = 0.0;
            double K_10mm_inf = 0.0;
            double K_20mm_inf = 0.0;

            double K_5mm_1m = 0.0;
            double K_10mm_1m = 0.0;
            double K_20mm_1m = 0.0;

            StressIntensityCorrectionFactor = 1.0; //value for infinitely large plate

            K_5mm_inf = A * Math.Pow((StressIntensityCorrectionFactor * (ld.CycleRange[i] * Math.Pow(206, 3)) * Math.Sqrt(Math.PI / 5)), m);
            K_10mm_inf = A * Math.Pow((StressIntensityCorrectionFactor * (ld.CycleRange[i] * Math.Pow(206, 3)) * Math.Sqrt(Math.PI / 10)), m);
            K_20mm_inf = A * Math.Pow((StressIntensityCorrectionFactor * (ld.CycleRange[i] * Math.Pow(206, 3)) * Math.Sqrt(Math.PI / 20)), m);

            dataOut.Intensity_5mm_inf.Add(K_5mm_inf);
            dataOut.Intensity_10mm_inf.Add(K_10mm_inf);
            dataOut.Intensity_20mm_inf.Add(K_20mm_inf);

            //For centre cracks
            crack = 5.0;
            width = 1000.0;
            StressIntensityCorrectionFactor = 1 - 0.025 * Math.Pow((crack / width), 2) + 0.06 * Math.Pow((crack / width), 4) / Math.Sqrt(Math.Cos((Math.PI * crack / width * 2)));

            K_5mm_1m = StressIntensityCorrectionFactor * (ld.CycleRange[i] * Math.Pow(206, 3)) * Math.Sqrt(Math.PI / 5);
            K_10mm_1m = StressIntensityCorrectionFactor * (ld.CycleRange[i] * Math.Pow(206, 3)) * Math.Sqrt(Math.PI / 10);
            K_20mm_1m = StressIntensityCorrectionFactor * (ld.CycleRange[i] * Math.Pow(206, 3)) * Math.Sqrt(Math.PI / 20);

            dataOut.Intensity_5mm_1M.Add(K_5mm_1m);
            dataOut.Intensity_10mm_1M.Add(K_10mm_1m);
            dataOut.Intensity_20mm_1M.Add(K_20mm_1m);
        }

        Console.WriteLine($"Stress intensity for {dataOut.name} \tat 5mm {dataOut.Intensity_5mm_inf.Max().ToString("0.00")}" +
                                                                $"\tat 10mm {dataOut.Intensity_10mm_inf.Max().ToString("0.00")}" +
                                                                $"\tat 20mm {dataOut.Intensity_20mm_inf.Max().ToString("0.00")}" +
                                                                $"\tat 5mm  {dataOut.Intensity_5mm_1M.Max().ToString("0.00")}" +
                                                                $"\tat 10mm {dataOut.Intensity_10mm_1M.Max().ToString("0.00")}" +
                                                                $"\tat 20mm {dataOut.Intensity_20mm_1M.Max().ToString("0.00")}");

        Intensities.Add(dataOut);
    }
    return Intensities;
}

void CriticalStrainCorrelation()
{
    //Get the equivalent strain derived from the different strain sensors



    //Get the derived strain from the Rosette's (one at a time and compare across A,B,C)

    //Determine which sensors provide the greatest accuracy
}

void StressStrain()
{
    //find the best practice to convert between the stress and strain of the system


}

int Main(string[] args)
{
    Console.WriteLine("Enter the path to the data folder");
    string? datafolder = args[0];

    if (!Directory.Exists(datafolder))
    {
        Console.WriteLine(datafolder + " is not a valid folder");
        return -1;
    }

    Console.WriteLine("Enter a data option.\r\n\t1 - Sensor to File\r\n\t2 - Calculation Validation\r\n\t3 - von Mises values\r\n\t4 - Stress-Strain Calculation\r\n\t5 - Sensor Correlation");
    string option = Console.ReadLine();

    switch (option)
    {
        case "1":
            DataToFile();
            return 0;
        case "2":
            PerformCalculation();
            return 0;
        case "3":
            RosetteVonMises();
            return 0;
        case "4":
            StressStrain();
            return 0;
        case "5":
            CriticalStrainCorrelation();
            return 0;
        default:
            return 0;
    }

    Console.WriteLine("Enter the sensor data to retrieve");
    sensorsTB =  Console.ReadLine();

    List<dataPoint> data = GetStrainData().Result; 

    Task.WaitAll();

    List<StrainData> straindataList= DataHandler(data);

    foreach (StrainData sd in straindataList)
    {
        Console.WriteLine(sd.name);
        //PlotData(DenseMatrix.OfArray(straindataList.ElementAt(0).strain.ToArray()), straindataList.ElementAt(0).name);
    }

    DirectoryInfo fi = new DirectoryInfo(datafolder);

    IEnumerable<FileInfo> dataFiles = fi.EnumerateFiles();

    unzip(dataFiles);

    //List<double> strainData = new List<double>();

    //List<List<double>> LocStrainData = new List<List<double>>();
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

    //RainflowAndAmplitudes(strainList, modeShape, timeData);

    return 0;
}

string[] input = ["D:\\Compressed Data"];

Main(input);


public struct LoadAndCycle
{
    public String Name;
    public List<double> CycleRange;
    public int CycleCount;
}

public struct StrainData
{
    public String name;
    public List<long> time;
    public List<double> strain;
}

public struct StressIntensity
{
    public String name;
    public List<double> Intensity_5mm_inf;
    public List<double> Intensity_10mm_inf;
    public List<double> Intensity_20mm_inf;
    public List<double> Intensity_5mm_1M;
    public List<double> Intensity_10mm_1M;
    public List<double> Intensity_20mm_1M;
}


public struct CorrelationData
{
    public String name { get; set; }
    public LoadAndCycle CriticalLoads { get; set; }
    public Matrix<double> EquivalentRangeMatrix { get; set; }
    public Matrix<double> LoadVector { get; set; }
    public StressIntensity stressIntensity { get; set; }
    public PDdamage Peridynamics { get; set; }
    public SNcurve SNTotal { get; set; }
    public StrainData CriticalStrain { get; set; }
}

public struct PDdamage
{
    public String name;
    public List<int> location;
    public List<double> damage;
}

public struct SNcurve
{
    public String name;
    public List<int> location;
    public List<double> damage;
}