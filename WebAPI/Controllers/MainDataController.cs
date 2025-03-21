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
    public class MainDataController : ControllerBase
    {
        private readonly AppDbContext _context;
        private static readonly Dictionary<string, DateTime> _lastCalculationTime = new Dictionary<string, DateTime>();

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
            await _context.SaveChangesAsync();

            // Проверка, прошла ли 1 секунда с последнего вычисления для текущего пользователя
            string userId = mainData.username;
            
            bool shouldCalculateDistance = false;

            lock (_lastCalculationTime)
            {
                if (!_lastCalculationTime.ContainsKey(userId) ||
                    (DateTime.UtcNow - _lastCalculationTime[userId]).TotalSeconds >= 0.5)
                {
                    _lastCalculationTime[userId] = DateTime.UtcNow;
                    shouldCalculateDistance = true;
                }
            }

            if (shouldCalculateDistance)
            {
                // Получаем данные только тех пользователей, которые активны в последние 4 секунд
                var activeClients = await _context.main_data
                    .Where(md => md.username != mainData.username &&
                           md.timestamp >= DateTime.UtcNow.AddSeconds(-4))
                    .GroupBy(md => md.username)  // Группируем по имени пользователя
                    .Select(g => g.OrderByDescending(md => md.timestamp).First())  // Выбираем самую последнюю запись для каждого пользователя
                    .ToListAsync();

                foreach (var clientData in activeClients)
                {
                    // Определяем, находятся ли автомобили на одной дороге и движутся в одном направлении
                    bool isSameRoad = IsLikelyOnSameRoad(mainData, clientData);

                    if (isSameRoad)
                    {
                        // Вычисляем расстояние между текущим клиентом и другим клиентом
                        var distance = DistanceCalculator.CalculateDistance(
                            mainData.latitude, mainData.longitude,
                            clientData.latitude, clientData.longitude);

                        // Создаем новую запись для таблицы ClientDistances
                        var clientDistance = new ClientDistance
                        {
                            client_id1 = mainData.username,
                            client_id2 = clientData.username,
                            distance = distance,
                            timestamp = DateTime.UtcNow
                        };

                        // Сохраняем запись в базу данных
                        _context.client_distance.Add(clientDistance);

                        // Определяем, кто лидер
                        await DetermineLeader(mainData, clientData);
                    }
                }

                // Сохраняем изменения в базу данных
                await _context.SaveChangesAsync();
            }

            return Ok(mainData);
        }
        
        // Метод для определения, находятся ли автомобили на одной дороге
        private bool IsLikelyOnSameRoad(MainData car1, MainData car2)
        {
            // Рассчитываем расстояние между автомобилями
            var distance = DistanceCalculator.CalculateDistance(
                car1.latitude, car1.longitude,
                car2.latitude, car2.longitude);

            // Если расстояние слишком большое, сразу считаем, что автомобили не на одной дороге
            if (distance > 0.5) // 500 метров = 0.5 км
                return false;

            // Проверяем направление движения
            double bearingDiff = Math.Abs(car1.bearing - car2.bearing);
            if (bearingDiff > 180) bearingDiff = 360 - bearingDiff;

            // Автомобили на одной дороге, если они близко и движутся примерно в одном направлении
            // (разница в направлении менее 30 градусов) 
            bool sameDirection = bearingDiff < 50;
            
            return sameDirection;
        }

        // Метод для определения лидера
        private async Task DetermineLeader(MainData currentCar, MainData otherCar)
        {
            try
            {
                // Вычисляем разницу координат (в градусах)
                double deltaLat = otherCar.latitude - currentCar.latitude;
                double deltaLon = otherCar.longitude - currentCar.longitude;

                // Переводим разницу координат в метры
                const double metersPerDegreeLat = 111320; // приблизительное значение для широты
                double avgLat = (currentCar.latitude + otherCar.latitude) / 2.0;
                double metersPerDegreeLon = 111320 * Math.Cos(DegreesToRadians(avgLat));

                double deltaY = deltaLat * metersPerDegreeLat; // смещение по оси "север-юг"
                double deltaX = deltaLon * metersPerDegreeLon;  // смещение по оси "восток-запад"

                // Переводим bearing текущего автомобиля из градусов в радианы
                double headingRad = currentCar.bearing * (Math.PI / 180);

                // Единичный вектор направления движения текущего автомобиля:
                double unitX = Math.Sin(headingRad);
                double unitY = Math.Cos(headingRad);

                // Вычисляем проекцию вектора разницы позиций на единичный вектор направления
                double projection = deltaX * unitX + deltaY * unitY;

                // Если проекция положительная, то otherCar находится впереди currentCar,
                // иначе currentCar движется вперед (лидирующий)
                bool isOtherAhead = projection > 0;

                // Обновляем статус лидера на основе вычисленной проекции
                if (isOtherAhead)
                {
                    await UpdateIsLeaderStatus(currentCar.username, false);
                    await UpdateIsLeaderStatus(otherCar.username, true);
                }
                else
                {
                    await UpdateIsLeaderStatus(currentCar.username, true);
                    await UpdateIsLeaderStatus(otherCar.username, false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error determining leader: {ex.Message}");
            }
        }


        // Вспомогательный метод для перевода градусов в радианы
        private double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        // Метод для обновления статуса isleader у пользователя
        private async Task UpdateIsLeaderStatus(string username, bool isLeader)
        {
            var latestData = await _context.main_data
                .Where(md => md.username == username)
                .OrderByDescending(md => md.timestamp)
                .FirstOrDefaultAsync();

            if (latestData != null && latestData.isleader != isLeader)
            {
                latestData.isleader = isLeader;
                _context.Entry(latestData).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
        }
    }
}