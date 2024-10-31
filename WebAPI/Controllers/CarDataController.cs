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
            if (car == null)
                return BadRequest();
            

            // Добавляем авто в базу данных
            _context.car.Add(car);
            await _context.SaveChangesAsync();

            return Ok("User registered successfully.");
        }
    }
}
