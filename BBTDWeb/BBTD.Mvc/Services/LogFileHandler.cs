using BBTD.Mvc.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Threading.Tasks;

namespace BBTD.Mvc.Services
{
    public interface ILogFileHandler
    {
        Task<IEnumerable<NLogRecord>> ReadLogAsync(string path);
        Task<IEnumerable<NLogRecord>> GetLastLogsAsync();
        Task SaveLastLogsAsync(string datasetName);
        IEnumerable<OutputDataFile> GetOutputDataFiles();
        OutputDataFile GetOutputDataFile(string name);
        void DeleteOutputDataFile(string name);
    }

    public class LogFileHandler : ILogFileHandler
    {
        private readonly IWebHostEnvironment _env;

        public LogFileHandler(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<IEnumerable<NLogRecord>> ReadLogAsync(string path)
        {
            using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader streamReader = new StreamReader(fileStream);

            var logs = new List<NLogRecord>();
            while (true)
            {
                var json = await streamReader.ReadLineAsync();
                if (json == null)
                    break;

                var log = JsonSerializer.Deserialize<NLogRecord>(json);
                if (log != null)
                    logs.Add(log);
            }
            return logs;
        }

        public async Task<IEnumerable<NLogRecord>> GetLastLogsAsync()
        {
            var logpath = Path.Combine(_env.ContentRootPath, "Logs/events.jsonlog");
            var logs = (await ReadLogAsync(logpath)).ToList();
            var offsetRecord = logs.LastOrDefault(l => l.Operation?.Equals("APP_OFFSET") ?? false);
            var offset = offsetRecord?.Message;

            var startLog = logs.Last(l => l.Message.Equals("Starting BarcodeSlideshow"));
            var lastLogs = logs.Skip(logs.IndexOf(startLog)).Where(x => x.Reference.HasValue).ToList();

            return lastLogs;
        }

        public async Task SaveLastLogsAsync(string datasetName)
        {
            var logfolder = Path.Combine(_env.ContentRootPath, $"OutputData");
            var logfile = Path.Combine(_env.ContentRootPath, $"OutputData/{datasetName}.json");

            if (!Directory.Exists(logfolder))
                Directory.CreateDirectory(logfolder);

            var logs = await GetLastLogsAsync();

            var logContent = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(logfile, logContent);
        }

        public IEnumerable<OutputDataFile> GetOutputDataFiles()
        {
            var logfolder = Path.Combine(_env.ContentRootPath, $"OutputData");
            var logfiles = Path.Combine(_env.ContentRootPath, $"OutputData/*.json");

            if (!Directory.Exists(logfolder))
                return new List<OutputDataFile>();

            var files = Directory.EnumerateFiles(logfolder);
            return files.Select(x => 
                new OutputDataFile { 
                    Filename = x, 
                    Name = Path.GetFileNameWithoutExtension(x) }).ToList();
        }

        public OutputDataFile GetOutputDataFile(string name)
        {
            var logfolder = Path.Combine(_env.ContentRootPath, $"OutputData");
            var logfile = Path.Combine(_env.ContentRootPath, $"OutputData/{name}.json");

            var outFile = new OutputDataFile
            {
                Filename = name,
                Name = Path.GetFileNameWithoutExtension(name),
                LogRecords = new List<NLogRecord>(),
                AnalysisRecords = new List<AnalysisRecord>()
            };

            if (!Directory.Exists(logfolder))
                return outFile;

            var content = File.ReadAllText(logfile);

            outFile.LogRecords = JsonSerializer.Deserialize<List<NLogRecord>>(content);
            outFile.AnalysisRecords = Analyze(outFile.LogRecords);

            return outFile;
        }

        private List<AnalysisRecord> Analyze(List<NLogRecord> logs)
        {
            var offsetLog = logs.FirstOrDefault(x => x.Operation == LogOperation.APP_OFFSET);
            int.TryParse(offsetLog?.Message.Split(":").Last().Trim(), out int offset);

            var logsByRef =
                from l in logs.ToList()
                group l by l.Reference into g_ref
                select new { Reference = g_ref.Key, Logs = g_ref.ToList() };

            var recordCount = 1;
            var analysisRecords = new List<AnalysisRecord>();
            foreach (var refGroup in logsByRef)
            {
                refGroup.Logs.ForEach(x =>
                {
                    x.ExactTimestamp = x.Application.Equals("APP") ? x.ExactTimestamp.AddMilliseconds(offset) : x.ExactTimestamp;
                });

                var imgReqLog = refGroup.Logs.FirstOrDefault(x => x.Operation == LogOperation.IMG_REQ);
                var imgGenLog = refGroup.Logs.FirstOrDefault(x => x.Operation == LogOperation.IMG_GEN);
                var imgRecvLog = refGroup.Logs.FirstOrDefault(x => x.Operation == LogOperation.IMG_RECV);
                var bcDataRecvLog = refGroup.Logs.FirstOrDefault(x => x.Operation == LogOperation.BC_DATA_RECV);
                var analysisRecord = new AnalysisRecord
                {
                    RecordNumber = recordCount++,
                    StartedAt = imgReqLog?.ExactTimestamp,
                };

                if (imgGenLog != null && imgReqLog != null)
                    analysisRecord.ImageGenerationTime = (imgGenLog.ExactTimestamp - imgReqLog.ExactTimestamp).Milliseconds;

                if (imgRecvLog != null && imgGenLog != null)
                    analysisRecord.ImageLoadingTime = (imgRecvLog.ExactTimestamp - imgGenLog.ExactTimestamp).Milliseconds;

                if (bcDataRecvLog != null && imgRecvLog != null)
                    analysisRecord.BarcodeReadingTime = (bcDataRecvLog.ExactTimestamp - imgRecvLog.ExactTimestamp).Milliseconds;

                if (bcDataRecvLog != null && imgReqLog != null)
                    analysisRecord.TotalTime = (bcDataRecvLog.ExactTimestamp - imgReqLog.ExactTimestamp).Milliseconds;

                analysisRecords.Add(analysisRecord);
            }

            return analysisRecords;
        }

        public void DeleteOutputDataFile(string name)
        {
            var logfolder = Path.Combine(_env.ContentRootPath, $"OutputData");
            var logfile = Path.Combine(_env.ContentRootPath, $"OutputData/{name}.json");

            if (!Directory.Exists(logfolder))
                return;

            File.Delete(logfile);
        }

    }
}
