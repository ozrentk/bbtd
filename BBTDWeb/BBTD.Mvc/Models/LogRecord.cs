using System;
using System.Text.Json.Serialization;

namespace BBTD.Mvc.Models
{
    public class LogRecord
    {
        public LogRecord(string message, int levelNumber, int? reference = null, string? operation = null)
        {
            this.Message = message;

            this.LevelNumber = levelNumber;

            this.ExactTimestamp = DateTime.UtcNow;

            this.Reference = reference;

            this.Operation = operation;
        }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("levelNumber")]
        public int LevelNumber { get; set; }

        [JsonIgnore]
        public NLog.LogLevel Level => NLog.LogLevel.FromOrdinal(LevelNumber);

        [JsonPropertyName("exact_ts")]
        public DateTime ExactTimestamp { get; set; }

        [JsonPropertyName("reference")]
        public int? Reference { get; set; }

        [JsonPropertyName("operation")]
        public string? Operation { get; set; }
    }
}
