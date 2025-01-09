using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Cryptography;

namespace TP
{
    public partial class Tournament
    {
        private static readonly string NAMEFORMAT = "{0} {1}";
        private static Helpers.PlaceMap PlaceMap = new Helpers.PlaceMap();

        public class ExportClassItem
        {
            public ScoreboardLiveApi.TournamentClass SBClass { get; }
            public Draw TPDraw { get; }
            public Event TPEvent { get; }
            public List<ExportClassItem> SubClasses { get; } = new List<ExportClassItem>();
            public List<ExportMatchItem> Matches { get; } = new List<ExportMatchItem>();
            public ExportClassItem(ScoreboardLiveApi.TournamentClass mainClass, Event tpEvent, Draw mainDraw, IEnumerable<ExportClassItem> subClasses = null, IEnumerable<ExportMatchItem> matches = null)
            {
                SBClass = mainClass;
                TPDraw = mainDraw;
                TPEvent = tpEvent;
                if (subClasses != null)
                {
                    SubClasses.AddRange(subClasses);
                }
                if (matches != null)
                {
                    Matches.AddRange(matches);
                }
            }
        }

        public class ExportMatchItem
        {
            public ScoreboardLiveApi.MatchExtended SB { get; set; }
            public Match TP { get; set; }
        }

        public IEnumerable<ExportClassItem> ExportClasses(Func<Event, bool> eventFilter)
        {
            List<ExportClassItem> exportItems = new List<ExportClassItem>();
            foreach (Event ev in Events.Where(eventFilter))
            {
                var rootDraw = FindMainDraw(ev);
                var rootClass = ConvertDraw(ev, rootDraw).First();

                var subDraws = FindSubDraws(ev, rootDraw);
                var subClasses = new List<ExportClassItem>();
                foreach (Draw subDraw in subDraws)
                {
                    subClasses.AddRange(ConvertDraw(ev, subDraw));
                }

                rootClass.SubClasses.AddRange(subClasses);

                exportItems.Add(rootClass);
            }
            return exportItems;
        }

        private static List<ExportClassItem> ConvertDraw(Event ev, Draw draw)
        {
            List<ExportClassItem> classItems = new List<ExportClassItem>();

            if (draw.DrawType == Data.DrawData.DrawTypes.QualifierCups)
            {
                // This draw can have multiple classes
                var rootMatches = draw.Matches.Where(match => match.WN == 0).OrderBy(match => match.Planning);
                int i = 1;
                foreach (var rootMatch in rootMatches)
                {
                    var classItem = new ExportClassItem(CreateClass(ev, draw), ev, draw);
                    classItem.SBClass.Description += string.Format(" {0}", i++);
                    classItem.Matches.AddRange(ConvertDrawMatches(draw, classItem.SBClass, rootMatch));
                    classItem.SBClass.Size = GetNearestHigherPowerOfTwo(CalculateClassSize(classItem.Matches));
                    classItems.Add(classItem);
                }
            }
            else
            {
                // This draw should have only one class
                var sbClass = CreateClass(ev, draw);
                var matches = ConvertDrawMatches(draw, sbClass);
                sbClass.Size = CalculateClassSize(matches);
                classItems.Add(new ExportClassItem(sbClass, ev, draw, matches: matches));
            }
            return classItems;
        }

        private static ScoreboardLiveApi.TournamentClass CreateClass(Event ev, Draw draw)
        {
            return new ScoreboardLiveApi.TournamentClass()
            {
                Category = CreateMatchCategoryString(ev),
                Description = draw.Name,
                ClassType = CreateClassType(draw),
            };
        }

        private static IEnumerable<ExportMatchItem> ConvertDrawMatches(Draw draw, ScoreboardLiveApi.TournamentClass sbClass, Match rootMatch = null)
        {
            if (draw.DrawType == Data.DrawData.DrawTypes.RoundRobin)
            {
                return draw.Matches.Select(tpMatch =>
                {
                    return new ExportMatchItem()
                    {
                        SB = ConvertMatch(tpMatch, sbClass.Category),
                        TP = tpMatch
                    };
                });
            }
            return TraverseMatchTree(draw, sbClass, rootMatch);
        }

        private static List<ExportMatchItem> TraverseMatchTree(Draw draw, ScoreboardLiveApi.TournamentClass sbClass, Match rootMatch)
        {
            List<ExportMatchItem> matches = new List<ExportMatchItem>();
            Match root = rootMatch ?? draw.Matches.FirstOrDefault(match => match.WN == 0);
            if (root == null)
            {
                throw new Exception("No root match could be found in draw");
            }
            ParseCupMatch(root, sbClass, matches);

            // Remove all matches without valid roots
            int maxTierPlanning = (matches.Max(match => match.TP.Planning) / 1000) * 1000;
            return matches.FindAll(emi => (emi.TP.Planning < maxTierPlanning) ||
              (((emi.TP.Entries.Item1 != null) || (emi.TP.Links.Item1 != null)) && ((emi.TP.Entries.Item2 != null) || (emi.TP.Links.Item2 != null)))
            );
        }

