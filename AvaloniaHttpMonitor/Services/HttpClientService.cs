using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AvaloniaHttpMonitor.Models;

namespace AvaloniaHttpMonitor.Services;

public class HttpClientService
{
    private readonly HttpClient _client = new();
    public event Action<LogEntry>? OnLogReceived;

    public async Task<string> SendRequestAsync(string url, string method, string body)
    {
        var sw = Stopwatch.StartNew();
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Url = url,
            Method = method,
            IsIncoming = false,
            RequestType = method,
            RequestBody = body
        };

        try
        {
            HttpResponseMessage response;
            if (method.ToUpper() == "GET")
            {
                response = await _client.GetAsync(url);
            }
            else if (method.ToUpper() == "POST")
            {
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                logEntry.Headers = content.Headers.ToString();
                response = await _client.PostAsync(url, content);
            }
            else
            {
                throw new NotSupportedException("Указанный метод не поддерживается данным клиентом.");
            }

            string responseBody = await response.Content.ReadAsStringAsync();
            
            logEntry.ResponseStatus = ((int)response.StatusCode).ToString();
            logEntry.ResponseBody = responseBody;

            sw.Stop();
            logEntry.DurationMs = sw.ElapsedMilliseconds;
            
            OnLogReceived?.Invoke(logEntry);
            return responseBody;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logEntry.ResponseStatus = "Error";
            logEntry.ResponseBody = ex.Message;
            logEntry.DurationMs = sw.ElapsedMilliseconds;
            
            OnLogReceived?.Invoke(logEntry);
            throw;
        }
    }
}
