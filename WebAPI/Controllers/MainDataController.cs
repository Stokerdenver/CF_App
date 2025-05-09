namespace WebAPI.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Text.Json;
    using System.Text;
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
        private static readonly Dictionary<string, KalmanFilter> _bearingFilters = new Dictionary<string, KalmanFilter>();

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

            // Сглаживаем значение bearing с помощью фильтра Калмана
            if (!_bearingFilters.ContainsKey(mainData.username))
            {
                _bearingFilters[mainData.username] = new KalmanFilter(q: 0.1, r: 0.5);
            }
            mainData.bearing = _bearingFilters[mainData.username].Update(mainData.bearing);

            mainData.isleader = false;

            // Добавляем данные в базу данных
            _context.main_data.Add(mainData);
            await _context.SaveChangesAsync();

            // === [1] Если пользователь ведомый — вызываем FastAPI ===
            double? predictedSpeed = null;
            if (!mainData.isleader)
            {
                predictedSpeed = await GetPredictedSpeedForFollower(mainData.username);
            }

            // Проверка, прошла ли 1 секунда с последнего вычисления для текущего пользователя
            string userId = mainData.username;
            
            bool shouldCalculateDistance = false;

            lock (_lastCalculationTime)
            {
                if (!_lastCalculationTime.ContainsKey(userId) ||
                    (DateTime.UtcNow - _lastCalculationTime[userId]).TotalSeconds >= 1.0)
                {
                    _lastCalculationTime[userId] = DateTime.UtcNow;
                    shouldCalculateDistance = true;
                }
            }

            if (shouldCalculateDistance)
            {
                // Получаем данные только тех пользователей, которые активны в последние 2 секунды
                var activeClients = await _context.main_data
                    .Where(md => md.username != mainData.username &&
                           md.timestamp >= DateTime.UtcNow.AddSeconds(-2))
                    .GroupBy(md => md.username)
                    .Select(g => g.OrderByDescending(md => md.timestamp).First())
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
                       // await DetermineLeader(mainData, clientData);
                    }
                }

                // Сохраняем изменения в базу данных
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                isleader = mainData.isleader,
                predicted_speed = predictedSpeed.HasValue ? Math.Round(predictedSpeed.Value, 2) : (double?)null,
                mainData.latitude,
                mainData.longitude,
                mainData.speed,
                mainData.bearing,
                mainData.timestamp
            });
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

            // Проверяем направление движения с учетом сглаженных значений bearing
            double bearingDiff = Math.Abs(car1.bearing - car2.bearing);
            if (bearingDiff > 180) bearingDiff = 360 - bearingDiff;

            // Увеличиваем допустимую разницу в направлении до 60 градусов
            // из-за сглаживания значений
            bool sameDirection = bearingDiff < 60;
            
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

        


private async Task<double?> GetPredictedSpeedForFollower(string username)
    {
        // 1. Получаем последние 10 записей из main_data
        var recentEntries = await _context.main_data
            .Where(md => md.username == username)
            .OrderByDescending(md => md.timestamp)
            .Take(10)
            .ToListAsync();

        if (recentEntries.Count < 10)
            return null;

        // 2. Получаем погодные данные
        var latestWeather = await _context.weather_data
            .Where(w => w.username == username)
            .OrderByDescending(w => w.timestamp)
            .FirstOrDefaultAsync();

        // 3. Получаем данные пользователя и автомобиля
        var user = await _context.user
            .Include(u => u.Cars)
            .FirstOrDefaultAsync(u => u.name == username);

        var car = user?.Cars?.FirstOrDefault();

        // 4. Формируем список записей
        var inputList = recentEntries
            .OrderBy(e => e.timestamp) // важно: по возрастанию
            .Select(e => new
            {
                speed_follower = e.speed,
                temp = latestWeather?.temp_c ?? 10,
                precip_mm = latestWeather?.precip_mm ?? 0,
                age = user?.age ?? 30,
                driving_experience = user?.driving_exp ?? 5,
                gender_encoded = user?.sex == "М" ? 1 : 0,
                car_year = car?.release_year ?? 2015
            })
            .ToList();

        // 5. Сериализуем и отправляем POST-запрос в FastAPI
        var json = JsonSerializer.Serialize(inputList);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(0.5)
            };
            try
            {
                var response = await client.PostAsync("http://45.84.225.138:8000/predict-speed", content);

                if (!response.IsSuccessStatusCode)
                    return null;

                var responseBody = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("predicted_speeds", out var speed) &&
                    speed.ValueKind == JsonValueKind.Number)
                {
                    return speed.GetDouble();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при запросе к FastAPI: {ex.Message}");
               
            }
            return null;
        }

    }
}