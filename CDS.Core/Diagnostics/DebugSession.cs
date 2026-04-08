using System;

namespace CDS.Core.Diagnostics
{
    public static class DebugSession
    {
        public static string Id { get; } =
            DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
    }
}