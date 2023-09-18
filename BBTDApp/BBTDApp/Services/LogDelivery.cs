using BBTDApp.Models;
using BBTDApp.NLogEx;
using Java.Lang.Ref;
using Java.Util.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BBTDApp.Services
{
    public class LogDelivery
    {
        private readonly string _deliveryAddress;
        private readonly JsonSerializerOptions _defaultSerializerOptions;

        private List<Models.LogRecord> _logCache = new List<Models.LogRecord>();

        public bool IsActive { get; set; }

        public LogDelivery(string deliveryAddress)
        {
            _deliveryAddress = deliveryAddress;
            IsActive = !string.IsNullOrEmpty(_deliveryAddress);

            _defaultSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        private static JsonSerializerOptions DefaultSerializerOptions() =>
            new JsonSerializerOptions { 
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault 
            };

        private HttpClient _httpClient;
        
        private HttpClient HttpClientFactory()
        {
            if(_httpClient == null)
                _httpClient = new HttpClient();
            
            return _httpClient;
        }

        private static Models.LogRecord ToLogRecord(string message, int levelNumber, int? reference, string operation) =>
            new Models.LogRecord
            {
                Message = message,
                LevelNumber = levelNumber,
                ExactTimestamp = DateTime.UtcNow,
                Reference = reference,
                Operation = operation
            };

        private static StringContent ToJson(Models.LogRecord logRecord)
        {
            var options = DefaultSerializerOptions();
            var json = JsonSerializer.Serialize(logRecord, options);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return content;
        }

        private static StringContent ToJson(IEnumerable<Models.LogRecord> logRecords)
        {
            var options = DefaultSerializerOptions();
            var json = JsonSerializer.Serialize(logRecords, options);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return content;
        }

        public async Task LogAsync(string message, int level, int? reference = null, string operation = null)
        {
            if (!IsActive)
                return;

            try
            {
                var record = ToLogRecord(message, level, reference, operation);
                var content = ToJson(new[] { record });

                var client = HttpClientFactory();
                var resp = await client.PostAsync($"{_deliveryAddress}/api/logdelivery/frommobile", content);
                if (!resp.IsSuccessStatusCode)
                {
                    Console.WriteLine("Error: HttpClient log delivery failed");
                    Console.WriteLine($"{resp.StatusCode}: {resp.ReasonPhrase}");
                    var body = await resp.Content.ReadAsStringAsync();
                    Console.WriteLine($"{body}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: LogAsync failed");
                Console.WriteLine(ex.Message);
            }
        }

        public void AddLog(string message, int level, int? reference = null, string operation = null)
        {
            if (!IsActive)
                return;

            try
            {
                Console.WriteLine(message);

                var record = ToLogRecord(message, level, reference, operation);
                _logCache.Add(record);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: AddLog failed");
                Console.WriteLine(ex.Message);
            }
        }

        public async Task DeliverLogsAsync()
        {
            if (!IsActive)
                return;

            try
            {
                if (_logCache.Count == 0)
                    return;

                var content = ToJson(_logCache);
                _logCache = new List<Models.LogRecord>();

                var client = HttpClientFactory();
                var resp = await client.PostAsync($"{_deliveryAddress}/api/logdelivery/frommobile", content);
                if (!resp.IsSuccessStatusCode)
                {
                    Console.WriteLine("Error: HttpClient log batch delivery failed");
                    Console.WriteLine($"{resp.StatusCode}: {resp.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: DeliverLogsAsync failed");
                Console.WriteLine(ex.Message);
            }
        }

        public async void CalculateOffset()
        {
            try
            {
                var client = HttpClientFactory();

                var deltas = new List<int>();
                for (int i = 0; i < 3; i++)
                {
                    var startTime = DateTime.UtcNow.ToLocalTime();
                    var resp = await client.GetAsync($"{_deliveryAddress}/api/logdelivery/sync");
                    var endTime = DateTime.UtcNow.ToLocalTime();
                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Error: HttpClient time sync failed");
                        Console.WriteLine($"{resp.StatusCode}: {resp.ReasonPhrase}");
                    }
                    else
                    {
                        var content = (await resp.Content.ReadAsStringAsync()).Trim('\"');
                        var serverTime = DateTime.Parse(content).ToLocalTime();
                        var delta = serverTime - endTime;
                        Console.WriteLine($"Delta: {delta.Milliseconds}ms ({startTime.ToString("HH:mm:ss.fff")} -> {serverTime.ToString("HH:mm:ss.fff")})");
                        deltas.Add(delta.Milliseconds);
                    }
                }

                var syncOffset = (int)deltas.Average();
                await LogAsync($"App offset: {syncOffset}", LogLevel.Info, null, LogOperation.APP_OFFSET);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: DeliverLogsAsync failed");
                Console.WriteLine(ex.Message);
            }
        }
    }
}
