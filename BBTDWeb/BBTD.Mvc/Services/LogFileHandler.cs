using BBTD.Mvc.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
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
        Task<IEnumerable<NLogRecord>> ReadRawLogsAsync(string path, DateTime? from = null);
        Task<IEnumerable<NLogRecord>> GetLogStartsAsync();
        Task<IEnumerable<NLogRecord>> GetLogOffsetsAsync();
        Task<IEnumerable<NLogRecord>> GetLogsAsync(DateTime from);
        Task SaveLastLogsAsync(DateTime from, string datasetName);
        IEnumerable<OutputDataFile> GetOutputDataFiles(string? prefix = null);
        OutputDataFile GetOutputDataFile(string name);
        void DeleteOutputDataFile(string name);
        void ExportAnalyzedDataFile(AnalyzedDataFile analyzedFile);
    }

    public class LogFileHandler : ILogFileHandler
    {
        private readonly IWebHostEnvironment _env;

        public LogFileHandler(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<IEnumerable<NLogRecord>> ReadRawLogsAsync(string path, DateTime? from = null)
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
            
            return 
                from != null ? 
                    logs.Where(x => x.ExactTimestamp >= from) : 
                    logs;
        }

        public async Task<IEnumerable<NLogRecord>> GetLogStartsAsync()
        {
            var logpath = Path.Combine(_env.ContentRootPath, "Logs/events.jsonlog");
            var logs = await ReadRawLogsAsync(logpath);

            var startLogs = logs.Where(l => l.Message.Equals("Starting BarcodeSlideshow"));
            var startLogsOrdered = startLogs.OrderByDescending(x => x.ExactTimestamp).ToList();

            return startLogsOrdered;
        }

        public async Task<IEnumerable<NLogRecord>> GetLogOffsetsAsync()
        {
            var logpath = Path.Combine(_env.ContentRootPath, "Logs/events.jsonlog");
            var logs = await ReadRawLogsAsync(logpath);

            var offsetLogs = logs.Where(l => l.Operation?.Equals("APP_OFFSET") ?? false);
            var offsetLogsOrdered = offsetLogs.OrderBy(x => x.ExactTimestamp).ToList();

            return offsetLogsOrdered;
        }

        public async Task<IEnumerable<NLogRecord>> GetLogsAsync(DateTime from)
        {
            var logPath = Path.Combine(_env.ContentRootPath, "Logs/events.jsonlog");
            var logs = (await ReadRawLogsAsync(logPath, from)).ToList();

            var offsetRecords = await GetLogOffsetsAsync();
            var offsetRecord = offsetRecords.LastOrDefault(x => x.ExactTimestamp <= from);
            int.TryParse(offsetRecord?.Message.Split(":").Last().Trim(), out int offset);

            var logStarts = 
                logs.Where(l => l.Message.Equals("Starting BarcodeSlideshow") && l.ExactTimestamp >= from)
                    .OrderBy(x => x.ExactTimestamp)
                    .Take(2)
                    .ToList();

            int logStartIdx1 = 0;
            int logStartIdx2 = 0;
            if (!logStarts.Any())
                return new List<NLogRecord>();
            
            if (logStarts.Count() > 0)
                logStartIdx1 = logs.IndexOf(logStarts.First());
            
            if (logStarts.Count() > 1)
                logStartIdx2 = logs.IndexOf(logStarts.Last());

            var logSlice =
                logs.OrderBy(x => x.ExactTimestamp)
                    .Skip(logStartIdx1)
                    .ToList();

            if (logStartIdx2 > 0)
                logSlice = logSlice.Take(logStartIdx2 - logStartIdx1).ToList();

            logSlice.ForEach(x =>
            {
                x.ExactTimestamp = x.Application.Equals("APP") ? x.ExactTimestamp.AddMilliseconds(offset) : x.ExactTimestamp;
            });
            logSlice = logSlice.Prepend(offsetRecord).ToList();

            return logSlice.OrderBy(x => x.ExactTimestamp);
        }

        public async Task SaveLastLogsAsync(DateTime from, string datasetName)
        {
            var logfolder = Path.Combine(_env.ContentRootPath, $"OutputData");
            var logfile = Path.Combine(_env.ContentRootPath, $"OutputData/{datasetName}.json");

            if (!Directory.Exists(logfolder))
                Directory.CreateDirectory(logfolder);

            var logs = await GetLogsAsync(from);

            var logContent = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(logfile, logContent);
        }

        public IEnumerable<OutputDataFile> GetOutputDataFiles(string? prefix = null)
        {
            var logfolder = Path.Combine(_env.ContentRootPath, $"OutputData");

            if (!Directory.Exists(logfolder))
                return new List<OutputDataFile>();

            var outputDataFiles = 
                Directory.EnumerateFiles(logfolder, "*.json").Select(x =>
                    new OutputDataFile
                    {
                        Filename = x,
                        Name = Path.GetFileNameWithoutExtension(x)
                    });
            
            if (prefix != null)
                outputDataFiles = outputDataFiles.Where(x => x.Name.StartsWith(prefix));

            return outputDataFiles
                .OrderBy(x => x.Name)
                .ToList();
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
                where l.Reference != null
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

                var bcDetailsLog = refGroup.Logs.FirstOrDefault(x => x.Operation == LogOperation.BC_DETAILS);
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

                if (bcDetailsLog != null)
                {
                    var tokensGroup = bcDetailsLog.Message.Split(":");
                    var tokensValues = tokensGroup.Last().Trim(' ', '[', ']').Split(";");

                    analysisRecord.BarcodeWidthMod = int.Parse(tokensValues[0]);
                    analysisRecord.BarcodeHeightMod = int.Parse(tokensValues[1]);
                    analysisRecord.BarcodeWidthPx = int.Parse(tokensValues[2]);
                    analysisRecord.BarcodeHeightPx = int.Parse(tokensValues[3]);

                    analysisRecord.ModuleWidthPx = double.Parse(tokensValues[4]);
                    analysisRecord.ModuleHeightPx = double.Parse(tokensValues[5]);
                    analysisRecord.ModuleWidthMm = double.Parse(tokensValues[6]);
                    analysisRecord.ModuleHeightMm = double.Parse(tokensValues[7]);

                    analysisRecord.BarcodeWidthMm = double.Parse(tokensValues[8]);
                    analysisRecord.BarcodeHeightMm = double.Parse(tokensValues[9]);

                    analysisRecord.PixelMmRatioX = double.Parse(tokensValues[10]);
                    analysisRecord.PixelMmRatioY = double.Parse(tokensValues[11]);
                }

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

        public void ExportAnalyzedDataFile(AnalyzedDataFile analyzedFile)
        {
            var exportfolder = Path.Combine(_env.ContentRootPath, $"AnalyzedData");
            var exportfile = Path.Combine(_env.ContentRootPath, $"AnalyzedData/{analyzedFile.DatasetName}.json");

            var batchNumber = 1;
            foreach (var fileName in analyzedFile.FileNames.OrderBy(x => x)) 
            {
                var rawDataFile = GetOutputDataFile(fileName);
                var name = Path.GetFileNameWithoutExtension(fileName);
                
                var batch = new AnalyzedDataBatch
                {
                    Records = rawDataFile.AnalysisRecords,
                    BatchNumber = batchNumber
                };

                var nameTokens = name.Split("-");

                var bcTypeToken = nameTokens.FirstOrDefault();
                if (bcTypeToken != null)
                {
                    batch.BarcodeType = bcTypeToken;
                }

                var itemsToken = nameTokens.FirstOrDefault(t => t.StartsWith("items"));
                if (itemsToken != null)
                {
                    int.TryParse(itemsToken.Substring(5), out int itemCount);
                    batch.ItemCount = itemCount;
                }

                var sizeToken = nameTokens.FirstOrDefault(t => t.StartsWith("size"));
                if (sizeToken != null)
                {
                    int.TryParse(sizeToken.Substring(4), out int size);
                    batch.Size = size;
                }

                var distToken = nameTokens.FirstOrDefault(t => t.StartsWith("dist"));
                if (distToken != null)
                {
                    int.TryParse(distToken.Substring(4), out int dist);
                    batch.Distance = dist;
                }

                analyzedFile.Batches.Add(batch);

                batchNumber++;
            }

            var content = JsonSerializer.Serialize(analyzedFile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(exportfile, content);
        }
    }
}
