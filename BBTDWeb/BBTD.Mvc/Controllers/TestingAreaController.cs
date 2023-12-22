using AutoMapper;
using BBTD.DB.Repository;
using BBTD.Mvc.Hubs;
using BBTD.Mvc.Models;
using BBTD.Mvc.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using LogLevel = BBTD.Mvc.NLogExtensions.LogLevel;

namespace BBTD.Mvc.Controllers
{
    public class TestingAreaController : Controller
    {
        private readonly ISetupRepo _setupRepo;
        private readonly ILogForwarder _logForwarder;
        private readonly IPersonRepo _personRepo;
        private readonly IBarcodeGenerator _barcodeGenerator;
        private readonly IMapper _mapper;
        private readonly IHubContext<MessageHub> _messageHubContext;

        public TestingAreaController(
            ILogForwarder logForwarder,
            ISetupRepo setupRepo,
            IPersonRepo personRepo,
            IBarcodeGenerator barcodeGenerator,
            IMapper mapper,
            IHubContext<MessageHub> messageHubContext)
        {
            _setupRepo = setupRepo;
            _logForwarder = logForwarder;
            _personRepo = personRepo;
            _barcodeGenerator = barcodeGenerator;
            _mapper = mapper;
            _messageHubContext = messageHubContext;
        }

        public IActionResult Index()
        {
            _logForwarder.LogForWebServer("Starting BarcodeSlideshow", LogLevel.Info);

            var setupData = _setupRepo.GetData();
            var data = new BarcodeSlideshowData
            {
                DataCount = setupData.NumberOfItems,
                BarcodeType = setupData.BarcodeType,
                BarcodeSize = setupData.BarcodeSize,
                TimeoutMilliseconds = setupData.ServerTimeoutMilliseconds,
                DistanceFromScreen = setupData.DistanceFromScreen
            };

            return View(data);
        }

        [HttpPost]
        public IActionResult Index(BarcodeSlideshowData data)
        {
            if (!ModelState.IsValid)
                return View(data);

            var setupData = _setupRepo.GetData();
            setupData.DistanceFromScreen = data.DistanceFromScreen;
            setupData.BarcodeType = data.BarcodeType;
            _setupRepo.SetData(setupData);

            return RedirectToAction();
        }

        public IActionResult Barcode(int id)
        {
            _logForwarder.LogForWebServer(
                $"[Barcode id={id}] Received image request from UI on server",
                BBTD.Mvc.NLogExtensions.LogLevel.Debug,
                id,
                LogOperation.IMG_REQ_RECV);

            var dbPerson = _personRepo.GetPerson(id);
            var vmPerson = _mapper.Map<BBTD.DB.Models.Person, BBTD.Mvc.Models.Person>(dbPerson);
            var jsonPerson = JsonSerializer.Serialize(vmPerson);

            var setupData = _setupRepo.GetData();

            var imageData = _barcodeGenerator.GenerateBarcode(jsonPerson, setupData.BarcodeSize, setupData.BarcodeType);
            _logForwarder.LogForWebServer($"[Barcode id={id}] Image generated on server, returning response", BBTD.Mvc.NLogExtensions.LogLevel.Debug, id, LogOperation.IMG_GEN);

            return new FileContentResult(imageData, "application/octet-stream")
            {
                FileDownloadName = $"barcode-{id}-{setupData.BarcodeSize}-{Guid.NewGuid()}.png"
            };
        }

