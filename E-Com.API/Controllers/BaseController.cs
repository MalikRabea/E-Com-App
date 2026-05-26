using AutoMapper;
using E_Com.Core.interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace E_Com.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BaseController : ControllerBase
    {
        protected readonly IUnitOfWork work;
        protected readonly IMapper mapper;



        public BaseController(IUnitOfWork work , IMapper mapper)
        {
            this.work = work;
            this.mapper = mapper;
        }

    }
}
