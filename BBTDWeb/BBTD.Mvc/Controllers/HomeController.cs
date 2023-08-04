using AutoMapper;
using BBTD.DB.Repository;
using BBTD.Mvc.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.NetworkInformation;
using ZXing;
using ZXing.Common;
using System.Text.Json;
using BBTD.Mvc.Services;
using Microsoft.AspNetCore.SignalR;
using BBTD.Mvc.Hubs;
using BBTD.DB.Models;

namespace BBTD.Mvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHubContext<MessageHub> _messageHubContext;
        private readonly IPersonRepo _personRepo;
        private readonly IMapper _mapper;
        private readonly IBarcodeGenerator _barcodeGenerator;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IHubContext<MessageHub> messageHubContext, IPersonRepo personRepo, IMapper mapper, IBarcodeGenerator barcodeGenerator, ILogger<HomeController> logger)
        {
            _messageHubContext = messageHubContext;
            _personRepo = personRepo;
            _mapper = mapper;
            _barcodeGenerator = barcodeGenerator;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var people = _personRepo.GetPeople();
            var vmPeople = _mapper.Map<IEnumerable<BBTD.DB.Models.Person>, IEnumerable<BBTD.Mvc.Models.Person>>(people);

            return View("PersonList", vmPeople);
        }

        public IActionResult Details(int id)
        {
            var person = _personRepo.GetPerson(id);
            var vmPerson = _mapper.Map<BBTD.DB.Models.Person, BBTD.Mvc.Models.Person>(person);

            return View("PersonDetails", vmPerson);
        }

        public IActionResult BarcodeSlideshow()
        {
            var data = new BarcodeSlideshowData
            {
                DataCount = _personRepo.GetPeopleCount(),
                BarcodeId = 1
            };

            return View("BarcodeSlideshow", data);
        }

        public IActionResult Barcode(int id)
        {
            var dbPerson = _personRepo.GetPerson(id);
            var vmPerson = _mapper.Map<BBTD.DB.Models.Person, BBTD.Mvc.Models.Person>(dbPerson);
            var jsonPerson = JsonSerializer.Serialize(vmPerson);
            var imageData = _barcodeGenerator.GenerateBarcode(jsonPerson);

            return new FileContentResult(imageData, "application/octet-stream")
            {
                FileDownloadName = $"barcode-{id}.png"
            };
        }


        [HttpPost]
        public async Task<IActionResult> Reader([FromBody] BBTD.Mvc.Models.Person person)
        {
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

            await _messageHubContext.Clients.All.SendAsync(
                "Reading", 
                person.Id,
                isReadingCorrect);

            return Json(isReadingCorrect);
        }

    }
}