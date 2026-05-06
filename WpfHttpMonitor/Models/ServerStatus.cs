using System;

namespace WpfHttpMonitor.Models
{
    public class ServerStatus
    {
        public int TotalRequests { get; set; }
        public int GetRequests { get; set; }
        public int PostRequests { get; set; }
        public TimeSpan Uptime { get; set; }
        public double AverageProcessingTimeMs { get; set; }
    }
}
