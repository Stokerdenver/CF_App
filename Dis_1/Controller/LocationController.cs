using Dis_1.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Dis_1.Controller
{
    public class LocationController
    {
        private readonly LocationService _locationService;

        public LocationController(LocationService locationService)
        {
            _locationService = locationService;
        }

        public async Task UpdateLocationAsync()
        {
            var locationData = await _locationService.GetCurrentLocationAsync();
            if (locationData != null)
            {
                await _locationService.SendDataToServerAsync(locationData);
            }
        }
    }
}
