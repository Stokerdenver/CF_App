using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Dis_1.Model
{
    public class LocationService
    {
        public async Task<LocationData> GetCurrentLocationAsync()
        {
            var location = await Geolocation.GetLocationAsync(new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Best,
                Timeout = TimeSpan.FromSeconds(1)
            });

            if (location != null)
            {
                return new LocationData
                {
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    Speed = location.Speed.HasValue ? location.Speed.Value : 0
                };
            }
            if (location == null)
            {
                return new LocationData
                {
                    Latitude = 0,
                    Longitude = 0,
                    Speed = 0
                };
            }
            return null;
        }

        public async Task SendDataToServerAsync(LocationData data)
        {
            var client = new HttpClient();
            var jsonData = JsonSerializer.Serialize(data);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            await client.PostAsync("http://10.0.2.2:5000/api/SensorData", content);
        }
    }
}
