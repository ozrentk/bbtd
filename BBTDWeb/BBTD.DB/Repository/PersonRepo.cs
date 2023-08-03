using BBTD.DB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBTD.DB.Repository
{
    public interface IPersonRepo
    {
        IEnumerable<Person> GetPeople();
        int GetPeopleCount();
        Person GetPerson(int id);
    }

    public class PersonRepo : IPersonRepo
    {
        private readonly BbtddbContext _context;

        public PersonRepo(BbtddbContext context)
        {
            _context = context;
        }

        public IEnumerable<Person> GetPeople()
        {
            return _context.People.ToList();
        }

        public int GetPeopleCount()
        {
            return _context.People.Count();
        }

        public Person GetPerson(int id)
        {
            return _context.People.Single(x => x.Id == id);
        }
    }
}
