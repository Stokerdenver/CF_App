namespace Dis_1;

using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;
using Dis_1.Model;
using Newtonsoft.Json;
using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
using System.Text;
using System.Text.Json;
using System.Threading;

public partial class GPS_test : ContentPage
{
    public string UserName { get; set; }
    public UserC user;

    // HttpClient чтобы не создавать его постоянно в коде
    private static readonly HttpClient httpClient = new HttpClient();

    public GPS_test()
    {
        InitializeComponent();
        UserName = Preferences.Get("UserLogin", string.Empty);
        _ = InitializeUserDataAsync();
    }

    // основные рабочие переменные
    public double longitudeToDb;
    public double latitudeToDb;
    public int speedToDb;
    public double bearingToDb; 
    public bool isleader = true;

    // булева переменная для проверки, запущена ли уже прослушка GPS
    private bool _isListening;
    private DateTime _lastDataSendTime = DateTime.MinValue;
    private const int SEND_INTERVAL_MS = 1000; // 1 секунда в миллисекундах

    // токен для работы с отправкой погоды
    private CancellationTokenSource ctsWeather;

    // 1. Получаем данные пользователя
    private async Task InitializeUserDataAsync()
    {
        user = await GetUserDataFromServer(UserName);
    }

    public async Task<UserC> GetUserDataFromServer(string userName)
    {
        try
        {
            var response = await httpClient.GetStringAsync($"http://45.84.225.138:80/api/User/{userName}");
            var user = JsonConvert.DeserializeObject<UserC>(response);
            return user;
        }
        catch (Exception ex)
        {
            gpsLabel.Text = $"Error getting user data: {ex.Message}";
            return null;
        }
    }

