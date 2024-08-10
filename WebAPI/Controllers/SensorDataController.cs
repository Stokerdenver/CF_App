namespace WebAPI.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;
    using WebAPI.Data;
    using WebAPI.Models;

    [ApiController]
    [Route("api/[controller]")]
    public class SensorDataController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SensorDataController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SensorData data)
        {
            if (data == null)
                return BadRequest();

            data.timestamp = DateTime.UtcNow;
            _context.sensordata.Add(data);
            await _context.SaveChangesAsync();

            return Ok();
        }
       
    }

}
