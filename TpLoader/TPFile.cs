using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Odbc;
using System.Threading.Tasks;

namespace TP
{
    public class TPFile
    {
        public OdbcConnection Connection { get; private set; }
        private object connectionLock = new object();

        public TPFile(string filename)
        {
            string password = "d4R2GY76w2qzZ";
            OdbcConnectionStringBuilder connectionStringBuilder = new OdbcConnectionStringBuilder();
            connectionStringBuilder.Driver = "Microsoft Access Driver (*.mdb, *.accdb)";
            connectionStringBuilder.Add("Dbq", filename);
            connectionStringBuilder.Add("Uid", "Admin");
            connectionStringBuilder.Add("Pwd", password);
            lock (connectionLock)
            {
                Connection = new OdbcConnection(connectionStringBuilder.ConnectionString);
                Connection.Open();
            }
        }

        public void Close()
        {
            lock (connectionLock)
            {
                if ((Connection != null) && (Connection.State == System.Data.ConnectionState.Open))
                {
                    Connection.Close();
                }
            }
        }
        private Task<List<T>> LoadStuff<T>(string sql, Func<IDataReader, T> parseDbData)
        {
            lock (connectionLock)
            {
                if (Connection?.State != ConnectionState.Open)
                {
                    throw new Exception("Could not read file; connection closed");
                }
            }
            return Task.Run(() =>
            {
                List<T> result = new List<T>();
                lock (connectionLock)
                {
                    using (IDbCommand cmd = Connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        using (IDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                T item = parseDbData(reader);
                                result.Add(item);
                            }
                        }
                    }
                }
                return result;
            });
        }

        public async Task<List<Data.TournamentInformation>> LoadTournamentInformation()
        {
            return await LoadStuff("SELECT * FROM TournamentInformation", reader => new Data.TournamentInformation(reader));
        }

        public async Task<List<Data.LinkData>> LoadLinks()
        {
            return await LoadStuff("SELECT * FROM Link", reader => new Data.LinkData(reader));
        }

        public async Task<List<Data.LocationData>> LoadLocations()
        {
            return await LoadStuff("SELECT * FROM Location", reader => new Data.LocationData(reader));
        }

        public async Task<List<Data.CourtData>> LoadCourts()
        {
            return await LoadStuff("SELECT * FROM Court", reader => new Data.CourtData(reader));
        }

        public async Task<List<Data.EventData>> LoadEvents()
        {
            return await LoadStuff("SELECT * FROM Event", reader => new Data.EventData(reader));
        }

        public async Task<List<Data.DrawData>> LoadDraws()
        {
            return await LoadStuff("SELECT * FROM Draw", reader => new Data.DrawData(reader));
        }

        public async Task<List<Data.EntryData>> LoadEntries()
        {
            return await LoadStuff("SELECT * FROM Entry", reader => new Data.EntryData(reader));
        }

        public async Task<List<Data.PlayerData>> LoadPlayers()
        {
            return await LoadStuff("SELECT * FROM Player", (reader) => new Data.PlayerData(reader));
        }

        public async Task<List<Data.ClubData>> LoadClubs()
        {
            return await LoadStuff("SELECT * FROM Club", reader => new Data.ClubData(reader));
        }

        public async Task<List<Data.PlayerMatchData>> LoadPlayerMatches()
        {
            return await LoadStuff("SELECT * FROM PlayerMatch", reader => new Data.PlayerMatchData(reader));
        }
    }
}
