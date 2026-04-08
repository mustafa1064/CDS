using System.Windows;
using CDS.Core.Diagnostics;
using CDS.Wpf.Sinks;

namespace CDS.TestApp
{
    public partial class App : Application
    {
        private FileDebugSink _fileSink;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ✅ Enable Debug System
            DebugConfig.Enabled = true;

            // ✅ Register File Sink
            _fileSink = new FileDebugSink();
            DebugBus.RegisterSink(_fileSink);

            // ✅ Initial Signal
            DebugBus.Emit("System", DebugLevel.Info, "CDS Initialized", "App");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // ✅ Ensure buffer flush
            _fileSink?.Stop();

            base.OnExit(e);
        }
    }
}