using System;
using System.Collections.Generic;
using System.Linq;

namespace TP
{
    public class Link : LinkData
    {
        public Data.DrawData Source { get; set; }
        protected Link(Data.LinkData raw) : base(raw) { }
        public int GetSourcePlacement()
        {
            if (Source.DrawType == Data.DrawData.DrawTypes.RoundRobin)
            {
                return SourcePosition;
            }
            return SourcePosition < 0 ? 2 : 1;
        }

        public int GetSourceClassIndex()
        {
            if (Source.DrawType == Data.DrawData.DrawTypes.RoundRobin)
            {
                return 0;
            }
            return Math.Max(Math.Abs(SourcePosition) - 1, 0);
        }

        public static Link Parse(Data.LinkData raw, IEnumerable<Data.DrawData> draws)
        {
            return new Link(raw)
            {
                Source = draws.First(draw => draw.ID == raw.SourceDrawID)
            };
        }
    }
}
