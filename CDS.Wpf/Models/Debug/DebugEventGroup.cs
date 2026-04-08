using CDS.Core.Diagnostics;
using System.Collections.Generic;

namespace CDS.Wpf.Models.Debug
{
    public class DebugEventGroup
    {
        public DebugEvent Event { get; set; } = null!;

        public int Count { get; set; }

        // 🔥 NEW: all instances of this group
        public List<DebugEvent> Items { get; set; } = new List<DebugEvent>();

        // 🔥 NEW: UI expand state
        public bool IsExpanded { get; set; }
    }
}