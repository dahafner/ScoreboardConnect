﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;


namespace TP.Data {
  public class CourtData : TpDataObject {
    public int ID { get; set; }
    public string Name { get; set; }
    public int LocationID { get; set; }
    public int TpMatchID { get; set; }
    public CourtData() {
    }
    public CourtData(CourtData cpy) {
      ID = cpy.ID;
      LocationID = cpy.LocationID;
      Name = cpy.Name;
      TpMatchID = cpy.TpMatchID;
    }
    public CourtData(System.Data.IDataReader reader) {
      ID = GetInt(reader, "id");
      Name = GetString(reader, "name");
      LocationID = GetInt(reader, "location");
      TpMatchID = GetInt(reader, "playermatch");
    }

    public CourtData(XmlReader reader) {
      Name = GetString(reader, "CT");
      TpMatchID = GetInt(reader, "ID");
    }
  }
}
