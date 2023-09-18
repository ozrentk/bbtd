using AutoMapper;
using BBTD.DB.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace BBTD.Mvc.Controllers
{
    public class SourceDataController : Controller
    {
        private readonly IPersonRepo _personRepo;
        private readonly IMapper _mapper;

        public SourceDataController(IPersonRepo personRepo, IMapper mapper)
        {
            _personRepo = personRepo;
            _mapper = mapper;
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
    }
}
