using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace influxConnect.Calculation
{
    public  class dataPoint
    {
        public string name;
        public string jobID;
        public string sensorID;
        public List<infPoint> points = new List<infPoint>();

        public dataPoint(string name, string jobID, string sneosrID)
        {
            this.name = name;
            this.jobID = jobID;
            this.sensorID = sneosrID;
            this.points = new List<infPoint>();
        }

        internal string returnStringData()
        {
            StringBuilder sb = new StringBuilder();
            foreach(infPoint p in this.points)
            {
                sb.Append(p.date + "," + p.value+ Environment.NewLine);
            }

            return sb.ToString();
        }
    }

    public class infPoint
    {
        public long date;
        public double value;

        public infPoint(long date, double value)
        {
            this.date = date;
            this.value = value;
        }
    }
}
