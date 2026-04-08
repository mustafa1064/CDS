using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CDS.Core.Diagnostics;

namespace CDS.TestApp
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource _autoCts;
        private readonly Random _rand = new Random();

        private readonly string[] _categories =
        {
            "UI", "Rendering", "Data", "Network", "Validation"
        };

        private readonly DebugLevel[] _levels =
        {
            DebugLevel.Info,
            DebugLevel.Warning,
            DebugLevel.Error,
            DebugLevel.Verbose
        };

        public MainWindow()
        {
            InitializeComponent();

            DebugBus.Emit("System", DebugLevel.Info, "Demo App Started", "MainWindow");
        }

        // =============================
        // BASIC BUTTONS
        // =============================

        private void Info_Click(object sender, RoutedEventArgs e)
        {
            DebugBus.Emit("UI", DebugLevel.Info, "User clicked Info", "MainWindow");
        }

        private void WarnError_Click(object sender, RoutedEventArgs e)
        {
            DebugBus.Emit("Validation", DebugLevel.Warning, "Invalid input detected", "Validator");
            DebugBus.Emit("System", DebugLevel.Error, "Simulated failure", "Engine");
        }

        private void Duplicate_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 10; i++)
            {
                DebugBus.Emit("Rendering", DebugLevel.Warning, "Image too large", "Renderer");
            }
        }

        private void Burst_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 100; i++)
            {
                DebugBus.Emit("Burst", DebugLevel.Info, $"Event #{i}", "BurstTest");
            }
        }

        private async void Stress_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    DebugBus.Emit("Stress", DebugLevel.Verbose, $"Load {i}", "StressTest");
                }
            });
        }

        // =============================
        // AUTO LOGGING
        // =============================

        private void StartAuto_Click(object sender, RoutedEventArgs e)
        {
            if (_autoCts != null)
                return;

            _autoCts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                while (!_autoCts.Token.IsCancellationRequested)
                {
                    EmitRandomEvent();

                    int delay = 200;

                    delay = (int)Dispatcher.Invoke(() => RateSlider.Value);

                    await Task.Delay(delay);
                }
            });
        }

        private void StopAuto_Click(object sender, RoutedEventArgs e)
        {
            _autoCts?.Cancel();
            _autoCts = null;
        }

        // =============================
        // RANDOMIZED STRUCTURED LOGS
        // =============================

        private void EmitRandomEvent()
        {
            var category = _categories[_rand.Next(_categories.Length)];
            var level = _levels[_rand.Next(_levels.Length)];

            var evt = new DebugEvent
            {
                Category = category,
                Level = level,
                Message = GenerateMessage(category),
                Source = "AutoGenerator",
                SubCategory = "Demo",
                Data = new Dictionary<string, object>
                {
                    ["Value"] = _rand.Next(0, 1000),
                    ["Flag"] = _rand.Next(0, 2) == 0,
                    ["Time"] = DateTime.UtcNow.ToString("HH:mm:ss")
                }
            };

            DebugBus.Emit(evt);
        }

        private string GenerateMessage(string category)
        {
            return category switch
            {
                "UI" => "Button interaction detected",
                "Rendering" => "Frame rendered",
                "Data" => "Dataset updated",
                "Network" => "API response received",
                "Validation" => "Rule evaluated",
                _ => "Unknown event"
            };
        }
    }
}