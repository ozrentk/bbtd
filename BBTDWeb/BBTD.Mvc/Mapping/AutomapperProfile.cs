using AutoMapper;

namespace BBTD.Mvc.Mapping
{
    public class AutomapperProfile : Profile
    {
        public AutomapperProfile()
        {
            CreateMap<BBTD.DB.Models.Person, BBTD.Mvc.Models.Person>();
        }
    }
}
