using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BBTD.Mvc.Models
{
    public class OutputDataFile
    {
        public string Filename { get; set; }
        public string Name { get; set; }
        public List<NLogRecord> LogRecords { get; set; }
        public List<AnalysisRecord> AnalysisRecords { get; set; }
    }
}
