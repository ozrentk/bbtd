using BBTD.Mvc.Models;
using BBTD.Mvc.Services;
using Microsoft.AspNetCore.Mvc;
using ScottPlot;
using ScottPlot.Statistics;
using System;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BBTD.Mvc.Controllers
{
    public class LogAnalysisController : Controller
    {
        private readonly ILogFileHandler _logHandler;

        public LogAnalysisController(ILogFileHandler logHandler)
        {
            _logHandler = logHandler;
        }

        /// <summary>
        /// List of logs that represent starts of batches
        /// </summary>
        public async Task<IActionResult> LogBatchStartList()
        {
            var logBatchStarts = await _logHandler.GetLogStartsAsync();

            return View(logBatchStarts);
        }

        /// <summary>
        /// List of logs inside the batch, that start with a start log which contains a given timestamp.
        /// It stops with next start log or with the end of the file.
        /// </summary>
        public async Task<IActionResult> LogList(DateTime from)
        {
            var lastLogs = await _logHandler.GetLogsAsync(from);
            ViewBag.From = from;

            return View(lastLogs);
        }

        /// <summary>
        /// Save the list of logs inside the batch (that starts with a start log which contains a given timestamp) with a particular filename.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveLogData(string datasetName, DateTime from)
        {
            await _logHandler.SaveLastLogsAsync(from, datasetName);

            return RedirectToAction("OutputData");
        }

        /// <summary>
        /// Get all saved batches of logs.
        /// </summary>
        public async Task<IActionResult> OutputData()
        {
            var outputDataFiles = _logHandler.GetOutputDataFiles();

            return View(outputDataFiles);
        }

        /// <summary>
        /// Remove saved batch of logs.
        /// </summary>
        public async Task<IActionResult> OutputDataDelete(OutputDataFile outputDataFile)
        {
            return View(outputDataFile);
        }

        /// <summary>
        /// Confirm removal of saved batch of logs.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> OutputDataDelete(string name)
        {
            _logHandler.DeleteOutputDataFile(name);

            return RedirectToAction("OutputData");
        }

        public async Task<IActionResult> OutputDataDetails(OutputDataFile outputDataFile)
        {
            var data = _logHandler.GetOutputDataFile(outputDataFile.Name);

            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> ExportAnalyzedData([FromBody]AnalyzedDataFile dataFile)
        {
            //var data = _logHandler.GetOutputDataFile(outputDataFile.Name);
            _logHandler.ExportAnalyzedDataFile(dataFile);

            return Json(new { });
        }

        public async Task<IActionResult> TimeSeries(string name)
        {
            (double[] dataX, double[] dataY) = GetOutputData(name);

            var plt = new ScottPlot.Plot(400, 300);
            plt.AddScatter(dataX, dataY);

            return ReturnPlotImage(plt);
        }

        public async Task<IActionResult> Distribution(string name)
        {
            (_, double[] dataY) = GetOutputData(name);

            // clean up data from error cases
            dataY = dataY.Where(x => x > 0).ToArray();

            var plt = new ScottPlot.Plot(400, 300);

            var hist = new Histogram(0, 1000, 100);
            hist.AddRange(dataY);

            // display histogram probabability as a bar plot
            double[] probabilities = hist.GetProbability();
            var bar = plt.AddBar(values: probabilities, positions: hist.Bins);
            bar.BarWidth = 1;
            bar.FillColor = ColorTranslator.FromHtml("#9bc3eb");
            bar.BorderColor = ColorTranslator.FromHtml("#82add9");

            // display histogram probability curve as a line plot
            plt.AddFunction(hist.GetProbabilityCurve(dataY, true), Color.Black, 2, LineStyle.Dash);

            //plt.AddBar(values: hist.Counts, positions: hist.Bins);

            // display vertical lines at points of interest
            var stats = new ScottPlot.Statistics.BasicStats(dataY);
            plt.AddVerticalLine(stats.Mean, Color.Black, 2, LineStyle.Solid, "mean");
            plt.AddVerticalLine(stats.Mean - stats.StDev, Color.Black, 2, LineStyle.Dash, "1 SD");
            plt.AddVerticalLine(stats.Mean + stats.StDev, Color.Black, 2, LineStyle.Dash);
            plt.AddVerticalLine(stats.Mean - stats.StDev * 2, Color.Black, 2, LineStyle.Dot, "2 SD");
            plt.AddVerticalLine(stats.Mean + stats.StDev * 2, Color.Black, 2, LineStyle.Dot);
            plt.AddVerticalLine(stats.Min, Color.Gray, 1, LineStyle.Dash, "min/max");
            plt.AddVerticalLine(stats.Max, Color.Gray, 1, LineStyle.Dash);
            plt.Legend(location: Alignment.UpperRight);

            // customize the plot style
            plt.SetAxisLimits(yMin: 0);

            return ReturnPlotImage(plt);
        }

        private (double[], double[]) GetOutputData(string name)
        {
            var data = _logHandler.GetOutputDataFile(name);
            var readingTimes = data.AnalysisRecords.Select(x => x.BarcodeReadingTime).ToList();

            var dataX = Enumerable.Range(0, readingTimes.Count).Select(x => Convert.ToDouble(x)).ToArray();
            var dataY = readingTimes.Select(x => Convert.ToDouble(x)).ToArray();

            return (dataX, dataY);
        }

        private static IActionResult ReturnPlotImage(Plot plt)
        {
            var imageData = plt.GetImageBytes();
            return new FileContentResult(imageData, "application/octet-stream")
            {
                FileDownloadName = $"analysis-{Guid.NewGuid()}.png"
            };
        }


    }
}
