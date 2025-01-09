using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.IO;

namespace ScoreboardConnectWinUI3
{
    [Serializable]
    class Settings : ISerializable
    {
        public int UnitID { get; set; }
        public string URL { get; set; }
        public Dictionary<int, int> SelectedTournaments { get; private set; }
        public Dictionary<int, Dictionary<int, string>> CourtSetup { get; private set; }

        public Settings()
        {
            SelectedTournaments = new Dictionary<int, int>();
            CourtSetup = new Dictionary<int, Dictionary<int, string>>();
        }

        public Settings(string url) : this()
        {
            URL = url;
        }

        public void Save(string filename)
        {
            FileStream stream = new FileStream(filename, FileMode.Create);
            using (stream)
            {
                //System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                //formatter.Serialize(stream, this);
            }
        }

        public static Settings Load(string filename)
        {
            if (!File.Exists(filename))
            {
                return new Settings("https://www.scoreboardlive.se");
            }
            FileStream stream = new FileStream(filename, FileMode.Open);
            using (stream)
            {
                //System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                //return (Settings)formatter.Deserialize(stream);
                return null;
            }
        }

        #region Serialization
        private const int c_version = 5;
        private const string c_id_version = "Settings.version";
        private const string c_id_unitid = "Settings.unitid";
        private const string c_id_url = "Settings.url";
        private const string c_id_selected_tournaments = "Settings.selected_tournaments";
        private const string c_id_court_setup = "Settings.court_setup";

        protected Settings(SerializationInfo info, StreamingContext context) : this()
        {
            int version = info.GetInt32(c_id_version);
            UnitID = info.GetInt32(c_id_unitid);
            URL = info.GetString(c_id_url);
            if (version > 2)
            {
                SelectedTournaments = (Dictionary<int, int>)info.GetValue(c_id_selected_tournaments, typeof(Dictionary<int, int>));
            }
            if (version > 4)
            {
                CourtSetup = (Dictionary<int, Dictionary<int, string>>)info.GetValue(c_id_court_setup, typeof(Dictionary<int, Dictionary<int, string>>));
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(c_id_version, c_version);
            info.AddValue(c_id_unitid, UnitID);
            info.AddValue(c_id_url, URL);
            info.AddValue(c_id_selected_tournaments, SelectedTournaments);
            info.AddValue(c_id_court_setup, CourtSetup);
        }
        #endregion
    }
}

