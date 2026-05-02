namespace OE2EmpireTracker.ViewModels
{
    /// <summary>
    /// Local copy of a single rank track's state in the edit buffer.
    /// Disconnected from the PlayerRank entity.
    /// </summary>
    public class LocalRankData
    {
        public int Rank { get; set; }

        public long CurrentXP { get; set; }

        public long NextXP { get; set; }

        public string Title { get; set; } = string.Empty;
    }
}