        private static void ParseCupMatch(TP.Match tpMatch, ScoreboardLiveApi.TournamentClass sbClass, List<ExportMatchItem> items, int column = 1, int startRow = 1)
        {
            ScoreboardLiveApi.MatchExtended sbMatch = ConvertMatch(tpMatch, sbClass.Category);
            sbMatch.Place = PlaceMap.GetPlace(column * 1000 + startRow);

            items.Add(new ExportMatchItem()
            {
                SB = sbMatch,
                TP = tpMatch
            });

            if (tpMatch.Source.Item1 != null)
            {
                ParseCupMatch(tpMatch.Source.Item1, sbClass, items, column + 1, startRow * 2 - 1);
            }
            if (tpMatch.Source.Item2 != null)
            {
                ParseCupMatch(tpMatch.Source.Item2, sbClass, items, column + 1, startRow * 2);
            }
        }

        public static ScoreboardLiveApi.MatchExtended ConvertMatch(TP.Match tpMatch, string category)
        {
            var entryTextsTeam1 = tpMatch.Entries.Item1 != null ? ConvertEntry(tpMatch.Entries.Item1) : ("", "", "", "");
            var entryTextsTeam2 = tpMatch.Entries.Item2 != null ? ConvertEntry(tpMatch.Entries.Item2) : ("", "", "", "");

            ScoreboardLiveApi.MatchExtended sbMatch = new ScoreboardLiveApi.MatchExtended()
            {
                Category = category,
                StartTime = tpMatch.PlanDate,
                TournamentMatchNumber = tpMatch.MatchNr,
                BallCount = tpMatch.Shuttles,
                Team1Set1 = tpMatch.Team1Set1,
                Team1Set2 = tpMatch.Team1Set2,
                Team1Set3 = tpMatch.Team1Set3,
                Team1Set4 = tpMatch.Team1Set4,
                Team1Set5 = tpMatch.Team1Set5,
                Team2Set1 = tpMatch.Team2Set1,
                Team2Set2 = tpMatch.Team2Set2,
                Team2Set3 = tpMatch.Team2Set3,
                Team2Set4 = tpMatch.Team2Set4,
                Team2Set5 = tpMatch.Team2Set5,
                Team1Player1Name = entryTextsTeam1.Item1,
                Team1Player1Team = entryTextsTeam1.Item2,
                Team1Player2Name = entryTextsTeam1.Item3,
                Team1Player2Team = entryTextsTeam1.Item4,
                Team2Player1Name = entryTextsTeam2.Item1,
                Team2Player1Team = entryTextsTeam2.Item2,
                Team2Player2Name = entryTextsTeam2.Item3,
                Team2Player2Team = entryTextsTeam2.Item4,
            };

            if (tpMatch.WalkOver || tpMatch.Retired)
            {
                sbMatch.Status = tpMatch.Winner switch
                {
                    Data.PlayerMatchData.Winners.Entry1 => "team1won",
                    Data.PlayerMatchData.Winners.Entry2 => "team2won",
                    _ => "nowinner"
                };
                sbMatch.Special = tpMatch.WalkOver ? "walkover" : "retired";
            }

            return sbMatch;
        }

        public static (string, string, string, string) ConvertEntry(TP.Entry entry)
        {
            return (
              entry.Player1 == null ? "" : string.Format(NAMEFORMAT, entry.Player1.FirstName, entry.Player1.LastName),
              entry.Player1 == null ? "" : (entry.Player1.Club?.Name ?? ""),
              entry.Player2 == null ? "" : string.Format(NAMEFORMAT, entry.Player2.FirstName, entry.Player2.LastName),
              entry.Player2 == null ? "" : (entry.Player2.Club?.Name ?? "")
            );
        }

        public static string CreateMatchCategoryString(Event ev)
        {
            return ev.Gender switch
            {
                Event.Genders.Men => ev.EventType == Event.EventTypes.Singles ? "ms" : "md",
                Event.Genders.Boys => ev.EventType == Event.EventTypes.Singles ? "ms" : "md",
                Event.Genders.Women => ev.EventType == Event.EventTypes.Singles ? "ws" : "wd",
                Event.Genders.Girls => ev.EventType == Event.EventTypes.Singles ? "ws" : "wd",
                _ => "xd",
            };
        }

