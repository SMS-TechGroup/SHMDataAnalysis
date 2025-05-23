using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Data;
using System.Text;

void GetDataBlocks(string[] fileData)
{
    //get the range at which different sensors start

    //remember that this may return multiple instances.
}

void CompileSensorData(string[] filedata, List<int>DataBlocks)
{

}

void UnzipFile(string filename)
{

}

DateTime EpochToTimestamp (double epochTimestamp)
{
    DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0,0, DateTimeKind.Utc).AddMilliseconds(epochTimestamp);
    Console.WriteLine(dateTime); // Output: 7/22/2021 12:00:00 AM
    return dateTime;
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
            Console.WriteLine(testElement + " is numeric");
            double.TryParse(testElement, out numOut); ;
            return numOut;
        }
    }
    return 0;
}

string FindMasterFile(DateTime targetDate, IEnumerable<FileInfo> folderFiles)
{
    //find the master file for a single day

    string masterFormat = String.Format("compiledData{0}.txt",targetDate.ToString("ddMMyy"));
    string masterFormatPath = Directory.GetParent(folderFiles.ElementAt(0).FullName) + "\\" + masterFormat;
    foreach (FileInfo file in folderFiles)
    {
        if (file.Name.Contains(masterFormat))
        {
            Console.WriteLine(masterFormat + " file found in directory");
            return masterFormatPath;
        }
    }

    
    Console.WriteLine(masterFormat + " not found, creating at " + masterFormatPath);
    File.Create(masterFormatPath);

    return masterFormatPath;
}

SortedList<int, string> getSensorsAndIndices(FileInfo dataFile)
{
    SortedList<int, string> indexSensor = new SortedList<int, string>();

    IEnumerable<string> rFile = File.ReadLines(dataFile.FullName);

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


    Console.WriteLine(indexSensor.ElementAt(0));

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

DataTable createMasterHeader(SortedList<int,string> sIndex, string masterFile, DataTable dTable)
{
    //create a header using the unique names of the sennsors
    
    var uniqueValues = sIndex.Where(pair => pair.Value == sIndex.ElementAt(0).Value)
                            .Select(pair => pair.Key);

    List<string> headerLine = new List<string>();

    string firstSensor = sIndex.ElementAt(0).Value;

    headerLine.Add(firstSensor + " time");
    headerLine.Add(firstSensor);

    //create a small array before it repeats the new sensor names
    for (int i = 1;i<= sIndex.Count()/2;i++)
    {
        string item = sIndex.ElementAt(i).Value;
        if (item == firstSensor)
        {
            break;
        } else
        {
            headerLine.Add(item +" time");
            headerLine.Add(item);
        }
           
    }

    Console.WriteLine("Unique Values:");
    foreach (var value in uniqueValues)
    {
        Console.WriteLine(value);
    }

    foreach (string item in headerLine)
    {
        Console.WriteLine(item);
        dTable.Columns.Add(item);
    }

    //Write the header line contents to the dataTable
    Console.WriteLine(headerLine.Count());

    //PrintDataTable(dTable);

    //Write the contents to the DataTable, recall they are split between time and data for each type of sensor

    //Match the column for the sensor time

    //Match the column for the sensor data

    return dTable;
}


DataTable WriteSensorFile(FileInfo dataFile, SortedList<int, string> sIndex)
{
    DataTable dTable = new DataTable();
    IEnumerable<string> rFile = File.ReadLines(dataFile.FullName);

    var firstFrog = sIndex.Where(pair => pair.Value == sIndex.ElementAt(0).Value)
                .Select(pair => pair.Key);

    var secondFrog = sIndex.Where(pair => pair.Value == sIndex.ElementAt(1).Value)
                .Select(pair => pair.Key);

    foreach (var item in firstFrog) { Console.WriteLine(item); }
    foreach (var item in secondFrog) { Console.WriteLine(item); }

    List<string> strings = new List<string>();

    string sensorName = rFile.ElementAt(firstFrog.ElementAt(0)).Split(':')[1];

    for (int i = 0; i < firstFrog.Count()-1;i++)
    {
        strings.AddRange(rFile.Skip(firstFrog.ElementAt(i)+1).Take(secondFrog.ElementAt(i) - firstFrog.ElementAt(i)-2));
    }

    dTable.Columns.Add(sensorName);

    DataRow workRow = dTable.NewRow();

    foreach(string line in strings)
    {
        dTable.Rows.Add(line);
    }

    PrintDataTable(dTable);

    return dTable;
}

DataTable writeSensorData(SortedList<int, string> sIndex,FileInfo dataFile, DataTable dTable)
{
    //Write the contents to the DataTable, recall they are split between time and data for each type of sensor
    IEnumerable<string> rFile = File.ReadLines(dataFile.FullName);

    List<string> tableArray = new List<string>();
    //create row of data that matches the header orientation

    var uniqueValues = sIndex.Where(pair => pair.Value == sIndex.ElementAt(0).Value)
                        .Select(pair => pair.Key);
    int sensorCount = 2;

    string firstSensor = sIndex.ElementAt(0).Value;

    var firstFrog = sIndex.Where(pair => pair.Value == sIndex.ElementAt(0).Value)
                    .Select(pair => pair.Key);

    foreach (var item in firstFrog) { Console.WriteLine(item); }

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
            sensorCount = sensorCount + 2;
        }

    }

    dTable.Clear();

    Console.WriteLine(uniqueValues.Count());


    for (int i = 0; i < sensorCount;i++)
    {
        int targetIndex = sIndex.ElementAt(i).Key;

        tableArray.Add(rFile.ElementAt(targetIndex+1));

        //needs error handling for blank entries within the table
        //different ranges need handling
    }

    //create a column of data for separate sensors, potential even a completely separate data table

    DataRow workRow = dTable.NewRow();

    workRow.ItemArray = (object[])tableArray.ToArray();
    dTable.Rows.Add(workRow);



    tableArray.Clear();

    //Match the column for the sensor data

    string filePath = "C:\\Users\\highf\\Desktop\\CompressedData\\CompiledTestdata.csv";
    StringBuilder sb = new StringBuilder();

    // Add column names
    IEnumerable<string> columnNames = dTable.Columns.Cast<DataColumn>().Select(column => column.ColumnName);
    sb.AppendLine(string.Join(",", columnNames));

    // Add rows
    foreach (DataRow row in dTable.Rows)
    {
        IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
        sb.AppendLine(string.Join(",", fields));
    }

    // Write to file
    File.WriteAllText(filePath, sb.ToString());


    return dTable;
}

