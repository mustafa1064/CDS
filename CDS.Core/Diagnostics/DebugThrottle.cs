using System;
using System.Collections.Concurrent;

namespace CDS.Core.Diagnostics
{
    public static class DebugThrottle
    {
        private static readonly ConcurrentDictionary<string, DateTime> _lastLogTime = new ConcurrentDictionary<string, DateTime>();

        public static bool ShouldLog(string key, int milliseconds = 500)
        {
            var now = DateTime.UtcNow;

            if (_lastLogTime.TryGetValue(key, out var last))
            {
                if ((now - last).TotalMilliseconds < milliseconds)
                    return false;
            }

            _lastLogTime[key] = now;
            return true;
        }
    }
}