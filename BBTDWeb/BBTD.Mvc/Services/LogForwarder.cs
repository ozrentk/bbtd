using BBTD.Mvc.Models;
using NLog;
using NLog.Fluent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BBTD.Mvc.Services
{
    public interface ILogForwarder
    {
        void LogForWebServer(string message, int levelNumber, int? reference = null, string? operation = null);
        void LogForWebServer(LogRecord logRecord);
        void LogForWebServer(LogRecord[] logRecords);
        void LogForWebUI(string message, int levelNumber);
        void LogForWebUI(LogRecord logRecord);
        void LogForWebUI(LogRecord[] logRecords);
        void LogForMobile(string message, int levelNumber);
        void LogForMobile(LogRecord logRecord);
        void LogForMobile(LogRecord[] logRecords);
        void LogRaw(LogRecord[] logRecords, string? application);
    }

    public class LogForwarder : ILogForwarder
    {
        private readonly ISetupRepo _setupRepo;
        private object _lockObj = new object();
        private Logger _logger = LogManager.GetCurrentClassLogger();
        
        private readonly string _webUiLogName = "GUI";
        private readonly string _serverLogName = "SRV";
        private readonly string _mobAppLogName = "APP";

        public LogForwarder(ISetupRepo setupRepo)
        {
            _setupRepo = setupRepo;
        }

        public void LogForWebUI(string message, int levelNumber)
        {
            LogRaw(new[] { new LogRecord(message, levelNumber) }, _webUiLogName);
        }

        public void LogForWebUI(LogRecord logRecord)
        {
            LogRaw(new[] { logRecord }, _webUiLogName);
        }

        public void LogForWebUI(LogRecord[] logRecords)
        {
            LogRaw(logRecords, _webUiLogName);
        }

        public void LogForWebServer(string message, int levelNumber, int? reference = null, string? operation = null)
        {
            LogRaw(new[] { new LogRecord(message, levelNumber, reference, operation) }, _serverLogName);
        }

        public void LogForWebServer(LogRecord logRecord)
        {
            LogRaw(new[] { logRecord }, _serverLogName);
        }

        public void LogForWebServer(LogRecord[] logRecords)
        {
            LogRaw(logRecords, _serverLogName);
        }

        public void LogForMobile(string message, int levelNumber)
        {
            LogRaw(new[] { new LogRecord(message, levelNumber) }, _mobAppLogName);
        }

        public void LogForMobile(LogRecord logRecord)
        {
            LogRaw(new[] { logRecord }, _mobAppLogName);
        }

        public void LogForMobile(LogRecord[] logRecords)
        {
            LogRaw(logRecords, _mobAppLogName);
        }

        public void LogRaw(LogRecord[] logRecords, string? application)
        {
            lock (_lockObj)
            {
                var setupData = _setupRepo.GetData();
                //var ipEndPoint = IPEndPoint.Parse(setupData.GraylogUrl);

                foreach (var logRecord in logRecords)
                {
                    var info = new LogEventInfo
                    {
                        LoggerName = application,
                        Message = logRecord.Message,
                        Level = logRecord.Level,
                        Parameters = new object[] { logRecord.ExactTimestamp, logRecord.Reference, logRecord.Operation }
                    };
                    _logger.Log(info);
                }
            }
        }

        //private static void FillData(string? application, LogRecord logRecord)
        //{
        //    //// Id
        //    //if (logRecord.Id == default)
        //    //    logRecord.Id = Guid.NewGuid();

        //    //// Version
        //    //if (logRecord.Version == default)
        //    //    logRecord.Version = "1.1";

        //    //// Host
        //    //if (logRecord.Host == default)
        //    //    logRecord.Host = "127.0.0.1";

        //    // Application
        //    if (application != null)
        //        logRecord.Application = application;

        //    // Message
        //    if (logRecord.Message == null)
        //        logRecord.Message = "(empty message)";

        //    // Level (number)
        //    if (logRecord.LevelNumber == default)
        //        logRecord.LevelNumber = 1;

        //    // Timestamp
        //    if (logRecord.ExactTimestamp == default)
        //        //logRecord.ExactTimestamp = DateTime.UtcNow;
        //        throw new InvalidOperationException("You must set exact timestamp!");

        //    if (logRecord.ExactTimestamp.Microsecond > 0)
        //        logRecord.ExactTimestamp = logRecord.ExactTimestamp.AddMicroseconds(-logRecord.ExactTimestamp.Microsecond);
        //}
    }
}
