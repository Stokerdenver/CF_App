namespace Dis_1;
using Dis_1.Model;
using System.Text;
using System.Text.Json;
using System.Threading;

public partial class GPS_test : ContentPage
{
    private bool _isRunning; // ���� ��� �������� ����������
    public GPS_test()
	{
		InitializeComponent();
       
    }

    // public LocationData test1;

    public string data_GPS;
    public double longitudeToDb;
    public double latitudeToDb;
    public int speedToDb;
    public string username = Preferences.Get("UserLogin", string.Empty);
    public bool isleader = true;

    async void StartLocationUpdates()
    {
        try
        {
            // ������ ���������� �� ������ � ��������������
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                // ���� ���������� �� �������������, ������� ���������
                gpsLabel.Text = "Location permission denied.";
                return;
            }
            
            // ������ ������������ �������� ���������� � �������� ������
            await UpdateAndSendDataAsync();
        }
        catch (Exception ex)
        {
            gpsLabel.Text = $"Error: {ex.Message}";
        }
    }

    async Task UpdateAndSendDataAsync()
    {
        while (_isRunning)
        {
            try
            {
                // ��������� �������� ��������������
                var location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(1)));

                if (location != null)
                {
                    // ���� �������� ����� 0, ������� "0"
                    double speed = location.Speed.HasValue ? location.Speed.Value : 0;
                    gpsLabel.Text = $"GPS Coordinates: {location.Latitude}, {location.Longitude}";
                    speedLabel.Text = $"Speed: {speed} m/s";
                    data_GPS = Convert.ToString(speed) + " " + Convert.ToString(location.Latitude) + " " + Convert.ToString(location.Longitude);

                   // ��������� ��, ��� ���������� � ��
                   testLabel.Text = data_GPS;

                    longitudeToDb = Convert.ToDouble(location.Longitude);
                    latitudeToDb = Convert.ToDouble(location.Latitude);
                    speedToDb = Convert.ToInt32(location.Speed);


                    // �������� ������ �� ������
                    await SendDataToServerAsync();
                }
            }
            catch (Exception ex)
            {
                gpsLabel.Text = $"Error: {ex.Message}";
            }



            // �������� � 1 ������� ����� ��������� �����������
            await Task.Delay(1000);
        }
    }

    public async Task SendDataToServerAsync()
    {
        try
        {
            // ���� �� ������������ �������� ������ ������ - �� �������
            // ����� ������ ��� ���� � � ������� ����� ������� ������ ����
            var data = new
            {
                longitude = longitudeToDb,
                latitude = latitudeToDb,
                speed = speedToDb,
                username = username,
                isleader = isleader
               
            };
            var client = new HttpClient();
            var jsonData = JsonSerializer.Serialize(data);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            await client.PostAsync("http://10.0.2.2:5000/api/MainData", content);
        }
        catch (Exception ex)
        {
            gpsLabel.Text = $"Failed to send data: {ex.Message}";
        }
        await SendWeatherAsync();
    }

    public async Task SendWeatherAsync()
    {
        var wdata = new
        {
            longitude = longitudeToDb,
            latitude = latitudeToDb,
            username = username,
            isleader = isleader

        };
        var client = new HttpClient();
        var jsonData = JsonSerializer.Serialize(wdata);
        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
        await client.PostAsync("http://10.0.2.2:5000/api/Weather", content);
        await Task.Delay(2000);
    }





    private async void Start_SendingData(object sender, EventArgs e)
    {

        _isRunning = true;
        StartLocationUpdates();
    }

    private async void Stop_SendingData(object sender, EventArgs e)
    {
        _isRunning = false;
    }
}