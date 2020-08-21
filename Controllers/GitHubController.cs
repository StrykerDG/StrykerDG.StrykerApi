using Akka.Actor;
using Microsoft.AspNetCore.Mvc;
using StrykerDG.StrykerActors.GitHub.Messages;
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
        private IActorRef _githubActor { get; set; }

        public GitHubController(IEnumerable<IActorRef> actorRefs)
        {
            _githubActor = actorRefs
                .Where(ar => ar.Path.ToString().Contains("GitHubActor"))
                .FirstOrDefault();
        }

        [HttpGet]
        [Route("User/{username}")]
        public async Task<IActionResult> GetUser(string username)
        {
            var result = await _githubActor.Ask(new AskForGitHubUserProfile(username));
            return Ok(result);
        }
    }
}
