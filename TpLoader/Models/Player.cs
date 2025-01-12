using System;
using System.Collections.Generic;
using System.Linq;

namespace TP
{
    public class Player : PlayerData
    {
        public Data.ClubData Club { get; set; }
        public Player(string bothNames, string clubName)
        {
            int separator = bothNames.IndexOf(' ');
            if (separator >= 0)
            {
                FirstName = bothNames.Substring(0, separator);
                LastName = bothNames.Substring(separator + 1, bothNames.Length - separator - 1);
            }
            else
            {
                FirstName = "";
                LastName = bothNames;
            }
            Club = new Data.ClubData()
            {
                Name = clubName
            };
        }

        protected Player(Data.PlayerData raw, Data.ClubData club) : base(raw)
        {
            Club = club;
        }

        public static Player Parse(Data.PlayerData raw, IEnumerable<Data.ClubData> clubs)
        {
            Data.ClubData club = clubs.FirstOrDefault(club => club.ID == raw.ClubID);
            /* if (club == null) {
              throw new Exception("Player club not specified or found");
            } */
            return new Player(raw, club);
        }

        public override string ToString()
        {
            return string.Format("{0}\t{1} {2}\t{3}", ID, FirstName, LastName, Club != null ? Club.Name : "<no club>");
        }
    }
}
