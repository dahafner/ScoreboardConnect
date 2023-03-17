﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Xml;
using ScoreboardLiveApi;

namespace TP.Data {
  public class PlayerMatchData : TpDataObject {
    public enum Winners { None = 0, Entry1 = 1, Entry2 = 2 }
    
    public int ID { get; set; }
    public int DrawID { get; set; }
    public int Planning { get; set; }
    public int EntryID { get; set; }
    public Winners Winner { get; set; }
    public int LinkID { get; set; }
    public DateTime PlanDate { get; set; }
    public int Van1 { get; set; }
    public int Van2 { get; set; }
    public int WN { get; set; }
    public int Team1Set1 { get; set; }
    public int Team1Set2 { get; set; }
    public int Team1Set3 { get; set; }
    public int Team1Set4 { get; set; }
    public int Team1Set5 { get; set; }
    public int Team2Set1 { get; set; }
    public int Team2Set2 { get; set; }
    public int Team2Set3 { get; set; }
    public int Team2Set4 { get; set; }
    public int Team2Set5 { get; set; }
    public int Shuttles { get; set; }
    public int MatchNr { get; set; }

    public bool WalkOver { get; set; }
    public bool Retired { get; set; }

    public PlayerMatchData() { }
    public PlayerMatchData(PlayerMatchData cpy) {
      ID = cpy.ID;
      DrawID = cpy.DrawID;
      Planning = cpy.Planning;
      EntryID = cpy.EntryID;
      Winner = cpy.Winner;
      LinkID = cpy.LinkID;
      PlanDate = cpy.PlanDate;
      Van1 = cpy.Van1;
      Van2 = cpy.Van2;
      WN = cpy.WN;
      Team1Set1 = cpy.Team1Set1;
      Team1Set3 = cpy.Team1Set2;
      Team1Set2 = cpy.Team1Set3;
      Team1Set4 = cpy.Team1Set4;
      Team1Set5 = cpy.Team1Set5;
      Team2Set1 = cpy.Team2Set1;
      Team2Set2 = cpy.Team2Set2;
      Team2Set3 = cpy.Team2Set3;
      Team2Set4 = cpy.Team2Set4;
      Team2Set5 = cpy.Team2Set5;
      Shuttles = cpy.Shuttles;
      MatchNr = cpy.MatchNr;
      WalkOver = cpy.WalkOver;
      Retired = cpy.Retired;
    }

    public PlayerMatchData(IDataReader reader) {
      ID = GetInt(reader, "id");
      DrawID = GetInt(reader, "draw");
      Planning = GetInt(reader, "planning");
      EntryID = GetInt(reader, "entry");
      Winner = (Winners)GetInt(reader, "winner");
      LinkID = GetInt(reader, "link");
      PlanDate = GetDateTime(reader, "plandate");
      Van1 = GetInt(reader, "van1");
      Van2 = GetInt(reader, "van2");
      WN = GetInt(reader, "wn");
      Team1Set1 = GetInt(reader, "team1set1");
      Team1Set2 = GetInt(reader, "team1set2");
      Team1Set3 = GetInt(reader, "team1set3");
      Team1Set4 = GetInt(reader, "team1set4");
      Team1Set5 = GetInt(reader, "team1set5");
      Team2Set1 = GetInt(reader, "team2set1");
      Team2Set2 = GetInt(reader, "team2set2");
      Team2Set3 = GetInt(reader, "team2set3");
      Team2Set4 = GetInt(reader, "team2set4");
      Team2Set5 = GetInt(reader, "team2set5");
      Shuttles = GetInt(reader, "shuttles");
      MatchNr = GetInt(reader, "id");
      WalkOver = GetBool(reader, "walkover");
      Retired = GetBool(reader, "retired");
    }
  }
}