        protected static int CalculateClassSize(IEnumerable<ExportMatchItem> matchItems)
        {
            // Count number of unique entries
            int entryCount = matchItems.Select(mi => mi.TP.Entries.Item1?.ID)
                        .Concat(matchItems.Select(mi => mi.TP.Entries.Item2?.ID))
                        .Where(id => id.GetValueOrDefault() > 0).Distinct().Count();
            // Get link count
            int linkCount = matchItems.Select(mi => mi.TP.Links.Item1?.ID)
                        .Concat(matchItems.Select(mi => mi.TP.Links.Item2?.ID))
                        .Where(id => id.GetValueOrDefault() > 0).Distinct().Count();
            // Return sum
            return entryCount + linkCount;
        }

        protected static int GetNearestHigherPowerOfTwo(int size)
        {
            int power = 0;
            while (size > (1 << power))
            {
                ++power;
            }
            return 1 << power;
        }

        protected static string CreateClassType(Draw draw)
        {
            return draw.DrawType switch
            {
                Draw.DrawTypes.RoundRobin => "roundrobin",
                _ => "cup"
            };
        }

        private static Draw FindMainDraw(Event ev)
        {
            // If there is a playoff cup in the draw list, use that, if not make sure there
            // is only one draw and use that. If there is more then one draw, but no playoff, there
            // is no way of knowing which to use so throw.
            var root = ev.Draws.FirstOrDefault(d => d.DrawType == Data.DrawData.DrawTypes.PlayOffCup);
            if (root != null)
            {
                return root;
            }
            else if (ev.Draws.Count() == 1)
            {
                return ev.Draws.First();
            }
            throw new Exception("Could not determine root draw in event");
        }

        private static IEnumerable<Draw> FindSubDraws(Event ev, Draw root)
        {
            return ev.Draws.Where(draw => draw.ID != root.ID);
        }
        private static IEnumerable<ScoreboardLiveApi.Link> CreateLinks(IEnumerable<ExportClassItem> ecis)
        {
            List<ScoreboardLiveApi.Link> sbLinks = new List<ScoreboardLiveApi.Link>();
            foreach (ExportClassItem eci in ecis)
            {
                var linkTargets = FindLinkTargetsInClass(eci);
                foreach (ExportMatchItem emi in linkTargets)
                {
                    if (emi.TP.Links.Item1 != null)
                    {
                        sbLinks.Add(ConvertLink(emi.TP.Links.Item1, FindLinkSource(emi.TP.Links.Item1, ecis), emi.SB, 1));
                    }
                    if (emi.TP.Links.Item2 != null)
                    {
                        sbLinks.Add(ConvertLink(emi.TP.Links.Item2, FindLinkSource(emi.TP.Links.Item2, ecis), emi.SB, 2));
                    }
                }
            }
            return sbLinks;
        }

        private static IEnumerable<ExportMatchItem> FindLinkTargetsInClass(ExportClassItem eci)
        {
            return eci.Matches.Where(emi => (emi.TP.Links.Item1 != null) || (emi.TP.Links.Item2 != null));
        }

        private static ScoreboardLiveApi.TournamentClass FindLinkSource(Link link, IEnumerable<ExportClassItem> classes)
        {
            return classes.Aggregate(new List<ExportClassItem>(), (c, next) =>
            {
                c.Add(next);
                if (next.SubClasses != null)
                {
                    c.AddRange(next.SubClasses);
                }
                return c;
            }).Where(c => c.TPDraw.ID == link.SourceDrawID)
            .ElementAt(link.GetSourceClassIndex())
            .SBClass;
        }

        private static ScoreboardLiveApi.Link ConvertLink(Link tpLink, ScoreboardLiveApi.TournamentClass tpSourceClass, ScoreboardLiveApi.Match sbTargetMatch, int targetTeamIndex)
        {
            return new ScoreboardLiveApi.Link()
            {
                SourceClassID = tpSourceClass.ID,
                SourcePlace = tpLink.GetSourcePlacement(),
                TargetMatchID = sbTargetMatch.MatchID,
                TargetTeam = targetTeamIndex == 1 ? "team1" : "team2"
            };
        }

        public static string CreateMatchTag(ScoreboardLiveApi.Tournament sbTournament, Match match, Event e, Draw draw)
        {
            string source = string.Format("{0} ?? {1}.{2} ?? {3}.{4} ?? {5}", match.ID, e.ID, e.Name, draw.ID, draw.Name, sbTournament.TournamentID);
            string hash;
            using (SHA256 sha = SHA256.Create())
            {
                hash = ScoreboardLiveApi.ApiHelper.ByteArrayToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(source)));
            }
            return hash;
        }
    }
}
