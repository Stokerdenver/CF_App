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

    // HttpClient ��� �������� ������, ����� �� ��������� �� ������ ������
    private static readonly HttpClient httpClient = new HttpClient();

    public GPS_test()
    {
        InitializeComponent();
        UserName = Preferences.Get("UserLogin", string.Empty);
        _ = InitializeUserDataAsync();
    }

    // ���� ��� �������� ���������� ��������
    public double longitudeToDb;
    public double latitudeToDb;
    public int speedToDb;
    public bool isleader = true;

    // ����, ��������������, ��������� �� �� ��� �� ������� (����� �� ������������� ��������)
    private bool _isListening;

    // ��� ���������� ������ �������� ������ � ������
    private CancellationTokenSource ctsWeather;

    // 1. �������� ������ ������������ (��� � ������)
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

    // 2. ������ ������������� ����������
    private async Task StartListeningAsync()
    {
        try
        {
            // ���������, �� �������� �� ��� �������������
            if (_isListening)
                return;

            // ����������� ���������� �� ������������� ���������� (MAUI Permissions)
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                gpsLabel.Text = "Location permission denied.";
                return;
            }

            // �������� ������� ��������� �����������
            var locator = CrossGeolocator.Current;

            // ������������� �� ������� ��������� ���������
            locator.PositionChanged += OnPositionChanged;
            locator.PositionError += OnPositionError;

            // ����������� �������������:
            //  - �������� ����������: 1 ���
            //  - ����������� ���������� (� ������) ��� ��������: 0
            await locator.StartListeningAsync(
                TimeSpan.FromSeconds(1), // ��������
                0,                       // ��������� (�)
                includeHeading: false,
                new ListenerSettings
                {
                    // ����� ��������� ��������� � ������ ���������
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

    // 3. ���������� ������� PositionChanged
    private void OnPositionChanged(object sender, PositionEventArgs e)
    {
        try
        {
            var position = e.Position;
            if (position == null)
                return;

            // �������� ����������
            latitudeToDb = position.Latitude;
            longitudeToDb = position.Longitude;


            // �������� (�/�), ��������� � ��/� (�������� �� 3.6)\

            double speedMps = position.Speed;

            // double speedMps = position.Speed;
            double speedKmh = speedMps * 3.6;
            speedToDb = Convert.ToInt32(speedKmh);

            // ��������� UI �� ������� ������
            MainThread.BeginInvokeOnMainThread(() =>
            {
                gpsLabel.Text = $"������� ����������: {position.Latitude}, {position.Longitude}";
                speedLabel.Text = $"��������: {Math.Round(speedKmh, 3)} ��/�";
            });

            // ���������� ������ �� ������ (fire-and-forget, ����� �� ����������� �������)
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

    // 4. ���������� ������ ����������������
    private void OnPositionError(object sender, PositionErrorEventArgs e)
    {
        // ��������� ������ ����������
        var error = e.Error;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            gpsLabel.Text = $"Geolocation error: {error}";
        });
    }

    // 5. ����� �������� ������ � ������ ��� � ��� (�����������, ��� � ��� ����)
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
                // �������� ��� �������������
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    gpsLabel.Text = $"Failed to send weather data: {ex.Message}";
                });
            }
            // ��� 1 ���, ���� ������
            await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
        }
    }

    // 6. ������ � ��������� ����������
    private async void Start_SendingData(object sender, EventArgs e)
    {
        // ��������� �������� �� ����������
        await StartListeningAsync();

        // ��������� ����������� (��� � ���) ���������� ������
        ctsWeather = new CancellationTokenSource();
        _ = Task.Run(() => StartWeatherUpdates(ctsWeather.Token));
    }

    private async void Stop_SendingData(object sender, EventArgs e)
    {
        // ������������� ������
        ctsWeather?.Cancel();

        // ������������� �������� �� ����������
        if (_isListening)
        {
            var locator = CrossGeolocator.Current;
            locator.PositionChanged -= OnPositionChanged;
            locator.PositionError -= OnPositionError;

            await locator.StopListeningAsync();
            _isListening = false;
        }

        gpsLabel.Text = "";
        testLabel.Text = "";
        speedLabel.Text = "";
    }

    // 7. �������� ������ �� ������ (��������� ������� �� ����)
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
                current_car = current_car
            };

            var jsonData = System.Text.Json.JsonSerializer.Serialize(data);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            await httpClient.PostAsync("http://45.84.225.138:80/api/MainData", content);
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                gpsLabel.Text = $"Failed to send data: {ex.Message}";
            });
        }
    }

    // 8. �������� ������ � ������ (������������� ��� � ��� � StartWeatherUpdates)
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
        await httpClient.PostAsync("http://45.84.225.138:80/api/Weather", content);
    }
}
