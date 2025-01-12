using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace TP
{
    public class Draw : TP.Data.DrawData
    {
        public List<Match> Matches { get; private set; } = new List<Match>();
        public List<Link> Links { get; private set; }
        protected Draw(Data.DrawData raw) : base(raw)
        {
            Matches = new List<Match>();
        }
        public static Draw Parse(Data.DrawData raw, IEnumerable<Data.PlayerMatchData> playerMatches, IEnumerable<Entry> entries, IEnumerable<Link> links)
        {
            Draw draw = new Draw(raw);
            if (draw.DrawType == DrawTypes.RoundRobin)
            {
                draw.Matches = Match.ParsePoolDraw(draw, playerMatches, entries, links).ToList();
            }
            else
            {
                draw.Matches = Match.TraverseCupDraw(draw, playerMatches, entries, links).ToList();
            }
            draw.Links = draw.Matches.Select(m => m.Links.Item1).Concat(draw.Matches.Select(m => m.Links.Item2)).Distinct().Where(link => link != null).ToList();
            return draw;
        }

        public static Draw Parse(XmlReader reader, List<Entry> entries)
        {
            Draw draw = new Draw(new Data.DrawData(reader));
            List<Data.PlayerMatchData> playerMatches = new List<Data.PlayerMatchData>();
            while (reader.ReadToFollowing("MATCH"))
            {
                using (var matchXml = reader.ReadSubtree())
                {
                    matchXml.Read();
                    playerMatches.Add(new Data.PlayerMatchData(matchXml)
                    {
                        DrawID = draw.ID,
                        EventID = draw.EventID
                    });
                }
            }
            if (draw.DrawType == DrawTypes.RoundRobin)
            {
                draw.Matches.AddRange(Match.ParsePoolDrawXML(draw, playerMatches, entries));
            }
            else
            {
                draw.Matches.AddRange(Match.TraverseCupDrawXML(draw, playerMatches, entries));
            }
            return draw;
        }

        public IEnumerable<Entry> GetEntries()
        {
            return Matches.Select(match => match.Entries.Item1).Concat(Matches.Select(match => match.Entries.Item2)).Where(entry => entry != null).Distinct();
        }


        /*
            public List<PlayerMatch> Matches { get; private set; } = new List<PlayerMatch>();
            public int EntryCount { get; set; }


            public Draw(XmlReader reader, List<Entry> entries) {
              ID = GetInt(reader, "ID");
              DrawType = (DrawTypes)GetInt(reader, "DRAWTYPE");
              Name = GetString(reader, "DRAWNAME");
              EntryCount = entries.Count;
              while (reader.ReadToFollowing("MATCH")) {
                Matches.Add(new PlayerMatch(reader));
              }
              Matches.ForEach(match => match.Entry = match.EntryID == 0 ? null : entries.Find(entry => entry.ID == match.EntryID));
            }

            public ScoreboardLiveApi.TournamentClass MakeScoreboardClass() {
              return new ScoreboardLiveApi.TournamentClass {
                Description = Name,
                Size = DrawSize(),
                ClassType = DrawType switch {
                  DrawTypes.RoundRobin => "roundrobin",
                  _ => "cup"
                }
              };
            }

            public int DrawSize() {
              return Matches.FindAll(match => match.Entry != null).Select(match => match.Entry).Distinct().Count() +
                     Matches.FindAll(match => match.Link != null).Count();
            }
        */

        public override string ToString()
        {
            return string.Format("DRAW {0}\t{1}\t{2}", ID, Name, DrawType);
            //return string.Format("DRAW {0}\t{1}\t{2}\t({3} matches)", ID, Name, DrawType, Matches.Count);
        }
    }
}
