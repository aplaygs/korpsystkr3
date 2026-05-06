using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using AvaloniaHttpMonitor.Models;
using AvaloniaHttpMonitor.Services;

namespace AvaloniaHttpMonitor;

public partial class MainWindow : Window
{
    private readonly HttpServerService _serverService = new();
    private readonly HttpClientService _clientService = new();
    
    private readonly ObservableCollection<LogEntry> _allLogs = new();
    public ObservableCollection<LogEntry> FilteredLogs { get; } = new();
    
    private DispatcherTimer _statsTimer;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        LogsDataGrid.ItemsSource = FilteredLogs;
        
        _serverService.OnLogReceived += OnLogReceived;
        _clientService.OnLogReceived += OnLogReceived;
        
        _statsTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _statsTimer.Tick += UpdateStats;
        
        this.Closing += (s, e) => _serverService.Stop();
    }

    private void OnLogReceived(LogEntry log)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _allLogs.Insert(0, log);
            ApplyFilter();
            
            if (log.IsIncoming)
            {
                var title = $"👉 [{log.Timestamp:HH:mm:ss}] {log.Method} {log.Url}";
                var request = $"Заголовки: {log.Headers}";
                var details = $"<- Ответ: Статус {log.ResponseStatus} ({log.DurationMs}мс)\nBody: {log.RequestBody}";
                
                var currentText = ServerLogsTextBox.Text ?? "";
                ServerLogsTextBox.Text = currentText + $"{title}\n{request}\n{details}\n----------------------------------------\n";
            }
        });
    }

    private void StartServerBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (StartServerBtn.Content?.ToString() == "Запустить сервер")
        {
            if (int.TryParse(PortTextBox.Text, out int port))
            {
                try
                {
                    _serverService.Start(port);
                    
                    StartServerBtn.Content = "Остановить сервер";
                    StartServerBtn.Background = Brush.Parse("#EF4444"); 
                    ServerStatusTextBlock.Text = "Работает";
                    ServerStatusTextBlock.Foreground = Brush.Parse("#10B981");
                    
                    _statsTimer.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка запуска: " + ex.Message);
                }
            }
        }
        else
        {
            _serverService.Stop();
            
            StartServerBtn.Content = "Запустить сервер";
            StartServerBtn.Background = Brush.Parse("#3B82F6"); 
            ServerStatusTextBlock.Text = "Остановлен";
            ServerStatusTextBlock.Foreground = Brush.Parse("#EF4444");
            
            _statsTimer.Stop();
            UpdateStats(null, EventArgs.Empty);
        }
    }

    private async void SendRequestBtn_Click(object? sender, RoutedEventArgs e)
    {
        SendRequestBtn.IsEnabled = false;
        ClientResponseTextBox.Text = "Ожидание ответа от сервера...";
        
        try
        {
            var methodItem = MethodComboBox.SelectedItem as ComboBoxItem;
            var method = methodItem?.Content?.ToString() ?? "GET";
            
            var response = await _clientService.SendRequestAsync(UrlTextBox.Text ?? "", method, RequestBodyTextBox.Text ?? "");
            ClientResponseTextBox.Text = response;
        }
        catch (Exception ex)
        {
            ClientResponseTextBox.Text = $"Ошибка при выполнении запроса:\n{ex.Message}";
        }
        finally
        {
            SendRequestBtn.IsEnabled = true;
        }
    }

    private void UpdateStats(object? sender, EventArgs e)
    {
        var status = _serverService.GetStatus();
        StatsTextBlock.Text = $"Время работы: {status.Uptime:hh\\:mm\\:ss}   |   " +
                              $"Всего запросов: {status.TotalRequests}   |   " +
                              $"GET: {status.GetRequests}   |   " +
                              $"POST: {status.PostRequests}   |   " +
                              $"Среднее время: {status.AverageProcessingTimeMs:F2} мс";
    }

    private void FilterComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e) => ApplyFilter();
    private void FilterKeywordTextBox_TextChanged(object? sender, TextChangedEventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        if (FilterMethodComboBox == null || FilterKeywordTextBox == null || FilterStatusTextBox == null) 
            return;
        
        var methodFilter = (FilterMethodComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
        var keyword = FilterKeywordTextBox.Text?.ToLower() ?? "";
        var statusKeyword = FilterStatusTextBox.Text?.ToLower() ?? "";
        
        FilteredLogs.Clear();
        
        foreach (var log in _allLogs)
        {
            bool methodMatch = methodFilter == "Все" || log.Method == methodFilter;
            bool keywordMatch = string.IsNullOrEmpty(keyword) || log.Url.ToLower().Contains(keyword);
            bool statusMatch = string.IsNullOrEmpty(statusKeyword) || log.ResponseStatus.ToLower().Contains(statusKeyword);
                                
            if (methodMatch && keywordMatch && statusMatch)
            {
                FilteredLogs.Add(log);
            }
        }
    }
}
