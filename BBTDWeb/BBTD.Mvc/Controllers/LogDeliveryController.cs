using BBTD.Mvc.Models;
using BBTD.Mvc.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Globalization;

namespace BBTD.Mvc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogDeliveryController : ControllerBase
    {
        //private readonly ISetupRepo _setupRepo;
        private readonly ILogForwarder _logForwarder;

        public LogDeliveryController(/*ISetupRepo setupRepo, */ILogForwarder logForwarder)
        {
            //_setupRepo = setupRepo;
            _logForwarder = logForwarder;
        }

        [HttpPost("[action]")]
        public void FromWebUI([FromBody]LogRecord[] logs)
        {
            _logForwarder.LogForWebUI(logs);
        }

        [HttpPost("[action]")]
        public void FromMobile([FromBody] LogRecord[] logs)
        {
            _logForwarder.LogForMobile(logs);
        }

        [HttpGet("[action]")]
        public IActionResult Sync()
        {
            var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture);
            return Ok(now);
        }
    }
}
