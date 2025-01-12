using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScoreboardLiveApi;
using System.Threading.Tasks;
using System.Data.Odbc;
using System.Security.Cryptography;
using TP.Helpers;

namespace TP {
  public class Converter2 {
    static readonly PlaceMap Places = new PlaceMap();

    private class IntermediateLink {
      public Draw SourceDraw { get; set; }
      public int SourcePlace { get; set; }
      public Draw TargetDraw { get; set; }
      public MatchExtended TargetMatch { get; set; }
      public string TeamIdentifier { get; set; }
    }

    public static Task<List<Event>> ExtractEvents(System.Data.IDbConnection dbConnection) {
      return Task.Run(() => {
        return Loader.LoadEvents(dbConnection.CreateCommand());
      });
    }
 
    public static Task<List<TournamentClass>> Extract(System.Data.IDbConnection dbConnection) {
      return Task.Run(() => {
        var events = Loader.LoadEvents(dbConnection.CreateCommand());
        return Extract(events);
      });
    }

    public static List<TournamentClass> Extract(List<Event> events) {
      var tpClasses = new List<TournamentClass>();
      var errors = new List<Exception>();
      foreach (Event e in events) {
        tpClasses.Add(ExtractClass(e));
      }
      return tpClasses;
    }

    public static Task<List<TP.Court>> ExtractCourts(System.Data.IDbConnection dbConnection) {
      return Task.Run(() => {
        var errors = new List<Exception>();
        var courts = new List<TP.Court>();
        courts.AddRange(Loader.LoadCourts(dbConnection.CreateCommand()));
        return courts;
      });
    }

    public static TournamentClass ExtractClass(Event e) {
      // Find root draw. If there is a playoff cup in the event, use that,
      // otherwise if there is only one draw, use that.
      var root = e.Draws.Find(d => d.DrawType == Draw.DrawTypes.PlayOffCup);
      if ((root == null) && (e.Draws.Count == 1)) {
        root = e.Draws[0];
      } else if (root == null) {
        throw new Exception("No root draw in event");
      }
      // List with all links
      var links = new List<IntermediateLink>();
      // Create class
      var tpClass = root.DrawType == Draw.DrawTypes.RoundRobin ? new List<TournamentClass>() { MakePoolClass(root, e) } : MakeCupClasses(root, e, links);
      if (tpClass.Count != 1) {
        throw new Exception(string.Format("Got too many/too few classes from root class; should be 1 but found {0}.", tpClass.Count));
      }
      // Create child classes
      e.Draws.FindAll(d => d.DrawType == Draw.DrawTypes.QualifierCups)
        .ForEach(d => tpClass[0].ChildClasses.AddRange(MakeCupClasses(d, e, links)));
      e.Draws.FindAll(d => d.DrawType == Draw.DrawTypes.RoundRobin)
       .ForEach(d => tpClass[0].ChildClasses.Add(MakePoolClass(d, e)));

      // Sort out links
      links.ForEach(intermediateLink => {
        TournamentClass sourceClass = tpClass[0].ChildClasses.Find(sc => sc.SourceDraw == intermediateLink.SourceDraw);
        if (sourceClass == null) {
          throw new Exception("Link source draw could not connect to a class.");
        }
        tpClass[0].Links.Add(new Link() {
          SourcePosition = intermediateLink.SourcePlace,
          SourceDrawID = intermediateLink.SourceDraw.ID,
          SourceClass = sourceClass,
          TargetMatch = intermediateLink.TargetMatch,
          TeamIdentifier = intermediateLink.TeamIdentifier
        });
      });

      return tpClass[0];
    }

    public static Task<List<TournamentInformation>> ExtractTournamentInformation(OdbcConnection connection) {
      return Task.Run(() => {
        var errors = new List<Exception>();
        var tournaments = new List<TournamentInformation>();
        tournaments.AddRange(Loader.LoadTournamentInformation(connection.CreateCommand()));
        return tournaments;
      });
    }

