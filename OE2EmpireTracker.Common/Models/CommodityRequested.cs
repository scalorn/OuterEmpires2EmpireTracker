using System;

namespace OE2EmpireTracker.Models
{
    public class CommodityRequested
    {
        public string Name { get; internal set; }
        public int Requested { get; internal set; }
        public int Delivered { get; internal set; }
        public DateTime NeedBy { get; internal set; }
        public bool Fulfilled { get; internal set; }
    }
}
