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

    // HttpClient для отправки данных, чтобы не создавать на каждый запрос
    private static readonly HttpClient httpClient = new HttpClient();

    public GPS_test()
    {
        InitializeComponent();
        UserName = Preferences.Get("UserLogin", string.Empty);
        _ = InitializeUserDataAsync();
    }

    // Поля для хранения актуальных значений
    public double longitudeToDb;
    public double latitudeToDb;
    public int speedToDb;
    public double bearingToDb; // Поле для направления движения
    public bool isleader = true;

    // Флаг, контролирующий, подписаны ли мы уже на события (чтобы не подписываться повторно)
    private bool _isListening;

    // Для управления циклом отправки данных о погоде
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

    // 2. Запуск прослушивания геолокации
    private async Task StartListeningAsync()
    {
        try
        {
            // Проверяем, не запущено ли уже прослушивание
            if (_isListening)
                return;

            // Запрашиваем разрешение на использование геолокации (MAUI Permissions)
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                gpsLabel.Text = "Location permission denied.";
                return;
            }

            // Получаем текущий экземпляр геолокатора
            var locator = CrossGeolocator.Current;

            // Подписываемся на событие изменения положения
            locator.PositionChanged += OnPositionChanged;
            locator.PositionError += OnPositionError;

            // Настраиваем прослушивание с включенным параметром heading
            await locator.StartListeningAsync(
                TimeSpan.FromSeconds(1), // интервал
                0,                       // дистанция (м)
                includeHeading: true,    // включаем получение направления 
                                         // Heading возвращается в градусах от 0 до 360, где 0 - север
                new ListenerSettings
                {
                    // Можно настроить приоритет и другие параметры
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

    // 3. Обработчик события PositionChanged с получением heading
    private void OnPositionChanged(object sender, PositionEventArgs e)
    {
        try
        {
            var position = e.Position;
            if (position == null)
                return;

            // Получаем координаты
            latitudeToDb = position.Latitude;
            longitudeToDb = position.Longitude;

            // Получаем направление движения прямо из GPS
            // Heading возвращается в градусах от 0 до 360, где 0 - север
            bearingToDb = position.Heading;

            // Скорость (м/с), переводим в км/ч
            double speedMps = position.Speed;
            double speedKmh = speedMps * 3.6;
            speedToDb = Convert.ToInt32(speedKmh);

            // Обновляем UI на главном потоке
            MainThread.BeginInvokeOnMainThread(() =>
            {
                gpsLabel.Text = $"Текущие координаты: {position.Latitude}, {position.Longitude}";
                speedLabel.Text = $"Скорость: {Math.Round(speedKmh, 3)} км/ч";
                // Можно добавить новый label для отображения направления
                if (headingLabel != null) // Если добавили такой label
                {
                    headingLabel.Text = $"Направление: {Math.Round(bearingToDb, 1)}°";
                }
            });

            // Отправляем данные на сервер (fire-and-forget, чтобы не блокировать событие)
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

    // 4. Обработчик ошибок позиционирования
    private void OnPositionError(object sender, PositionErrorEventArgs e)
    {
        // Обработка ошибок геолокации
        var error = e.Error;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            gpsLabel.Text = $"Geolocation error: {error}";
        });
    }

    // 5. Метод отправки данных о погоде раз в час 
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
                // логируем при необходимости
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    gpsLabel.Text = $"Failed to send weather data: {ex.Message}";
                });
            }
            // Ждём 1 час, либо отмену
            await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
        }
    }

    // 6. Запуск и остановка «прослушки»
    private async void Start_SendingData(object sender, EventArgs e)
    {
        // Запускаем подписку на геолокацию
        await StartListeningAsync();

        // Запускаем параллельно (раз в час) обновление погоды
        ctsWeather = new CancellationTokenSource();
        _ = Task.Run(() => StartWeatherUpdates(ctsWeather.Token));
    }

    private async void Stop_SendingData(object sender, EventArgs e)
    {
        // Останавливаем погоду
        ctsWeather?.Cancel();

        // Останавливаем подписку на геолокацию
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

    // 7. Отправка данных на сервер (добавили bearing)
    public async Task SendDataToServerAsync()
    {
        try
        {
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
                bearing = bearingToDb  // Добавляем направление движения
            };

            var jsonData = System.Text.Json.JsonSerializer.Serialize(data);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
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

    // 8. Отправка данных о погоде (добавили bearing)
    public async Task SendWeatherAsync()
    {
        var wdata = new
        {
            longitude = longitudeToDb,
            latitude = latitudeToDb,
            username = UserName,
            isleader = isleader,
            bearing = bearingToDb  // Добавляем направление
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