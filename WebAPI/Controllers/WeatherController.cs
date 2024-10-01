namespace WebAPI.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Threading.Tasks;
    using WebAPI.Data;
    using WebAPI.Models;

    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly AppDbContext _context;
     //   private readonly HttpClient _httpClient;

        public WeatherController(HttpClient httpClient, AppDbContext context)
        {
           // _httpClient = httpClient;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] WeatherData weatherData)
        {
            if (weatherData == null)
                return BadRequest();

            // Добавляем текущее время
            weatherData.timestamp = DateTime.UtcNow;



            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://weatherapi-com.p.rapidapi.com/current.json?q={weatherData.latitude}%2C{weatherData.longitude}"),
                Headers =
            {
                { "x-rapidapi-key", "e710e7dafdmsh85754b3dbe7d286p1afe9ejsnaf5dd92756f8" },
                { "x-rapidapi-host", "weatherapi-com.p.rapidapi.com" },
            },
            };

            var response = await client.SendAsync(request);
            
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();

            Console.WriteLine(body);

            // Парсим JSON-строку
            JObject jsonData = JObject.Parse(body);

            weatherData.temp_c = (double)jsonData["current"]["temp_c"];
            weatherData.wind_kph = (double)jsonData["current"]["wind_kph"];
            weatherData.precip_mm = (double)jsonData["current"]["precip_mm"];
            weatherData.is_day = (int)jsonData["current"]["is_day"];


            // Добавляем данные в базу данных
            _context.weather_data.Add(weatherData);
            await _context.SaveChangesAsync(); // Сохраняем изменения в базе данных

            return Ok(weatherData); // Возвращаем ответ с добавленными данными
        }
    }
}
