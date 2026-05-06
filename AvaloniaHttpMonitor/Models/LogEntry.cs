using System;

namespace AvaloniaHttpMonitor.Models;

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string RequestBody { get; set; } = string.Empty;
    public string Headers { get; set; } = string.Empty;
    public string ResponseStatus { get; set; } = string.Empty;
    public string ResponseBody { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public bool IsIncoming { get; set; }
    
    public string Direction => IsIncoming ? "Входящий" : "Исходящий";
}
