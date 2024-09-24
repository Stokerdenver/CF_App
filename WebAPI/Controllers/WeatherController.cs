namespace WebAPI.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Threading.Tasks;
    using WebAPI.Data;
    using WebAPI.Models;

    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WeatherController(AppDbContext context)
        {
            _context = context;
        }
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] WeatherData weatherData)
        {
            if (weatherData == null)
                return BadRequest();

            // Добавляем текущее время
            weatherData.timestamp = DateTime.UtcNow;

            // Добавляем данные в базу данных
            _context.weather_data.Add(weatherData);
            await _context.SaveChangesAsync(); // Сохраняем изменения в базе данных

            return Ok(weatherData); // Возвращаем ответ с добавленными данными
        }
    }
}
