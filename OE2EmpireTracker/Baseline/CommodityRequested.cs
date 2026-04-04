using System;

namespace OE2EmpireTracker.Baseline
{
    public class CommodityRequested
    {
        public string Name { get; set; }
        public int Requested { get; set; }
        public int Delivered { get; set; }
        public DateTime NeedBy { get; set; }
        public bool Fulfilled { get; set; }
    }
}
