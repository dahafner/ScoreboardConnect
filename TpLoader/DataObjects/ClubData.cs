using System;
using System.Collections.Generic;
using System.Text;

namespace TP.Data
{
    public class ClubData : TpDataObject
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public ClubData()
        {

        }
        public ClubData(System.Data.IDataReader reader)
        {
            ID = GetInt(reader, "id");
            Name = GetString(reader, "name");
        }
    }
}
