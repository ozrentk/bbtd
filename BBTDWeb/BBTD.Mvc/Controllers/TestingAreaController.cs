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
                TimeoutMilliseconds = setupData.TimeoutMilliseconds,
            };

            return View(data);
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

        [HttpPost]
        public async Task<IActionResult> Reader([FromBody] BBTD.Mvc.Models.Person person)
        {
            _logForwarder.LogForWebServer($"[Barcode id={person.Id}] Barcode reader data received on server", BBTD.Mvc.NLogExtensions.LogLevel.Debug, person.Id, LogOperation.BC_DATA_RECV);

            var dbPerson = _personRepo.GetPerson(person.Id);
            var vmPerson = _mapper.Map<BBTD.DB.Models.Person, BBTD.Mvc.Models.Person>(dbPerson);

            bool isReadingCorrect = false;
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

            return Json(isReadingCorrect);
        }
    }
}
