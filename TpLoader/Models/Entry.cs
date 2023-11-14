﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Linq;

namespace TP {
  public class Entry : TP.Data.EntryData {
    public Player Player1 { get; set; }
    public Player Player2 { get; set; }

    protected Entry(Data.EntryData raw, Player player1, Player player2 = null) : base(raw) {
      Player1 = player1;
      Player2 = player2;
    }

    public static Entry Parse(Data.EntryData raw, IEnumerable<Player> players) {
      Player player1 = players.First(p => p.ID == raw.Player1ID);
      Player player2 = players.FirstOrDefault(p => p.ID == raw.Player2ID);
      if (player1 == null) {
        throw new Exception("Entry player not found");
      }
      return new Entry(raw, player1, player2);
    }

    public static Entry Parse(XmlReader reader) {
      string name1 = GetString(reader, "NAME1");
      string club1 = GetString(reader, "CLUB1");
      string name2 = GetString(reader, "NAME2");
      string club2 = GetString(reader, "CLUB2");
      return new Entry(new Data.EntryData(reader),
                       new Player(name1, club1),
                       string.IsNullOrWhiteSpace(name2) ? null : new Player(name2, club2)
      );
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder();
      sb.AppendFormat("ENTRY {0}:{1}", ID, Environment.NewLine);
      sb.AppendFormat("\t{0}{1}", Player1 == null ? "<no player found>" : Player1.ToString(), Environment.NewLine);
      if (Player2 != null) {
        sb.AppendFormat("\t{0}{1}", Player2.ToString(), Environment.NewLine);
      }
      return sb.ToString();
    }
  }
}
