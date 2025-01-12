using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace TP
{
    public class Court : TP.Data.CourtData
    {
        public Data.LocationData Location { get; set; }
        protected Court(Data.CourtData raw, Data.LocationData location) : base(raw)
        {
            Location = location;
        }
        public static Court Parse(Data.CourtData raw, IEnumerable<Data.LocationData> locations)
        {
            Data.LocationData location = locations.First(location => location.ID == raw.LocationID);
            if (location == null)
            {
                throw new Exception("Court location not specified or found.");
            }
            return new Court(raw, location);
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Name, Location.Name);
        }
    }
}
