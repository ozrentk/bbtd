using BBTD.Mvc.Models;
using NLog;
using NLog.LayoutRenderers;
using System;
using System.Text;

namespace BBTD.Mvc.NLogExtensions
{
    [LayoutRenderer("exacttimestamp")]
    public class ExactTimestampLayoutRenderer : LayoutRenderer
    {
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var parameters = logEvent.Parameters;
            if (parameters == null || parameters.Length == 0)
                return;

            var exactTimestamp = (System.DateTime)parameters[0];
            var exTsStr = exactTimestamp.ToString("yyyy-MM-ddTHH:mm:ss.fff");
            builder.Append(exTsStr);
        }
    }

    [LayoutRenderer("reference")]
    public class ReferenceLayoutRenderer : LayoutRenderer
    {
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var parameters = logEvent.Parameters;
            if (parameters == null || parameters.Length <= 1 || parameters[1] == null)
                return;

            var reference = (int)parameters[1];
            builder.Append(reference);
        }
    }

    [LayoutRenderer("operation")]
    public class OperationLayoutRenderer : LayoutRenderer
    {
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var parameters = logEvent.Parameters;
            if (parameters == null || parameters.Length <= 2 || parameters[2] == null)
                return;

            var operation = (string)parameters[2];
            builder.Append(operation);
        }
    }
}