    // 2. Начинаем прослушку GPS
    private async Task StartListeningAsync()
    {
        try
        {
            
            if (_isListening)
                return;

            // Запрашиваем разрешение на использование GPS (MAUI Permissions)
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                gpsLabel.Text = "Location permission denied.";
                return;
            }

            // Создание экземпляра геолокатора
            var locator = CrossGeolocator.Current;

            // Подписка на события изменения координат и ошибок
            locator.PositionChanged += OnPositionChanged;
            locator.PositionError += OnPositionError;

            // Запуск прослушивания координат
            await locator.StartListeningAsync(
                TimeSpan.FromSeconds(1), // Интервал обновления (1 секунда)
                0,                       // Дистанция обновления (0 метров)
                includeHeading: true,    // Включаем направление движения
                                         // Heading в градусах от 0 до 360
                new ListenerSettings
                {
                    
                    ActivityType = ActivityType.AutomotiveNavigation,
                    AllowBackgroundUpdates = true,
                    ListenForSignificantChanges = false,
                    PauseLocationUpdatesAutomatically = false
                }
            );

            _isListening = true;
        }
        catch (Exception ex)
        {
            gpsLabel.Text = $"Error: {ex.Message}";
        }
    }

    // 3. Обработчик изменения позиции
    private void OnPositionChanged(object sender, PositionEventArgs e)
    {
        try
        {
            var position = e.Position;
            if (position == null)
                return;

            // Обновление координат
            latitudeToDb = position.Latitude;
            longitudeToDb = position.Longitude;
            bearingToDb = position.Heading;

            // Получение скорости в км/ч
            double speedMps = position.Speed;
            double speedKmh = speedMps * 3.6;
            speedToDb = Convert.ToInt32(speedKmh);

            // Обновление интерфейса
            MainThread.BeginInvokeOnMainThread(() =>
            {
                gpsLabel.Text = $"Текущие координаты: {position.Latitude}, {position.Longitude}";
                speedLabel.Text = $"Скорость: {Math.Round(speedKmh, 3)} км/ч";
                
                if (headingLabel != null) 
                {
                    headingLabel.Text = $"Направление: {Math.Round(bearingToDb, 1)}°";
                }
            });

            // Отправка данных на сервер
            _ = SendDataToServerAsync();
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                gpsLabel.Text = $"Error in OnPositionChanged: {ex.Message}";
            });
        }
    }

    // 4. Обработчик ошибок геолокации
    private void OnPositionError(object sender, PositionErrorEventArgs e)
    {
        
        var error = e.Error;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            gpsLabel.Text = $"Geolocation error: {error}";
        });
    }

    // 5. Циклическая отправка погодных данных (раз в час)
    private async Task StartWeatherUpdates(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await SendWeatherAsync();
            }
            catch (Exception ex)
            {
                
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    gpsLabel.Text = $"Failed to send weather data: {ex.Message}";
                });
            }
            
            await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
        }
    }

    // 6. Запуск передачи данных
    private async void Start_SendingData(object sender, EventArgs e)
    {
        
        await StartListeningAsync();

        
        ctsWeather = new CancellationTokenSource();
        _ = Task.Run(() => StartWeatherUpdates(ctsWeather.Token));
    }

    // 7. Остановка передачи данных
    private async void Stop_SendingData(object sender, EventArgs e)
    {
        ctsWeather?.Cancel();

        if (_isListening)
        {
            var locator = CrossGeolocator.Current;
            locator.PositionChanged -= OnPositionChanged;
            locator.PositionError -= OnPositionError;

            await locator.StopListeningAsync();
            _isListening = false;
        }

        gpsLabel.Text = "";
        speedLabel.Text = "";
        headingLabel.Text = "";
        leaderStatusLabel.Text = "";


    }

    // 8. Отправка данных на сервер
    public async Task SendDataToServerAsync()
    {
        try
        {
            // Проверяем, прошла ли 1 секунда с последней отправки
            var now = DateTime.UtcNow;
            if ((now - _lastDataSendTime).TotalMilliseconds < SEND_INTERVAL_MS)
            {
                return;
            }
            _lastDataSendTime = now;

            string current_car = string.Empty;
            if (user != null && user.Cars != null && user.Cars.Any())
            {
                var defaultCar = user.Cars.FirstOrDefault();
                current_car = defaultCar?.model;
            }
            if (CurrentCar.SelectedCar != null)
            {
                current_car = CurrentCar.SelectedCar.model;
            }

            var data = new
            {
                longitude = longitudeToDb,
                latitude = latitudeToDb,
                speed = speedToDb,
                username = UserName,
                isleader = isleader,
                current_car = current_car,
                bearing = bearingToDb  // Передаем направление движения
            };

            var jsonData = System.Text.Json.JsonSerializer.Serialize(data);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            // Отправляем данные на сервер
            var response = await httpClient.PostAsync($"{AppSettings.ServerUrl}/api/MainData", content);

            if (response.IsSuccessStatusCode)
            {
                // Получаем ответ с обновленными данными (включая поле isleader)
                  var result = await response.Content.ReadAsStringAsync();
                  var returnedData = System.Text.Json.JsonSerializer.Deserialize<MainData>(result);

                  // Обновляем UI: показываем статус "Лидер" или "Ведомый"
                  MainThread.BeginInvokeOnMainThread(() =>
                  {
                      // Предполагаем, что у вас есть label leaderStatusLabel на странице
                      leaderStatusLabel.Text = returnedData.isleader ? "Вы Лидер" : "Вы Ведомый";
                  });
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    gpsLabel.Text = "Ошибка при отправке данных";
                });
            }
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                gpsLabel.Text = $"Failed to send data: {ex.Message}";
            });
        }
    }

    // 9. Отправка погодных данных
    public async Task SendWeatherAsync()
    {
        var wdata = new
        {
            longitude = longitudeToDb,
            latitude = latitudeToDb,
            username = UserName,
            isleader = isleader
             
        };
        var jsonData = System.Text.Json.JsonSerializer.Serialize(wdata);
        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
        await httpClient.PostAsync($"{AppSettings.ServerUrl}/api/Weather", content);
    }

    private void Change_Server(object sender, EventArgs e)
    {
        //AppSettings.ServerUrl = "http://45.84.225.138:81";
    }
}