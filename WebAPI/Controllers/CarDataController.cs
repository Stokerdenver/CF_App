namespace WebAPI.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Threading.Tasks;
    using WebAPI.Data;
    using WebAPI.Models;
    using WebAPI.Services;
    [ApiController]
    [Route("api/[controller]")]
    public class CarDataController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CarDataController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> AddCar([FromBody] Car car)
        {
            if (car == null || car.user_id == 0)
                return BadRequest();

            //car.User = null;
            // Добавляем авто в базу данных
            var userExists = await _context.user.AnyAsync(u => u.id == car.user_id);
            if (!userExists)
                return NotFound("Пользователь не найден.");

            _context.car.Add(car);
            await _context.SaveChangesAsync();

            return Ok("Car added successfully.");
        }
    }
}
