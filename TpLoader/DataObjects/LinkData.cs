using System;
using System.Collections.Generic;
using System.Text;

namespace TP.Data
{
    public class LinkData : TpDataObject
    {
        public int ID { get; set; }
        public int SourceDrawID { get; set; }
        public int SourcePosition { get; set; }
        public LinkData() { }
        public LinkData(LinkData cpy)
        {
            ID = cpy.ID;
            SourceDrawID = cpy.SourceDrawID;
            SourcePosition = cpy.SourcePosition;
        }
        public LinkData(System.Data.IDataReader reader)
        {
            ID = GetInt(reader, "id");
            SourceDrawID = GetInt(reader, "src_draw");
            SourcePosition = GetInt(reader, "src_pos");
        }
    }
}
