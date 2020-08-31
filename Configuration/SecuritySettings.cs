using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StrykerDG.StrykerApi.Configuration
{
    public class SecuritySettings
    {
        public string GitHubToken { get; set; }
        public string GitHubUserAgent { get; set; }

        public string TwitchClientId { get; set; }
        public string TwitchClientSecret { get; set; }

        public string ClockifyKey { get; set; }
    }
}
