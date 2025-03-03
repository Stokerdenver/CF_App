namespace Dis_1;

using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;
using Dis_1.Model;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json;
using System.Threading;

public partial class GPS_test : ContentPage
{
    public string UserName { get; set; }
    public UserC user;
    // ���������� ������ ��������� HttpClient ��� ����� ������
    private static readonly HttpClient httpClient = new HttpClient();

    public GPS_test()
    {
        InitializeComponent();
        UserName = Preferences.Get("UserLogin", string.Empty);
        // ������������� ������������ � ������� ������
        _ = InitializeUserDataAsync();
    }

    private bool _isRunning; // ����� ������������ ��� ������ UI, ���� �����
    public string data_GPS;
    public double longitudeToDb;
    public double latitudeToDb;
    public int speedToDb;
    public bool isleader = true;

    // ����������� ������������� ������ ������������
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

    // ����� ��� ������� ���������� � �������������� CancellationToken
    async Task StartLocationUpdatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            // ������ ���������� �� ������ � ��������������
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                gpsLabel.Text = "Location permission denied.";
                return;
            }

            // ������ ������������� ���������� ������ (��� � ���)
            _ = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await SendWeatherAsync();
                    }
                    catch (Exception ex)
                    {
                        // ����� ����� ���������� ������, ���� �����
                    }
                    await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
                }
            }, cancellationToken);

            // �������� ���� ���������� ��������������
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // ��������� �������� �������������� � ����������� ��������� (10 ������)
                    var location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(1)));
                    if (location != null)
                    {
                        // ����������� �������� �� �/� � ��/�
                        double speedMps = location.Speed.HasValue ? location.Speed.Value : 0;
                        double speedKmh = speedMps * 3.6;

                        gpsLabel.Text = $"������� ����������: {location.Latitude}, {location.Longitude}";
                        speedLabel.Text = $"��������: {Math.Round(speedKmh, 1)} ��/�";
                        data_GPS = $"{speedKmh} {location.Latitude} {location.Longitude}";

                        longitudeToDb = location.Longitude;
                        latitudeToDb = location.Latitude;
                        speedToDb = Convert.ToInt32(speedKmh);

                        // �������� ������ �� ������ (fire-and-forget, ����� �� ����������� ����)
                        _ = SendDataToServerAsync();
                    }
                }
                catch (Exception ex)
                {
                    gpsLabel.Text = $"Error: {ex.Message}";
                }
                // �������� � 1 ������� ����� ������������
                await Task.Delay(1000, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            gpsLabel.Text = $"Error in location updates: {ex.Message}";
        }
    }

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
            gpsLabel.Text = $"Failed to send data: {ex.Message}";
        }
    }

    // �������� ������ � ������ (��� �������� ������ ������)
    public async Task SendWeatherAsync()
    {
        try
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
        catch (Exception ex)
        {
            gpsLabel.Text = $"Failed to send weather data: {ex.Message}";
        }
    }

    CancellationTokenSource cts;

    private async void Start_SendingData(object sender, EventArgs e)
    {
        cts = new CancellationTokenSource();
        await StartLocationUpdatesAsync(cts.Token);
    }

    private void Stop_SendingData(object sender, EventArgs e)
    {
        cts?.Cancel();
        gpsLabel.Text = "";
        testLabel.Text = "";
        speedLabel.Text = "";
    }
}
