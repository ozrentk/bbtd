using BBTD.Mvc.Models;
using BBTD.Mvc.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using ZXing;

namespace BBTD.Mvc.Controllers
{
    public class SetupController : Controller
    {
        private readonly ISetupRepo _setupRepo;
        private readonly IBarcodeGenerator _barcodeGenerator;

        public SetupController(ISetupRepo setupRepo, IBarcodeGenerator barcodeGenerator)
        {
            _setupRepo = setupRepo;
            _barcodeGenerator = barcodeGenerator;
        }

        public IActionResult Index()
        {
            var setupData = _setupRepo.GetData();
            return View(setupData);
        }

        [HttpPost]
        public IActionResult Index(SetupData setupData)
        {
            if (!ModelState.IsValid)
                return View(setupData);

            if (!setupData.IsRefresh)
                _setupRepo.SetData(setupData);
            else
                _setupRepo.ResetData();

            return RedirectToAction();
        }

        public IActionResult AppConfigurationBarcode()
        {
            var setupData = _setupRepo.GetRawJson();

            if (setupData == null)
                return NotFound();

            var imageData = _barcodeGenerator.GenerateBarcode(
                setupData,
                200,
                BarcodeFormat.QR_CODE);

            return new FileContentResult(imageData, "application/octet-stream")
            {
                FileDownloadName = $"app-config-barcode-{Guid.NewGuid()}.png"
            };
        }
    }
}
