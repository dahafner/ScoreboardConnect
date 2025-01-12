using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace TP.Data
{
    public class EntryData : TpDataObject
    {
        public int ID { get; set; }
        public int Player1ID { get; set; }
        public int Player2ID { get; set; }
        public EntryData() { }
        public EntryData(EntryData cpy)
        {
            ID = cpy.ID;
            Player1ID = cpy.Player1ID;
            Player2ID = cpy.Player2ID;
        }
        public EntryData(System.Data.IDataReader reader)
        {
            ID = GetInt(reader, "id");
            Player1ID = GetInt(reader, "player1");
            Player2ID = GetInt(reader, "player2");
        }

        public EntryData(XmlReader reader)
        {
            ID = GetInt(reader, "ID");
        }
    }
}
