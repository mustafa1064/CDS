using System;
using System.Collections.Generic;

namespace CDS.Core.Diagnostics
{
    public class DebugEvent
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public DebugLevel Level { get; set; }

        // 🔥 CHANGE: ENUM → STRING
        public string Category { get; set; } = "General";

        public string Message { get; set; } = "";

        public string? Source { get; set; }

        public string SessionId { get; set; } = DebugSession.Id;

        public int ThreadId { get; set; } = Environment.CurrentManagedThreadId;

        // 🔥 NEW POWER FEATURE
        public string? SubCategory { get; set; }

        public Dictionary<string, object> Data { get; set; } = new();
    }
}