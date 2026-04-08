using CDS.Core.Diagnostics;
using CDS.Wpf.Models.Debug;
using CDS.Wpf.Services;
using CDS.Wpf.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace CDS.Wpf.Views
{
    /// <summary>
    /// Interaction logic for DebugPanelView.xaml
    /// </summary>
    public partial class DebugPanelView : UserControl
    {
        private static string Format(DebugEventGroup g, DebugPanelViewModel vm)
        {
            var e = g.Event;
            var parts = new List<string>();

            if (vm.ShowTime)
                parts.Add($"[{e.Timestamp:HH:mm:ss.fff}]");

            if (vm.ShowCategory)
                parts.Add($"[{e.Category}]");

            if (vm.ShowLevel)
                parts.Add($"[{e.Level}]");

            if (vm.ShowSource && !string.IsNullOrWhiteSpace(e.Source))
                parts.Add(e.Source);

            if (vm.ShowMessage)
                parts.Add($":: {e.Message}");

            if (vm.ShowThread)
                parts.Add($"| Thread={e.ThreadId}");

            if (vm.ShowSession)
                parts.Add($"| Session={e.SessionId}");

            // 🔥 ADD COUNT HERE
            if (g.Count > 1)
                parts.Add($"(x{g.Count})");

            return string.Join(" ", parts);
        }

        private INotifyCollectionChanged? _collection;

        private DateTime _lastScroll = DateTime.MinValue;

        private DebugCollectorService? _collector;

        public DebugPanelView()
        {
            InitializeComponent();

            if (DataContext == null)
            {
                _collector = new DebugCollectorService();
                _collector.Start();

                // ❌ DO NOT REGISTER AS SINK

                DataContext = new DebugPanelViewModel(_collector);
            }
        }


        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DebugPanelViewModel vm)
            {
                var lines = vm.FilteredEvents
                    .Select(g => Format(g, vm));

                Clipboard.SetText(string.Join("\n", lines));
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DebugPanelViewModel vm)
            {
                var path = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"CDS_Export_{DebugSession.Id}.txt");

                var lines = vm.FilteredEvents
                    .Select(g => Format(g, vm));

                File.WriteAllLines(path, lines);

                MessageBox.Show($"Exported to:\n{path}");
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DebugPanelViewModel vm)
            {
                vm.Collector.Clear();
            }
        }
        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is DataGrid grid)
            {
                _collection = grid.ItemsSource as INotifyCollectionChanged;

                if (_collection != null)
                {
                    _collection.CollectionChanged += OnCollectionChanged;
                }
            }
        }
        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // 🔥 Only react to new items (prevents scroll spam on Clear/Reset)
            if (e.Action != NotifyCollectionChangedAction.Add)
                return;

            if (DataContext is not DebugPanelViewModel vm)
                return;

            if (!vm.AutoScroll)
                return;

            var grid = DebugGrid;

            if (grid == null || grid.Items.Count == 0)
                return;

            // 🔥 Throttle scroll (VERY IMPORTANT under heavy logs)
            var now = DateTime.UtcNow;

            if ((now - _lastScroll).TotalMilliseconds < 100)
                return;

            _lastScroll = now;

            // 🔥 Ensure UI thread safety
            if (!grid.Dispatcher.CheckAccess())
            {
                grid.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (grid.Items.Count == 0)
                        return;

                    var lastItem = grid.Items[grid.Items.Count - 1];
                    grid.ScrollIntoView(lastItem);
                }));

                return;
            }

            // 🔥 Scroll to latest item
            var last = grid.Items[grid.Items.Count - 1];
            grid.ScrollIntoView(last);
        }
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_collection != null)
            {
                _collection.CollectionChanged -= OnCollectionChanged;
            }

            if (_collector != null)
            {
                _collector.Dispose();
            }
        }
        private void ToggleExpand_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DebugEventGroup group)
            {
                group.IsExpanded = !group.IsExpanded;

                if (DataContext is DebugPanelViewModel vm)
                {
                    // 🔥 Save state using same key logic
                    var key = $"{group.Event.Category}|{group.Event.Level}|{group.Event.Source}|{group.Event.Message}";
                    vm.SetExpandState(key, group.IsExpanded);

                    vm.ReapplyExpansion();
                }
            }
        }
    }
}