        public IActionResult BarcodeDetails(int id)
        {
            var dbPerson = _personRepo.GetPerson(id);
            var vmPerson = _mapper.Map<BBTD.DB.Models.Person, BBTD.Mvc.Models.Person>(dbPerson);
            var jsonPerson = JsonSerializer.Serialize(vmPerson);

            var setupData = _setupRepo.GetData();

            (int bcWidthMod, int bcHeightMod, int bcWidthPx, int bcHeightPx) = _barcodeGenerator.GetBarcodeRawSizes(jsonPerson, setupData.BarcodeSize, setupData.BarcodeType);
            var moduleWidthPx = (double)bcWidthPx / bcWidthMod;
            var moduleHeightPx = (double)bcHeightPx / bcHeightMod;

            double? moduleWidthMm = default;
            double? moduleHeightMm = default;
            double? bcWidthMm = default;
            double? bcHeightMm = default;
            double? pxMmWidthRatio = default;
            double? pxMmHeightRatio = default;
            bool isMmAvailable = false;
            if (setupData.ScreenWidthMm.HasValue && 
                setupData.ScreenHeightMm.HasValue &&
                setupData.ScreenWidthPx.HasValue && 
                setupData.ScreenHeightPx.HasValue)
            {
                isMmAvailable = true;
                moduleWidthMm = ((double)setupData.ScreenWidthMm / setupData.ScreenWidthPx) * moduleWidthPx;
                moduleHeightMm = ((double)setupData.ScreenHeightMm / setupData.ScreenHeightPx) * moduleHeightPx;
                bcWidthMm = ((double)setupData.ScreenWidthMm / setupData.ScreenWidthPx) * bcWidthPx;
                bcHeightMm = ((double)setupData.ScreenHeightMm / setupData.ScreenHeightPx) * bcHeightPx;
                pxMmWidthRatio = ((double)setupData.ScreenWidthPx / setupData.ScreenWidthMm);
                pxMmHeightRatio = ((double)setupData.ScreenHeightPx / setupData.ScreenHeightMm);
            }

            if (isMmAvailable)
            {
                _logForwarder.LogForWebServer(
                    $"[Barcode id={id}] Barcode details generated on server, returning response: [{bcWidthMod};{bcHeightMod};{bcWidthPx};{bcHeightPx};{moduleWidthPx};{moduleHeightPx};{moduleWidthMm};{moduleHeightMm};{bcWidthMm};{bcHeightMm};{pxMmWidthRatio};{pxMmHeightRatio}]",
                    BBTD.Mvc.NLogExtensions.LogLevel.Debug,
                    id,
                    LogOperation.BC_DETAILS);
            }

            return Json(
                new { 
                    bcWidthMod, bcHeightMod, 
                    bcWidthPx, bcHeightPx, 
                    moduleWidthPx, moduleHeightPx,
                    moduleWidthMm, moduleHeightMm,
                    bcWidthMm, bcHeightMm,
                    pxMmWidthRatio, pxMmHeightRatio,
                    isMmAvailable
                });
        }

        [HttpPost]
        public async Task<IActionResult> DataFromReader([FromBody] BBTD.Mvc.Models.Person person)
        {
            bool isReadingCorrect = false;

            if (person == null)
            {
                _logForwarder.LogForWebServer($"Received empty barcode reader data", BBTD.Mvc.NLogExtensions.LogLevel.Debug);

                await _messageHubContext.Clients.All.SendAsync("ClientReadTimeout");
            }
            else
            { 
                _logForwarder.LogForWebServer($"[Barcode id={person.Id}] Barcode reader data received on server", BBTD.Mvc.NLogExtensions.LogLevel.Debug, person.Id, LogOperation.BC_DATA_RECV);

                var dbPerson = _personRepo.GetPerson(person.Id);
                var vmPerson = _mapper.Map<BBTD.DB.Models.Person, BBTD.Mvc.Models.Person>(dbPerson);

                if (vmPerson.CreatedAt == person.CreatedAt &&
                    vmPerson.IsActive == person.IsActive &&
                    vmPerson.FirstName == person.FirstName &&
                    vmPerson.LastName == person.LastName &&
                    vmPerson.Email == person.Email &&
                    vmPerson.Description == person.Description)
                {
                    isReadingCorrect = true;
                }
                else if (person.IsForce)
                {
                    isReadingCorrect = true;
                }
                else
                {
                    isReadingCorrect = false;
                }

                _logForwarder.LogForWebServer($"[Barcode id={person.Id}] Informing UI that barcode is read succesfully", BBTD.Mvc.NLogExtensions.LogLevel.Debug, person.Id, LogOperation.BC_NOTIFY);
                await _messageHubContext.Clients.All.SendAsync(
                    "Reading",
                    person.Id,
                    isReadingCorrect);
            }

            return Json(isReadingCorrect);
        }
    }
}
