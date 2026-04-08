using CDS.Core.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CDS.Wpf.Services
{
    public class DebugCollectorService : IDisposable
    {
        public ObservableCollection<DebugEvent> Events { get; } = new ObservableCollection<DebugEvent>();

        private readonly ConcurrentQueue<DebugEvent> _queue = new ConcurrentQueue<DebugEvent>();
        private bool _isRunning;
        private Action<DebugEvent> _handler = null;

        public bool IsPaused { get; set; }
        public int MaxEvents { get; set; } = 2000;

        public void Start()
        {
            if (_isRunning)
                return;

            _isRunning = true;

            _handler = e => _queue.Enqueue(e);
            DebugBus.OnDebugEvent += _handler;

            Task.Run(ProcessQueue);
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            _isRunning = false;

            if (_handler != null)
            {
                DebugBus.OnDebugEvent -= _handler;
                _handler = null;
            }
        }

        private async Task ProcessQueue()
        {
            while (_isRunning)
            {
                if (IsPaused)
                {
                    await Task.Delay(100);
                    continue;
                }

                var batch = new List<DebugEvent>();

                while (_queue.TryDequeue(out var e))
                {
                    batch.Add(e);
                }

                if (batch.Count > 0)
                {
                    await PushBatchToUi(batch);
                }

                await Task.Delay(50);
            }
        }

        private async Task PushBatchToUi(List<DebugEvent> batch)
        {
            const int chunkSize = 100;

            for (int i = 0; i < batch.Count; i += chunkSize)
            {
                var chunk = batch.Skip(i).Take(chunkSize).ToList();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var e in chunk)
                    {
                        Events.Add(e);
                    }

                    while (Events.Count > MaxEvents)
                        Events.RemoveAt(0);

                }, System.Windows.Threading.DispatcherPriority.Background);

                // 🔥 Yield UI thread → CRITICAL FIX
                await Task.Delay(1);
            }
        }

        public void Clear()
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => Events.Clear()));

            while (_queue.TryDequeue(out var _)) { }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}