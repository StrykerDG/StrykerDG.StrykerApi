using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StrykerDG.StrykerApi.Configuration
{
    public class ApiSettings
    {
        public string[] CORS { get; set; }
        public SecuritySettings SecuritySettings { get; set; }
    }
}
