namespace Dis_1;
using Dis_1.Model;
using System.Text;
using System.Text.Json;

public partial class GPS_test : ContentPage
{
	public GPS_test()
	{
		InitializeComponent();
    }

    // public LocationData test1;

    public string data_GPS;
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
        while (true)
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
                    testLabel.Text = data_GPS;


                    // ���������� ������ � ������� test1
                    /* test1.Latitude = location.Latitude;
                     test1.Longitude = location.Longitude;
                     test1.Speed = speed;
                    */


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
                Data = data_GPS
            };
            var client = new HttpClient();
            var jsonData = JsonSerializer.Serialize(data);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            await client.PostAsync("http://10.0.2.2:5000/api/SensorData", content);
        }
        catch (Exception ex)
        {
            gpsLabel.Text = $"Failed to send data: {ex.Message}";
        }
    }

    private void Start_SendingData(object sender, EventArgs e)
    {
        // ������ ���������� ������
        StartLocationUpdates();
    }
}