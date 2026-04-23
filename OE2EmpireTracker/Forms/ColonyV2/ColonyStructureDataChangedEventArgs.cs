using System;

namespace OE2EmpireTracker.Forms.ColonyV2
{
    /// <summary>
    /// Event args for ColonyStructureV2.ColonyStructureDataChanged.
    /// IsStructural=true means add/reorder/delete (requires full layout rebuild).
    /// IsStructural=false means worker toggle, state change (lightweight update only).
    /// </summary>
    public class ColonyStructureDataChangedEventArgs : EventArgs
    {
        public ColonyStructureDataChangedEventArgs(bool isStructural)
        {
            IsStructural = isStructural;
        }

        public bool IsStructural { get; }
    }
}
