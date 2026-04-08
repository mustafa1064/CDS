using CDS.Core.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CDS.Wpf.Sinks
{
    public class FileDebugSink : IDebugSink
    {
        private readonly string _filePath;

        private readonly ConcurrentQueue<string> _buffer = new ConcurrentQueue<string>();
        private bool _running;

        private readonly object _fileLock = new object();

        // ✅ Constructor (replaces static constructor)
        public FileDebugSink()
        {
            var folder = Path.Combine(
                System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.LocalApplicationData),
                "CDS");

            Directory.CreateDirectory(folder);

            _filePath = Path.Combine(
                folder,
                $"CDS_DebugLog_{DebugSession.Id}.txt");

            _running = true;
            Task.Run(FlushLoop);
        }

        // ✅ Sink entry point (replaces Write)
        public void OnEvent(DebugEvent e)
        {
            if (!DebugConfig.Enabled)
                return;

            var line =
                $"[{e.Timestamp:HH:mm:ss.fff}] [{e.Category}] [{e.Level}] {e.Source} :: {e.Message}";

            _buffer.Enqueue(line);
        }

        private async Task FlushLoop()
        {
            while (_running)
            {
                if (_buffer.IsEmpty)
                {
                    await Task.Delay(200);
                    continue;
                }

                var sb = new StringBuilder();

                while (_buffer.TryDequeue(out var line))
                    sb.AppendLine(line);

                try
                {
                    lock (_fileLock)
                    {
                        File.AppendAllText(_filePath, sb.ToString());
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CDS FileSink Error] {ex}");
                }

                await Task.Delay(200);
            }
        }

        // ✅ Instance Stop (optional lifecycle control)
        public void Stop()
        {
            _running = false;

            var sb = new StringBuilder();

            while (_buffer.TryDequeue(out var line))
                sb.AppendLine(line);

            try
            {
                lock (_fileLock)
                {
                    File.AppendAllText(_filePath, sb.ToString());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CDS FileSink Error] {ex}");
            }
        }
    }
}