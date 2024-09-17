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
    public class MainDataController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MainDataController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] MainData mainData)
        {
            if (mainData == null)
                return BadRequest();

            // Добавляем текущее время
            mainData.timestamp = DateTime.UtcNow;

            // Добавляем данные в базу данных
            _context.main_data.Add(mainData);
            await _context.SaveChangesAsync(); // Сохраняем изменения в базе данных

            return Ok(mainData); // Возвращаем ответ с добавленными данными
        }
    }
}
