using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WpfHttpMonitor.Models;

namespace WpfHttpMonitor.Services
{
    public class HttpServerService
    {
        private HttpListener _listener;
        private bool _isRunning = false;
        private readonly ConcurrentBag<LogEntry> _logs = new ConcurrentBag<LogEntry>();
        private readonly ConcurrentDictionary<string, string> _savedMessages = new ConcurrentDictionary<string, string>();
        
        public event Action<LogEntry>? OnLogReceived;
        private DateTime _startTime;

        public void Start(int port)
        {
            if (_isRunning) return;

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            
            try
            {
                _listener.Start();
                _isRunning = true;
                _startTime = DateTime.Now;
                
                // Запуск прослушивания в фоновом потоке
                Task.Run(() => ListenAsync());
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка запуска сервера. Возможно, требуются права администратора.\nДетали: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;
            
            _isRunning = false;
            _listener.Stop();
            _listener.Close();
        }

        private async Task ListenAsync()
        {
            while (_isRunning)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    // Многопоточная обработка запросов:
                    // Каждый запрос обрабатывается в пуле потоков независимо от других
                    ThreadPool.QueueUserWorkItem(async _ => await ProcessRequestAsync(context));
                }
                catch (HttpListenerException)
                {
                    // Игнорируем исключение, возникающее при остановке сервера
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка при получении запроса: {ex.Message}");
                }
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            var sw = Stopwatch.StartNew();
            var request = context.Request;
            var response = context.Response;

            var logEntry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Method = request.HttpMethod,
                Url = request.Url?.ToString() ?? string.Empty,
                Headers = request.Headers.ToString(),
                IsIncoming = true,
                RequestType = request.HttpMethod
            };

            try
            {
                string requestBody = "";
                if (request.HasEntityBody)
                {
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    requestBody = await reader.ReadToEndAsync();
                    logEntry.RequestBody = requestBody;
                }

                string responseBody = "";
                
                // Обработка GET-запроса (возврат статистики сервера)
                if (request.HttpMethod == "GET")
                {
                    var status = GetStatus();
                    responseBody = JsonSerializer.Serialize(status);
                    response.ContentType = "application/json";
                    response.StatusCode = (int)HttpStatusCode.OK;
                }
                // Обработка POST-запроса (сохранение сообщения)
                else if (request.HttpMethod == "POST")
                {
                    try
                    {
                        var json = JsonSerializer.Deserialize<JsonElement>(requestBody);
                        if (json.TryGetProperty("message", out var messageProp))
                        {
                            var msg = messageProp.GetString() ?? "";
                            var id = Guid.NewGuid().ToString();
                            _savedMessages.TryAdd(id, msg);
                            
                            responseBody = JsonSerializer.Serialize(new { id = id, status = "Сообщение успешно сохранено" });
                            response.StatusCode = (int)HttpStatusCode.Created;
                        }
                        else
                        {
                            responseBody = JsonSerializer.Serialize(new { error = "Поле 'message' не найдено в JSON" });
                            response.StatusCode = (int)HttpStatusCode.BadRequest;
                        }
                    }
                    catch
                    {
                        responseBody = JsonSerializer.Serialize(new { error = "Некорректный формат JSON" });
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                    response.ContentType = "application/json";
                }
                else
                {
                    responseBody = JsonSerializer.Serialize(new { error = "Метод не поддерживается сервером" });
                    response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    response.ContentType = "application/json";
                }

                byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                
                logEntry.ResponseBody = responseBody;
                logEntry.ResponseStatus = response.StatusCode.ToString();
            }
            catch (Exception ex)
            {
                logEntry.ResponseStatus = "500";
                logEntry.ResponseBody = ex.Message;
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            finally
            {
                sw.Stop();
                logEntry.DurationMs = sw.ElapsedMilliseconds;
                
                _logs.Add(logEntry);
                OnLogReceived?.Invoke(logEntry);
                
                SaveLogToFile(logEntry);
                
                response.Close();
            }
        }

        private void SaveLogToFile(LogEntry log)
        {
            try
            {
                var logLine = $"[{log.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {log.Method} {log.Url} | Статус: {log.ResponseStatus} | Время: {log.DurationMs}мс\n";
                // Дозапись в файл
                File.AppendAllText("logs.txt", logLine);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка записи лога в файл: {ex.Message}");
            }
        }

        public ServerStatus GetStatus()
        {
            int total = 0, get = 0, post = 0;
            long totalMs = 0;
            
            // Получаем моментальный снимок для статистики
            var logsArray = _logs.ToArray();
            
            foreach (var log in logsArray)
            {
                if (!log.IsIncoming) continue;
                total++;
                if (log.Method == "GET") get++;
                if (log.Method == "POST") post++;
                totalMs += log.DurationMs;
            }

            return new ServerStatus
            {
                TotalRequests = total,
                GetRequests = get,
                PostRequests = post,
                Uptime = _isRunning ? DateTime.Now - _startTime : TimeSpan.Zero,
                AverageProcessingTimeMs = total > 0 ? (double)totalMs / total : 0
            };
        }
    }
}
