using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OE2EmpireTracker.Data
{
    public class PlayerSkill
    {
        public int Level { get; set; } = 0;
        public bool TrainingStarted { get; set; } = false;
        public CountDownTime CompletionTime { get; set; } = new CountDownTime();
    }
}
