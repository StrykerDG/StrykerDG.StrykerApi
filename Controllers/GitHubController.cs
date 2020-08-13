using Microsoft.AspNetCore.Mvc;
using StrykerDG.StrykerServices.GitHubService;
using StrykerDG.StrykerServices.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StrykerDG.StrykerApi.Controllers
{
    [ApiController]
    [Route("GitHub")]
    public class GitHubController : ControllerBase
    {
        private IStrykerService _service { get; set; }

        public GitHubController(IStrykerService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetSomething()
        {
            var result = await _service.Get("users/strykerdg");
            return Ok(result);
        }
    }
}
