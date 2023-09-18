using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BBTD.Mvc.Models
{
    public class NLogRecord
    {
        [JsonPropertyName("m")]
        public string Message { get; set; }

        [JsonPropertyName("l")]
        [DisplayName("Level")]
        public string LevelString { get; set; }

        //[JsonIgnore]
        //public NLog.LogLevel Level => NLog.LogLevel.FromString(LevelString);

        [JsonPropertyName("t")]
        [DisplayName("Timestamp")]
        public DateTime ExactTimestamp { get; set; }

        [JsonPropertyName("a")]
        public string Application { get; set; }

        [JsonPropertyName("r")]
        public string? ReferenceString { get; set; }

        public int? Reference =>
            ReferenceString == null ? null : int.Parse(ReferenceString);

        [JsonPropertyName("o")]
        public string? Operation { get; set; }
    }
}
