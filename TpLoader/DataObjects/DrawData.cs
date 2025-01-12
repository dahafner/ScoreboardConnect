using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using ScoreboardLiveApi;
using System.Linq;

namespace TP.Data
{
    public class DrawData : TpDataObject
    {
        public enum DrawTypes { PlayOffCup = 1, RoundRobin = 2, QualifierCups = 6 }
        public int ID { get; set; }
        public string Name { get; set; }
        public DrawTypes DrawType { get; set; }
        public int EventID { get; set; }
        public int DrawEndSize { get; set; }
        public DrawData() { }
        public DrawData(DrawData cpy)
        {
            ID = cpy.ID;
            Name = cpy.Name;
            DrawType = cpy.DrawType;
            EventID = cpy.EventID;
            DrawEndSize = cpy.DrawEndSize;
        }
        public DrawData(System.Data.IDataReader reader)
        {
            ID = GetInt(reader, "id");
            Name = GetString(reader, "name");
            DrawType = (DrawTypes)GetInt(reader, "drawType");
            EventID = GetInt(reader, "event");
            DrawEndSize = GetInt(reader, "drawendsize");
        }

        public DrawData(XmlReader reader)
        {
            ID = GetInt(reader, "ID");
            Name = GetString(reader, "DRAWNAME");
            DrawType = (DrawTypes)GetInt(reader, "DRAWTYPE");
            EventID = GetInt(reader, "EVENT");
            DrawEndSize = GetInt(reader, "DRAWENDSIZE");
        }
    }
}
