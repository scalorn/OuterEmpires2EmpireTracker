using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OE2EmpireTracker.Models
{
    public class PlayerRank
    {
        public int Rank { get; internal set; } = 0;
        public long CurrentXP { get; internal set; } = 0;
        public long NextXP { get; internal set; } = 0;
        public string Title { get; internal set; } = string.Empty;
    }
}
