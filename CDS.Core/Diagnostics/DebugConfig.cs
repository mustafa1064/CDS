using System;

namespace CDS.Core.Diagnostics
{
    public static class DebugConfig
    {
        static DebugConfig()
        {
#if DEBUG
        Enabled = true;
#else
            Enabled = Environment.GetEnvironmentVariable("APP_DEBUG") == "1";
#endif
        }

        public static bool Enabled { get; set; }

        public static DebugLevel MinimumLevel { get; set; } = DebugLevel.Info;
    }
}