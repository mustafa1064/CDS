using System;
using System.Collections.Generic;

namespace CDS.Core.Diagnostics
{
    public static class DebugBus
    {
        private static readonly List<IDebugSink> _sinks = new();
        private static readonly object _lock = new object();
        public static event Action<DebugEvent>? OnDebugEvent;
        public static void RegisterSink(IDebugSink sink)
        {
            lock (_lock)
            {
                _sinks.Add(sink);
            }
        }
        public static void Emit(
        string category,
        DebugLevel level,
        string message,
        string? source = null)
        {
            if (!DebugConfig.Enabled)
                return;

            if (level < DebugConfig.MinimumLevel)
                return;

            var evt = new DebugEvent
            {
                Category = category,
                Level = level,
                Message = message,
                Source = source
            };

            OnDebugEvent?.Invoke(evt);

            List<IDebugSink> snapshot;

            lock (_lock)
            {
                snapshot = new List<IDebugSink>(_sinks);
            }

            foreach (var sink in snapshot)
            {
                sink.OnEvent(evt);
            }

            // Keep VS debug output as secondary
            System.Diagnostics.Debug.WriteLine(
                $"[{evt.Timestamp:HH:mm:ss}] [{category}] {message}"
            );
        }
        public static void Emit(DebugEvent evt)
        {
            if (!DebugConfig.Enabled)
                return;

            if (evt.Level < DebugConfig.MinimumLevel)
                return;

            OnDebugEvent?.Invoke(evt);

            List<IDebugSink> snapshot;

            lock (_lock)
            {
                snapshot = new List<IDebugSink>(_sinks);
            }

            foreach (var sink in snapshot)
            {
                sink.OnEvent(evt);
            }

            System.Diagnostics.Debug.WriteLine(
                $"[{evt.Timestamp:HH:mm:ss}] [{evt.Category}] {evt.Message}"
            );
        }
    }
}