SortedList<int, string> writeOrUpdateMasterFile(FileInfo dataFile, string masterFile, DataTable dTable)
{
    //Check the header is correct on the master file
    IEnumerable<string> mFileContent = File.ReadLines(masterFile);

    //Header format is the name of each sensor type, this array is potentially quite large 
    //Presuming that the datafile will be consistently of the same formt perhaps we can create the header while finding the indices
    //and then cross references this

    //Custom data structure likely needed as well

    SortedList<int, string> sIndex = getSensorsAndIndices(dataFile);

    if (sIndex.Count==0) { return sIndex; }


    dTable = createMasterHeader(sIndex, masterFile, dTable);

    //dTable = writeSensorData(sIndex, dataFile, dTable);

    if (mFileContent.Any())
    {

    }else
    {
        Console.WriteLine(masterFile + " has no header...writing");
    }

    Console.WriteLine(sIndex.ElementAt(0));

    return sIndex;

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

    string masterFile = "";

    DirectoryInfo fi = new DirectoryInfo(datafolder);

    IEnumerable<FileInfo> dataFiles = fi.EnumerateFiles();

    foreach (FileInfo dataFile in dataFiles)
    {
        Console.WriteLine(dataFile.Name);

        //match the datafile to a pottential master file

        if(dataFile.Extension == ".txt" && !(dataFile.ToString().Contains("compiled")))
        {
            //all master files must have a timestamp beginning at midnight on the first day of the month

            //check for master file

            //extract the first timestamp and check for a matching master_day file

            IEnumerable<string> rFile = File.ReadLines(dataFile.FullName);
            double firstTime = FindFirstTimestamp(rFile);

            //Conver to a human readable timestamp
            DateTime dt = EpochToTimestamp(firstTime);

            //Get first timestamp

            Console.WriteLine(dt.ToString("dd-MM-yyyy HH:mm:ss:fff"));

            //check for master file for given day (this will later extend to month and year)
            masterFile = FindMasterFile(dt, dataFiles);

            //if no master file extract data and add to file

            DataTable dataTable = new DataTable();

            SortedList<int,string>sIndex = writeOrUpdateMasterFile(dataFile, masterFile, dataTable);

            //writeSensorData(sIndex, dataFile, dataTable);

            WriteSensorFile(dataFile, sIndex);
        }
    }

    return 0;
}

string[] input = ["C:\\Users\\highf\\Desktop\\CompressedData"];

Main(input);