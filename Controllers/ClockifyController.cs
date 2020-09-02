using Akka.Actor;
using Microsoft.AspNetCore.Mvc;
using StrykerDG.StrykerActors.Clockify.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StrykerDG.StrykerApi.Controllers
{
    [ApiController]
    [Route("Clockify")]
    public class ClockifyController : ControllerBase
    {
        private IActorRef _clockifyActor { get; set; }

        public ClockifyController(IEnumerable<IActorRef> actorRefs)
        {
            _clockifyActor = actorRefs
                .Where(ar => ar.Path.ToString().Contains("ClockifyActor"))
                .FirstOrDefault();
        }

        [HttpGet]
        [Route("TimeEntries")]
        public async Task<IActionResult> GetUserTimeEntries()
        {
            // TODO: Impliment filters
            var result = await _clockifyActor.Ask(new AskForTimeEntries());
            return Ok(result);
        }
    }
}
