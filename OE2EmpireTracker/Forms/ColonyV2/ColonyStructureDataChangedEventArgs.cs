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
        public ColonyStructureDataChangedEventArgs(bool isStructural, string scrollToStructureUUID = null)
        {
            IsStructural = isStructural;
            ScrollToStructureUUID = scrollToStructureUUID;
        }

        public bool IsStructural { get; }

        /// <summary>
        /// When set, the parent form should scroll to the structure with this UUID
        /// after rebuilding the layout. Used by MoveUp/MoveDown to keep the moved
        /// structure visible after the structural rebuild.
        /// </summary>
        public string ScrollToStructureUUID { get; }
    }
}
