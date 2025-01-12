using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace TP.Data
{
    public class EventData : TpDataObject
    {
        public enum EventTypes { Singles = 1, Doubles = 2 }
        public enum Genders { Unknown = 0, Men = 1, Women = 2, Mixed = 3, Boys = 4, Girls = 5 }

        public int ID { get; set; }
        public int TournamentInformationID { get; set; }
        public string Name { get; set; }
        public EventTypes EventType { get; set; } = EventTypes.Doubles;
        public Genders Gender { get; set; } = Genders.Unknown;
        public EventData() { }
        public EventData(EventData cpy)
        {
            ID = cpy.ID;
            TournamentInformationID = cpy.TournamentInformationID;
            Name = cpy.Name;
            EventType = cpy.EventType;
            Gender = cpy.Gender;
        }
        public EventData(System.Data.IDataReader reader)
        {
            ID = GetInt(reader, "id");
            Name = GetString(reader, "name");
            Gender = (Genders)GetInt(reader, "gender");
            EventType = (EventTypes)GetInt(reader, "eventtype");
            TournamentInformationID = GetInt(reader, "tournamentinformationid");
        }
        public EventData(XmlReader reader)
        {
            ID = GetInt(reader, "ID");
            Name = GetString(reader, "NAME");
        }
    }
}
