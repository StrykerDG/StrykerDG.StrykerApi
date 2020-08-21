using Akka.Actor;
using Microsoft.AspNetCore.Mvc;
using StrykerDG.StrykerActors.Twitch.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StrykerDG.StrykerApi.Controllers
{
    [ApiController]
    [Route("Twitch")]
    public class TwitchController : ControllerBase
    {
        private IActorRef _twitchActor { get; set; }
        
        public TwitchController(IEnumerable<IActorRef> actorRefs)
        {
            _twitchActor = actorRefs
                .Where(ar => ar.Path.ToString().Contains("TwitchActor"))
                .FirstOrDefault();
        }

        [HttpGet]
        [Route("User/{username}")]
        public async Task<IActionResult> GetUser(string username)
        {
            var result = await _twitchActor.Ask(new AskForTwitchUserProfile(username));
            return Ok(result);
        }
    }
}
