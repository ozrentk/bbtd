using BBTD.Mvc.Models;
using BBTD.Mvc.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BBTD.Mvc.Controllers
{
    public class LogAnalysisController : Controller
    {
        private readonly ILogFileHandler _logHandler;

        public LogAnalysisController(ILogFileHandler logHandler)
        {
            _logHandler = logHandler;
        }

        public async Task<IActionResult> LastLogList()
        {
            var lastLogs = await _logHandler.GetLastLogsAsync();

            return View(lastLogs);
        }

        [HttpPost]
        public async Task<IActionResult> SaveLogData(string datasetName)
        {
            await _logHandler.SaveLastLogsAsync(datasetName);

            return RedirectToAction("OutputData");
        }

        public async Task<IActionResult> OutputData()
        {
            var outputDataFiles = _logHandler.GetOutputDataFiles();

            return View(outputDataFiles);
        }

        public async Task<IActionResult> OutputDataDelete(OutputDataFile outputDataFile)
        {
            return View(outputDataFile);
        }

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
    }
}
