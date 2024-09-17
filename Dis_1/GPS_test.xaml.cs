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
    public double longitudeToDb;
    public double latitudeToDb;
    public int speedToDb;

    async void StartLocationUpdates()
    {
        try
        {
            // Запрос разрешений на доступ к местоположению
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                // Если разрешения не предоставлены, выводим сообщение
                gpsLabel.Text = "Location permission denied.";
                return;
            }

            // Запуск асинхронного процесса обновления и отправки данных
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
                // Получение текущего местоположения
                var location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(1)));

                if (location != null)
                {
                    // Если скорость равна 0, выводим "0"
                    double speed = location.Speed.HasValue ? location.Speed.Value : 0;
                    gpsLabel.Text = $"GPS Coordinates: {location.Latitude}, {location.Longitude}";
                    speedLabel.Text = $"Speed: {speed} m/s";
                    data_GPS = Convert.ToString(speed) + " " + Convert.ToString(location.Latitude) + " " + Convert.ToString(location.Longitude);

                   // проверяем то, что отправляем в бд
                   testLabel.Text = data_GPS;

                    longitudeToDb = Convert.ToDouble(location.Longitude);
                    latitudeToDb = Convert.ToDouble(location.Latitude);
                    speedToDb = (int)Convert.ToInt64(location.Speed);


                    // Отправка данных на сервер
                    await SendDataToServerAsync();
                }
            }
            catch (Exception ex)
            {
                gpsLabel.Text = $"Error: {ex.Message}";
            }

            // Задержка в 1 секунду перед следующим обновлением
            await Task.Delay(1000);
        }
    }

    public async Task SendDataToServerAsync()
    {
        try
        {
            // если на сериализацию отдавать просто стринг - не рабоает
            // нужен объект вар типа и в атрибут этого объекта класть инфу
            var data = new
            {
                longitude = longitudeToDb,
                latitude = latitudeToDb,
                speed = speedToDb
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
    }

    private void Start_SendingData(object sender, EventArgs e)
    {
        // Запуск обновления данных
        StartLocationUpdates();
    }
}