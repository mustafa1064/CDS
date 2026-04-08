using CDS.Core.Diagnostics;
using CDS.Wpf.Models.Debug;
using CDS.Wpf.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace CDS.Wpf.ViewModels
{
    public class DebugPanelViewModel : INotifyPropertyChanged
    {
        public DebugCollectorService Collector { get; }

        public bool ShowTime { get; set; } = true;
        public bool ShowCategory { get; set; } = true;
        public bool ShowLevel { get; set; } = true;
        public bool ShowSource { get; set; } = true;
        public bool ShowMessage { get; set; } = true;
        public bool ShowThread { get; set; } = true;
        public bool ShowSession { get; set; } = true;

        private DateTime _lastRebuild = DateTime.MinValue;
        
        // 🔥 PAUSE BUFFER (Backpressure-safe)
        private readonly List<DebugEvent> _pausedBuffer = new();
        private readonly object _bufferLock = new();

        private DebugLevel? _selectedLevel;
        public DebugLevel? SelectedLevel
        {
            get => _selectedLevel;
            set
            {
                _selectedLevel = value;
                OnPropertyChanged();
                RebuildFilter();
            }
        }
        private string _selectedCategory;
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                RebuildFilter();
            }
        }
        private string _filterText = "";
        public string FilterText
        {
            get => _filterText;
            set
            {
                _filterText = value;
                OnPropertyChanged();
                RebuildFilter();
            }
        }

        private bool _isPaused;
        public bool IsPaused
        {
            get => _isPaused;
            set
            {
                if (_isPaused == value) return;

                _isPaused = value;
                OnPropertyChanged();

                if (!_isPaused)
                {
                    // 🔥 RESUME → flush buffered events
                    FlushPausedBuffer();
                }
            }
        }

        private bool _autoScroll = true;
        public bool AutoScroll
        {
            get => _autoScroll;
            set
            {
                _autoScroll = value;
                OnPropertyChanged();
            }
        }

        public int MaxEvents
        {
            get => Collector.MaxEvents;
            set
            {
                Collector.MaxEvents = value;
                OnPropertyChanged();
            }
        }

        private bool _removeDuplicates;
        public bool RemoveDuplicates
        {
            get => _removeDuplicates;
            set
            {
                _removeDuplicates = value;
                OnPropertyChanged();
                RebuildFilter();
            }
        }

        private int _uiRefreshIntervalMs = 200;

        public int UiRefreshIntervalMs
        {
            get => _uiRefreshIntervalMs;
            set
            {
                _uiRefreshIntervalMs = value;
                OnPropertyChanged();
            }
        }

        private bool _enableBufferLimit = true;
        public bool EnableBufferLimit
        {
            get => _enableBufferLimit;
            set
            {
                _enableBufferLimit = value;
                OnPropertyChanged();
            }
        }

        private int _maxBufferSize = 10000;
        public int MaxBufferSize
        {
            get => _maxBufferSize;
            set
            {
                _maxBufferSize = value;
                OnPropertyChanged();
            }
        }

        public Array Levels => Enum.GetValues(typeof(DebugLevel));
        public IEnumerable<string> Categories =>
            Collector.Events
                .Select(e => e.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c);

        private List<DebugEventGroup> _filtered = new();

        // 🔥 Persist expand state across rebuilds
        private readonly Dictionary<string, bool> _expandState = new();

        public IEnumerable<DebugEventGroup> FilteredEvents => _filtered;

        public DebugPanelViewModel(DebugCollectorService collector)
        {
            Collector = collector;
            Collector.Events.CollectionChanged += (_, e) =>
            {
                // 🔥 PAUSE MODE → buffer instead of UI update
                if (IsPaused)
                {
                    if (e.NewItems != null)
                    {
                        lock (_bufferLock)
                        {
                            foreach (DebugEvent item in e.NewItems)
                                _pausedBuffer.Add(item);

                            // 🔥 RAM SAFETY LIMIT (PLACE HERE)
                            if (EnableBufferLimit && _pausedBuffer.Count > MaxBufferSize)
                            {
                                _pausedBuffer.RemoveRange(0, _pausedBuffer.Count - MaxBufferSize);
                            }
                        }
                    }
                    return;
                }

                var now = DateTime.UtcNow;

                // 🔥 Throttle UI updates (user controlled)
                if (UiRefreshIntervalMs > 0 &&
                    (now - _lastRebuild).TotalMilliseconds < UiRefreshIntervalMs)
                    return;

                _lastRebuild = now;

                RebuildFilter();
                OnPropertyChanged(nameof(Categories));
            };
        }

        private void FlushPausedBuffer()
        {
            List<DebugEvent> snapshot;

            lock (_bufferLock)
            {
                if (_pausedBuffer.Count == 0)
                    return;

                snapshot = _pausedBuffer.ToList();
                _pausedBuffer.Clear();
            }

            // 🔥 Merge buffered events into collector safely
            Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
            {
                const int batchSize = 200;

                for (int i = 0; i < snapshot.Count; i += batchSize)
                {
                    var chunk = snapshot.Skip(i).Take(batchSize);

                    foreach (var e in chunk)
                        Collector.Events.Add(e);

                    await Task.Delay(1);
                }

                // 🔥 SAFE: update AFTER replay completes
                RebuildFilter();
                OnPropertyChanged(nameof(Categories));

            }));
        }

        private void RebuildFilter()
        {
            List<DebugEvent> snapshot;

            if (Application.Current.Dispatcher.CheckAccess())
            {
                snapshot = Collector.Events.ToList();
            }
            else
            {
                snapshot = Application.Current.Dispatcher.Invoke(() =>
                    Collector.Events.ToList());
            }

            IEnumerable<DebugEvent> query = snapshot;

            // 🔹 Level filter
            if (SelectedLevel.HasValue)
                query = query.Where(e => e.Level == SelectedLevel);

            // 🔹 Category filter
            if (!string.IsNullOrEmpty(SelectedCategory))
                query = query.Where(e => e.Category == SelectedCategory);

            // 🔹 FULL TEXT FILTER
            if (!string.IsNullOrWhiteSpace(FilterText))
            {
                query = query.Where(e =>
                {
                    var full =
                        $"{e.Timestamp:HH:mm:ss.fff} {e.Category} {e.Level} {e.Source} {e.Message} {e.ThreadId} {e.SessionId}";

                    return full.IndexOf(FilterText, StringComparison.OrdinalIgnoreCase) >= 0;
                });
            }

            // 🔥 🔥 GROUPING LOGIC (REPLACES DUPLICATE FILTER)
            List<DebugEventGroup> result;

            if (RemoveDuplicates)
            {
                result = query
                        .GroupBy(e => BuildDuplicateKey(e))
                        .Select(g =>
                        {
                            var key = g.Key;

                            return new DebugEventGroup
                            {
                                Event = g.First(),
                                Count = g.Count(),
                                Items = g.ToList(),

                                // 🔥 RESTORE previous expand state if exists
                                IsExpanded = _expandState.TryGetValue(key, out var expanded) && expanded
                            };
                        })
                        .OrderBy(g => g.Event.Timestamp)
                        .ToList();
            }
            else
            {
                result = query
                    .Select(e => new DebugEventGroup
                    {
                        Event = e,
                        Count = 1,
                        Items = new List<DebugEvent> { e },
                        IsExpanded = false
                    })
                    .ToList();
            }

            ApplyFlattening(result);
            OnPropertyChanged(nameof(FilteredEvents));
        }

        private void ApplyFlattening(List<DebugEventGroup> groups)
        {
            var flat = new List<DebugEventGroup>();

            foreach (var g in groups)
            {
                flat.Add(g);

                if (g.IsExpanded && g.Items.Count > 1)
                {
                    // If you want to skip first item (as it's the group header), use: g.Items.Skip(1) // foreach (var item in g.Items.Skip(1)) // now, foreach (var item in g.Items)
                    foreach (var item in g.Items.Skip(1))
                    {
                        flat.Add(new DebugEventGroup
                        {
                            Event = item,
                            Count = 0,
                            Items = new List<DebugEvent> { item }
                        });
                    }
                }
            }

            _filtered = flat;
        }
        public void ReapplyExpansion()
        {
            // 🔥 Rebuild ONLY flattening layer (FAST, no filtering)
            var groups = _filtered
                .Where(g => g.Count > 0) // only parent groups
                .Select(g =>
                {
                    var key = BuildDuplicateKey(g.Event);

                    return new DebugEventGroup
                    {
                        Event = g.Event,
                        Count = g.Count,
                        Items = g.Items,

                        // 🔥 restore from state
                        IsExpanded = _expandState.TryGetValue(key, out var ex) && ex
                    };
                })
                .ToList();

            ApplyFlattening(groups);

            OnPropertyChanged(nameof(FilteredEvents));
        }
        public void Refresh()
        {
            OnPropertyChanged(nameof(FilteredEvents));
        }

        private string BuildDuplicateKey(DebugEvent e)
        {
            // 🔥 Ignore timestamp, thread, session → focus on logical duplication
            return $"{e.Category}|{e.Level}|{e.Source}|{e.Message}";
        }
        public void SetExpandState(string key, bool expanded)
        {
            _expandState[key] = expanded;
        }

        public bool IsEnabled
        {
            get => DebugConfig.Enabled;
            set
            {
                DebugConfig.Enabled = value;

                if (value)
                {
                    Collector.Clear(); // 🔥 fresh session feel
                }

                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}