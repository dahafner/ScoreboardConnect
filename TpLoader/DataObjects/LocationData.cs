using System;
using System.Collections.Generic;
using System.Text;

namespace TP.Data
{
    public class LocationData : TpDataObject
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public LocationData(System.Data.IDataReader reader)
        {
            ID = GetInt(reader, "id");
            Name = GetString(reader, "name");
        }
    }
}