    private static List<TournamentClass> MakeCupClasses(Draw draw, Event e, List<IntermediateLink> links) {
      // Return object
      var newClasses = new List<TournamentClass>();
      // Find the root matches in the database
      var roots = draw.Matches.FindAll(match => match.WN == 0);
      // Create a new class for each root
      roots.ForEach(root => {
        var matches = new List<MatchExtended>();
        AddMatch(draw, e, root, matches, links);
        var tournamentClass = draw.MakeScoreboardClass();
        tournamentClass.Category = e.CreateMatchCategoryString();
        matches.ForEach(m => {
          m.Category = tournamentClass.Category;
          m.Tag = HashMatch(e.TournamentInformation?.TournamentName, m.TournamentMatchNumber);
        });
        newClasses.Add(new TournamentClass(draw, tournamentClass, matches));
      });
      // Return
      return newClasses;
    }

    private static TournamentClass MakePoolClass(Draw draw, Event e) {
      // Return object
      ScoreboardLiveApi.TournamentClass tc = draw.MakeScoreboardClass();
      tc.Category = e.CreateMatchCategoryString();
      // Player entries
      var poolPlayers = draw.Matches.FindAll(m => m.EntryID > 0);
      // Match entries
      var matches = draw.Matches.FindAll(m => (m.Van1 > 0) && (m.Van2 > 0) && (m.Van1 < m.Van2))
        .Select(m => m.MakeScoreboardMatch(poolPlayers.Find(pp => pp.Planning == m.Van1)?.Entry, poolPlayers.Find(pp => pp.Planning == m.Van2)?.Entry)).ToList();
      matches.ForEach(m => {
        m.Category = tc.Category;
        m.Tag = HashMatch(e.TournamentInformation?.TournamentName, m.MatchID);
      });
      // Return
      return new TournamentClass(draw, tc, matches);
    }

    private static PlayerMatch FindPlayerMatch(Draw draw, int planning) {
      var match = draw.Matches.Find(m => m.Planning == planning);
      if (match == null) {
        throw new Exception(string.Format("Match with planning = {0} not found.", planning));
      }
      return match;
    }

    private static void AddMatch(Draw draw, Event e, PlayerMatch match, List<MatchExtended> matches, List<IntermediateLink> links, int column = 1, int startRow = 1) {
      // If this match does not have any 'van1' or 'van2', it's a base item and should be ignored, search can stop here
      if ((match.Van1 == 0) || (match.Van2 == 0)) {
        return;
      }
      // Get the van1 and van2 matches
      var van1 = FindPlayerMatch(draw, match.Van1);
      var van2 = FindPlayerMatch(draw, match.Van2);
      // Create the match
      var scoreboardMatch = match.MakeScoreboardMatch(van1.Entry, van2.Entry);
      // Set the place item
      scoreboardMatch.Place = Places.GetPlace(column * 1000 + startRow);
      matches.Add(scoreboardMatch);
      // Create any links
      if (van1.Link != null) {
        links.Add(CreateLink(van1, draw, e, "team1", scoreboardMatch));
      }
      if (van2.Link != null) {
        links.Add(CreateLink(van2, draw, e, "team2", scoreboardMatch));
      }
      // Create matches one level below
      AddMatch(draw, e, van1, matches, links, column + 1, startRow * 2 - 1);
      AddMatch(draw, e, van2, matches, links, column + 1, startRow * 2);
    }

    private static IntermediateLink CreateLink(PlayerMatch van, Draw targetDraw, Event e, string teamIdentifier, MatchExtended sbMatch) {
      if (van.Link == null) {
        throw new Exception("PlayerMatch link reference cannot be null when extracting link.");
      }
      Draw sourceDraw = e.Draws.Find(d => d.ID == van.Link.SourceDrawID);
      if (sourceDraw == null) {
        throw new Exception("Trying to find link to draw not in same event.");
      }
      return new IntermediateLink() { 
        SourceDraw = sourceDraw, 
        SourcePlace = van.Link.SourcePosition, 
        TeamIdentifier = teamIdentifier, 
        TargetMatch = sbMatch, 
        TargetDraw = targetDraw 
      };
    }

    public static string HashMatch(string tournamentName, int matchID) {
      // Create the string to hash
      string source = string.Format("{0} ?? {1}", tournamentName, matchID);
      // Hash the match string and return
      string hash;
      using (SHA256 sha = SHA256.Create()) {
        hash = ApiHelper.ByteArrayToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(source)));
      }
      return hash;
    }
  }
}
