using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace TP.Data
{
    public class TournamentInformation : TpDataObject
    {
        public int ID { get; set; }
        public string TournamentID { get; set; }
        public string TournamentName { get; set; }

        public TournamentInformation() { }
        public TournamentInformation(System.Data.IDataReader reader)
        {
            ID = GetInt(reader, "id");
            TournamentID = GetString(reader, "tournamentid");
            TournamentName = GetString(reader, "tournamentname");
        }
    }
}
