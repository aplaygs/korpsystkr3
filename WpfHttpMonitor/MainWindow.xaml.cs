using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using WpfHttpMonitor.Models;
using WpfHttpMonitor.Services;
using System.Windows.Media;
using System.Windows.Controls;

namespace WpfHttpMonitor
{
    public partial class MainWindow : Window
    {
        private readonly HttpServerService _serverService = new HttpServerService();
        private readonly HttpClientService _clientService = new HttpClientService();
        
        // Коллекции для привязки к DataGrid
        private readonly ObservableCollection<LogEntry> _allLogs = new ObservableCollection<LogEntry>();
        private readonly ObservableCollection<LogEntry> _filteredLogs = new ObservableCollection<LogEntry>();
        
        private DispatcherTimer _statsTimer;

        public MainWindow()
        {
            InitializeComponent();
            
            LogsDataGrid.ItemsSource = _filteredLogs;
            
            // Подписка на события логгирования
            _serverService.OnLogReceived += OnLogReceived;
            _clientService.OnLogReceived += OnLogReceived;
            
            // Таймер для обновления статистики раз в секунду
            _statsTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _statsTimer.Tick += UpdateStats;
        }

        private void OnLogReceived(LogEntry log)
        {
            Dispatcher.Invoke(() =>
            {
                // Добавляем в начало списка
                _allLogs.Insert(0, log);
                ApplyFilter(); // Применяем фильтр для отображения в таблице
                
                // Вывод текстовых логов для сервера
                if (log.IsIncoming)
                {
                    var title = $"👉 [{log.Timestamp:HH:mm:ss}] {log.Method} {log.Url}";
                    var request = $"Заголовки: {log.Headers}";
                    var details = $"<- Ответ: Статус {log.ResponseStatus} ({log.DurationMs}мс)\nBody: {log.RequestBody}";
                    
                    ServerLogsTextBox.AppendText($"{title}\n{request}\n{details}\n----------------------------------------\n");
                    ServerLogsTextBox.ScrollToEnd();
                }
            });
        }

        private void StartServerBtn_Click(object sender, RoutedEventArgs e)
        {
            if (StartServerBtn.Content.ToString() == "Запустить сервер")
            {
                if (int.TryParse(PortTextBox.Text, out int port))
                {
                    try
                    {
                        _serverService.Start(port);
                        
                        // Обновление UI
                        StartServerBtn.Content = "Остановить сервер";
                        StartServerBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")); // Red
                        ServerStatusTextBlock.Text = "Работает";
                        ServerStatusTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")); // Green
                        
                        _statsTimer.Start();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Пожалуйста, введите корректный номер порта!");
                }
            }
            else
            {
                _serverService.Stop();
                
                // Обновление UI
                StartServerBtn.Content = "Запустить сервер";
                StartServerBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")); // Blue
                ServerStatusTextBlock.Text = "Остановлен";
                ServerStatusTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")); // Red
                
                _statsTimer.Stop();
                UpdateStats(null, EventArgs.Empty);
            }
        }

        private async void SendRequestBtn_Click(object sender, RoutedEventArgs e)
        {
            SendRequestBtn.IsEnabled = false;
            ClientResponseTextBox.Text = "Ожидание ответа от сервера...";
            
            try
            {
                var methodItem = MethodComboBox.SelectedItem as ComboBoxItem;
                var method = methodItem?.Content.ToString() ?? "GET";
                
                var response = await _clientService.SendRequestAsync(UrlTextBox.Text, method, RequestBodyTextBox.Text);
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

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void FilterKeywordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (FilterMethodComboBox == null || FilterKeywordTextBox == null || FilterStatusTextBox == null || _allLogs == null) 
                return;
            
            var methodFilter = (FilterMethodComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var keyword = FilterKeywordTextBox.Text.ToLower();
            var statusKeyword = FilterStatusTextBox.Text.ToLower();
            
            _filteredLogs.Clear();
            
            foreach (var log in _allLogs)
            {
                bool methodMatch = methodFilter == "Все" || log.Method == methodFilter;
                bool keywordMatch = string.IsNullOrEmpty(keyword) || log.Url.ToLower().Contains(keyword);
                bool statusMatch = string.IsNullOrEmpty(statusKeyword) || log.ResponseStatus.ToLower().Contains(statusKeyword);
                                    
                if (methodMatch && keywordMatch && statusMatch)
                {
                    _filteredLogs.Add(log);
                }
            }
        }
    }
}
