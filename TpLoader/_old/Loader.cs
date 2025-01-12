using System;
using System.Collections.Generic;
using System.Text;
using TP.Data;
using System.Data;

namespace TP
{
    public class Loader
    {
        private static List<T> LoadStuff<T>(IDbCommand cmd, Func<IDataReader, T> parseDbData)
        {
            List<T> result = new List<T>();
            using (IDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    T item = parseDbData(reader);
                    result.Add(item);
                }
            }
            return result;
        }

        public static List<TournamentInformation> LoadTournamentInformation(IDbCommand cmd)
        {
            cmd.CommandText = "SELECT * FROM TournamentInformation";
            List<TournamentInformation> tournaments = LoadStuff<TournamentInformation>(cmd, reader => new TournamentInformation(reader));
            return tournaments;
        }

        public static List<LinkData> LoadLinks(IDbCommand cmd)
        {
            // Get links
            cmd.CommandText = "SELECT * FROM Link";
            List<LinkData> links = LoadStuff<LinkData>(cmd, reader => new LinkData(reader));
            // Return
            return links;
        }

        public static List<LocationData> LoadLocations(IDbCommand cmd)
        {
            cmd.CommandText = "SELECT * FROM Location";
            return LoadStuff(cmd, reader => new LocationData(reader));
        }

        public static List<CourtData> LoadCourts(IDbCommand cmd)
        {
            cmd.CommandText = "SELECT * FROM Court";
            var courts = LoadStuff(cmd, reader => new CourtData(reader));
            var locations = LoadLocations(cmd);
            //courts.ForEach(c => c.Location = locations.Find(l => l.ID == c.LocationID));
            return courts;
        }

        public static List<EventData> LoadEvents(IDbCommand cmd)
        {
            // Get draws
            var draws = LoadDraws(cmd);
            // Get players
            var players = LoadPlayers(cmd);
            // Get tournament informations
            var tournaments = LoadTournamentInformation(cmd);
            // Get events
            cmd.CommandText = "SELECT * FROM Event";
            List<EventData> events = LoadStuff<EventData>(cmd, reader => new EventData(reader));
            //events.ForEach(e => {
            //  e.Draws.AddRange(draws.FindAll(d => d.EventID == e.ID));
            //  e.TournamentInformation = tournaments.Find(t => t.ID == e.TournamentInformationID);
            // });
            return events;
        }

        private static List<DrawData> LoadDraws(IDbCommand cmd)
        {
            cmd.CommandText = "SELECT * FROM Draw";
            var draws = LoadStuff<DrawData>(cmd, reader => new DrawData(reader));
            var matches = LoadMatches(cmd);
            //draws.ForEach(draw => draw.Matches.AddRange(matches.FindAll(match => match.DrawID == draw.ID)));
            return draws;
        }

        public static List<EntryData> LoadEntries(IDbCommand cmd)
        {
            cmd.CommandText = "SELECT * FROM Entry";
            var entries = LoadStuff<EntryData>(cmd, reader => new EntryData(reader));
            var players = LoadPlayers(cmd);
            //entries.ForEach(entry => {
            //  if (entry.Player1ID > 0) {
            //    entry.Player1 = players.Find(p => p.ID == entry.Player1ID);
            //  }
            //  if (entry.Player2ID > 0) {
            //    entry.Player2 = players.Find(p => p.ID == entry.Player2ID);
            //  }
            //});
            return entries;
        }

        public static List<PlayerData> LoadPlayers(IDbCommand cmd)
        {
            cmd.CommandText = "SELECT * FROM Player";
            var players = LoadStuff<PlayerData>(cmd, (reader) => new PlayerData(reader));
            var clubs = LoadClubs(cmd);
            //players.ForEach(player => player.Club = clubs.Find(club => club.ID == player.ClubID));
            return players;
        }

        private static List<ClubData> LoadClubs(IDbCommand cmd)
        {
            cmd.CommandText = "SELECT * FROM Club";
            return LoadStuff<ClubData>(cmd, reader => new ClubData(reader));
        }

        private static List<PlayerMatchData> LoadMatches(IDbCommand cmd)
        {
            cmd.CommandText = "SELECT * FROM PlayerMatch";
            var matches = LoadStuff<PlayerMatchData>(cmd, reader => new PlayerMatchData(reader));
            var entries = LoadEntries(cmd);
            var links = LoadLinks(cmd);
            //matches.ForEach(match => match.Entry = match.EntryID == 0 ? null : entries.Find(entry => entry.ID == match.EntryID));
            //matches.ForEach(match => match.Link = match.LinkID == 0 ? null : links.Find(link => link.ID == match.LinkID));
            return matches;
        }
    }
}
