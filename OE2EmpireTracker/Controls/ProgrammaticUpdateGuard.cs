namespace OE2EmpireTracker.Controls
{
    /// <summary>
    /// Interface for controls that use a programmatic update counter
    /// to suppress event handlers during code-driven updates.
    /// </summary>
    public interface IProgrammaticUpdateSource
    {
        /// <summary>
        /// Increments the programmatic update counter.
        /// While the counter is greater than zero, event handlers should be suppressed.
        /// </summary>
        void BeginProgrammaticUpdate();

        /// <summary>
        /// Decrements the programmatic update counter.
        /// </summary>
        void EndProgrammaticUpdate();
    }

    /// <summary>
    /// RAII-style guard that increments a programmatic update counter on construction
    /// and decrements it when released or disposed. Prevents event handlers from
    /// firing during code-driven UI updates.
    /// </summary>
    public class ProgrammaticUpdateGuard : System.IDisposable
    {
        private readonly IProgrammaticUpdateSource _source;
        private bool _hasLocked;

        public ProgrammaticUpdateGuard(IProgrammaticUpdateSource source)
        {
            _source = source;
            _source.BeginProgrammaticUpdate();
            _hasLocked = true;
        }

        public void release()
        {
            if (_hasLocked)
            {
                _source.EndProgrammaticUpdate();
                _hasLocked = false;
            }
        }

        public void Dispose()
        {
            release();
        }

        ~ProgrammaticUpdateGuard()
        {
            release();
        }
    }
}
