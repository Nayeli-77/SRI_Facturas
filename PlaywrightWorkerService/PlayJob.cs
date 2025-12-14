using System;
using System.Collections.Generic;
using System.Text;

namespace PlaywrightWorkerService
{
    public class PlayJob
    {
        public string? JobId { get; set; }
        public DateTime Fecha { get; set; }
        public string? Url { get; set; }
        public string? Payload { get; set; }
    }

}
