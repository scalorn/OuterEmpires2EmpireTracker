namespace OE2EmpireTracker.ViewModels
{
    /// <summary>
    /// Local copy of a single rank track's state in the edit buffer.
    /// Disconnected from the PlayerRank entity.
    /// </summary>
    public class LocalRankData
    {
        public int Rank { get; set; }

        public long CurrentXp { get; set; }

        public long XpToNextLevel { get; set; }

        public string RankName { get; set; } = string.Empty;
    }
}
