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

namespace BBTD.Mvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly IPersonRepo _personRepo;
        private readonly IMapper _mapper;
        private readonly IBarcodeGenerator _barcodeGenerator;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IPersonRepo personRepo, IMapper mapper, IBarcodeGenerator barcodeGenerator, ILogger<HomeController> logger)
        {
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

        public IActionResult Barcode(int id)
        {
            var person = _personRepo.GetPerson(id);
            var vmPerson = _mapper.Map<BBTD.DB.Models.Person, BBTD.Mvc.Models.Person>(person);
            var jsonPerson = JsonSerializer.Serialize(vmPerson);
            var imageData = _barcodeGenerator.GenerateBarcode(jsonPerson);

            return new FileContentResult(imageData, "application/octet-stream")
            {
                FileDownloadName = $"barcode-{id}.png"
            };
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

        public IActionResult BarcodeSlideshowPartial(int id)
        {
            var person = _personRepo.GetPerson(id);
            var vmPerson = _mapper.Map<BBTD.DB.Models.Person, BBTD.Mvc.Models.Person>(person);
            var jsonPerson = JsonSerializer.Serialize(vmPerson);
            var imageData = _barcodeGenerator.GenerateBarcode(jsonPerson);

            return new FileContentResult(imageData, "application/octet-stream")
            {
                FileDownloadName = $"barcode-{id}.png"
            };
        }
    }
}