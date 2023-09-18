namespace BBTD.Mvc.NLogExtensions
{
    public static class LogLevel
    {
        public static readonly int Trace = NLog.LogLevel.Trace.Ordinal;
        public static readonly int Debug = NLog.LogLevel.Debug.Ordinal;
        public static readonly int Info = NLog.LogLevel.Info.Ordinal;
        public static readonly int Warn = NLog.LogLevel.Warn.Ordinal;
        public static readonly int Error = NLog.LogLevel.Error.Ordinal;
        public static readonly int Fatal = NLog.LogLevel.Fatal.Ordinal;
        public static readonly int Off = NLog.LogLevel.Off.Ordinal;
    }
}
