using System;
using System.Text.Json.Serialization;

namespace BBTDApp.Models
{
    public class LogRecord
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("levelNumber")]
        public int LevelNumber { get; set; }

        [JsonPropertyName("exact_ts")]
        public DateTime ExactTimestamp { get; set; }

        [JsonPropertyName("reference")]
        public int? Reference { get; set; }

        [JsonPropertyName("operation")]
        public string Operation { get; set; }
    }
}