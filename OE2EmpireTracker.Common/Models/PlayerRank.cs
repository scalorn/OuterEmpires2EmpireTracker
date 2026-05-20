using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OE2EmpireTracker.Models
{
    public class PlayerRank
    {
        public int Rank { get; set; } = 0;
        public long CurrentXP { get; set; } = 0;
        public long NextXP { get; set; } = 0;
        public string Title { get; set; } = string.Empty;
    }
}
