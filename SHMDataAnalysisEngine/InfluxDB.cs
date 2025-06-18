using influxConnect.Calculation;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core.Flux.Domain;
using InfluxDB.Client.Writes;
using Newtonsoft.Json.Linq;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using System.Windows.Forms;

namespace influxConnect
{
    class InfluxDB
    {
        InfluxDBClient client;
        public InfluxDB(string address, string token) 
        {
            client = new InfluxDBClient(address, token);

            testCon();

        }

        public async void testCon()
        {
            var health = await client.PingAsync();
            System.Diagnostics.Debug.WriteLine($"InfluxDB Health: {health}");

        }

        public async Task<List<dataPoint>> returnData(long unixFrom, long unixTo, string org, string bucket, string[] sensors, int step, int stepMax)
        {
            try
            {
                string flux = "from(bucket:\"" + bucket + "\") |> range(start: time(v: " + unixFrom + "),  stop: time(v: " + unixTo + "))";
                if (sensors.Length > 0 && sensors[0].Length > 0)
                {
                    flux += "|> filter(fn: (r) => r[\"_measurement\"] == \"" + sensors[0] + "\" ";

                    for (int i = 1; i < sensors.Length; i++)
                    {
                        flux += " or r[\"_measurement\"] == \"" + sensors[i] + "\"";
                    }

                    flux += ")";
                }

                Console.WriteLine(flux);

                var fluxTables = await client.GetQueryApi().QueryAsync(flux, org);
                List<dataPoint> data = new List<dataPoint>();
                Console.WriteLine("DataReceived");
                fluxTables.ForEach(fluxTable =>
                {
                    var fluxRecords = fluxTable.Records;
                    //Console.WriteLine(fluxRecords[0].Row.Count + ": " + fluxRecords[0]);
                    if (fluxRecords[0].Row.Count == 10)
                    {
                        data.Add(new dataPoint(fluxRecords[0].GetMeasurement(), fluxRecords[0].Row[8].ToString(), fluxRecords[0].Row[9].ToString()));
                        for (int i = 1; i < fluxRecords.Count; i++)
                        {
                            data[data.Count - 1].points.Add(new infPoint(((Instant)fluxRecords[i].GetTime()).ToUnixTimeMilliseconds(), Convert.ToDouble(fluxRecords[i].GetValue())));
                        }
                    }
                });

               // MessageBox.Show("Influx Data Received " + step + "/" + stepMax);

                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error at InfluxDB data retreive: " + ex);
                return null;
            }


        }

        internal void releaseDataBase()
        {
            client.Dispose();
        }
    }
}
