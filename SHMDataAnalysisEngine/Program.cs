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

string Calcuations = "Peri-SG1,Peri-SG2,Peri-SG3,Peri-SG4,Peri-SG5,Peri-SG6,SN-SG1,SN-SG2,SN-SG3,SN-SG4,SN-SG5,SN-SG6,Snd1,Snd2,Snd3,Snd4,Snd5,Snd6,loc1,loc2,loc3";

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
        for (int num = 0; num < data.Count() - 1; num++)
        {
            if (sd.strain[num] != sd.strain[num + 1])
            {
                sd.strain.RemoveAt(num); sd.time.RemoveAt(num);
            }
        }
    }

    return data;
}

List<LoadAndCycle> LoadAndCycleCount(List<StrainData> data)
{

    LoadAndCycle Load = new LoadAndCycle();
    List<LoadAndCycle> LocationLoads = new List<LoadAndCycle>(); ;

    Load.CycleRange = new List<double>();
    Load.CycleCount = new int();

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

    DateTime fromDTP = DateTime.Parse("19/06/25 10:00:00");
    DateTime toDTP = DateTime.Parse("19/06/25 11:00:00"); ;

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

void RainflowAndAmplitudes(List<StrainData> strainList)
{
    //Establish the equivalent strains for each of the critical locations across the boat before calculating their respective rainfalls.

    //Identify the sensors for which data has been provided. This may not be the case that all
    //locations have data.

    //List<Matrix<double>> critStrain = getCriticalStrains(modeList);

    List<StrainData> LocationData = new List<StrainData>(6);
    StrainData blankSD = new StrainData();

    foreach(StrainData sd in strainList)
    {
        if (sd.name.Contains("L1") || sd.name.Contains("R1")) { LocationData.Add(sd); } 
        if (sd.name.Contains("L2") || sd.name.Contains("R2")) { LocationData.Add(sd); } 
        if (sd.name.Contains("L3") || sd.name.Contains("R3")) { LocationData.Add(sd); } 
        if (sd.name.Contains("L4")) { LocationData.Add(sd); } 
        if (sd.name.Contains("L5")) { LocationData.Add(sd); } 
        if (sd.name.Contains("L6")) { LocationData.Add(sd); } 
    }

    Console.WriteLine("Rainflow Start " + strainList.Count());

    LocationData = removeDuplicates(LocationData);

    foreach (StrainData ld in LocationData)
    {
        Console.WriteLine("Remove Duplicates "+ ld.name + " "+ld.strain.Count() );
    }

    //ensure that only turning points are left in the data

    LocationData = findTurningPoints(LocationData);

    foreach (StrainData ld in LocationData)
    {
        Console.WriteLine("Turning Points " + ld.name + " " + ld.strain.Count());
    }

    //Rearrange the data to begin and end with the largest absolute value (cut and splice)

    LocationData = AbsoluteRearrange(LocationData);

    foreach (StrainData ld in LocationData)
    {
        Console.WriteLine("Absolute rearrange "+ ld.name + " max " + ld.strain[ld.strain.Count-1] + " min " + ld.strain[0]);
    }


    //Load Range and Cycle counts
    List<LoadAndCycle> LocationLoads = LoadAndCycleCount(LocationData);


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

    foreach (LoadAndCycle ld in LocationLoads)
    {
        Console.WriteLine("Load and Cycle for "+ ld.Name +"Cycles " + ld.CycleCount + " Loads " + ld.CycleRange.Max());
    }


    Matrix<double> eqAmpMatrix = EquivalentAmplitudeRange(LocationLoads);


    Matrix<double> LoadVector = eqAmpModeShape(eqAmpMatrix);

    Console.WriteLine(LoadVector);
    /*List<LoadAndCycle> LoadRangeList = new List<LoadAndCycle>();

    LoadRangeList.Add(Load1); LoadRangeList.Add(Load2); LoadRangeList.Add(Load3); LoadRangeList.Add(Load4); LoadRangeList.Add(Load5); LoadRangeList.Add(Load6);*/

    List<long> timeData = new List<long>();

    timeData.Add(LocationData[0].time.Min());
    timeData.Add(LocationData[0].time.Max());


    for (int j = 0; j < periMatrix.ColumnCount; j++)
    {
        PeriDynamics(LoadVector, LocationLoads.ElementAt(j), j, timeData);
    }

    for (int j=0; j< LocationLoads.Count(); j++)
    {
        SNDamage(eqAmpMatrix[j, 0], LocationLoads.ElementAt(j), j, timeData);
    }
   

}

Matrix<double> EquivalentAmplitudeRange(List<LoadAndCycle> Loads)
{
    //determine the equivalent amplitude range

    List<double> eqAmpRange = new List<double>();
    int count = 0;

    Matrix<double> eqAmpMatrix = Matrix<double>.Build.Dense(6,1);

    foreach (LoadAndCycle ld in Loads)
    {

        for (int i = 0; i < ld.CycleRange.Count(); i++)
        {
            eqAmpRange.Add(Math.Pow(((i / ld.CycleCount) * Math.Pow(ld.CycleRange[i], 5)), 0.2));
            //Console.WriteLine(eqAmpRange.ElementAt(i));
        }

        Console.WriteLine("Equivalent Amplitude  Range Sum " + eqAmpRange.Sum());

        eqAmpMatrix[count, 0] = eqAmpRange.Sum();
    }

    return eqAmpMatrix;

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

void PerformCalculation()
{
    //Gather data from the topside sensors - later modify the data get function with a time range

    List<dataPoint> dataOut = new List<dataPoint>();

    List<StrainData> LongGauge = new List<StrainData> ();
    List<StrainData> ShortGauge = new List<StrainData>();
    List<StrainData> DispGauge = new List<StrainData>();

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


            ShortGauge.AddRange(tmpData);
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

    //Gather data from the correlation sensors

    //average the output of the short base gauges

    Console.WriteLine("Long Gauge Calculation begins");
    RainflowAndAmplitudes(LongGauge);

    Console.WriteLine("Displacement Gauge Calculation beings");
    RainflowAndAmplitudes(DispGauge);


    Console.WriteLine("Calculation Stages Complete");
}

void RosetteVonMises()
{

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

    Console.WriteLine("Enter a data option, 1 - Sensor to File\r\n 2 - Calculation Validation\r\n 3 - von Mises Calculation for Rosette's");
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