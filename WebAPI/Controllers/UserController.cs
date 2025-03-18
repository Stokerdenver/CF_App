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
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] User user)
        {

            // проверяем только существует ли пользователь с таким именем и всё
            // ничего не хешируем

            // Проверьте, существует ли пользователь с таким именем
            var existingUser = _context.user.FirstOrDefault(u => u.name == user.name);
            if (existingUser != null)
            {
                return Conflict("Пользователь с таким именем уже существует.");
            }
            // Добавляем пользователя в базу данных
            _context.user.Add(user);
            await _context.SaveChangesAsync();

            return Ok(user.id);
        }

        [HttpGet("{userName}")]
        public async Task<IActionResult> GetUser(string userName)
        {
            // Получение данных пользователя из базы данных
            var user = await _context.user
                .Include(u => u.Cars) // Включение связанных автомобилей
                .FirstOrDefaultAsync(u => u.name == userName);

            if (user == null)
            {
                return NotFound();
            }

            // Возвращаем данные пользователя в формате JSON
            return Ok(user);
        }
    }
}
