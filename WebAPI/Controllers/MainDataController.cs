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
                // Получаем данные только тех пользователей, которые активны в последние 5 секунд
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
                        await DetermineLeader(mainData, clientData, distance);
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
            // (разница в направлении менее 30 градусов) или движутся в противоположных направлениях
            // (разница около 180 градусов, ±15 градусов)
            bool sameDirection = bearingDiff < 30;
            bool oppositeDirection = bearingDiff > 165 && bearingDiff < 195;

            return sameDirection || oppositeDirection;
        }

        // Метод для определения лидера
        private async Task DetermineLeader(MainData currentCar, MainData otherCar, double distance)
        {
            try
            {
                // Определяем направление от текущего автомобиля к другому
                double bearingBetween = CalculateBearing(
                    currentCar.latitude, currentCar.longitude,
                    otherCar.latitude, otherCar.longitude);

                // Определяем, находится ли один автомобиль впереди другого
                bool isCurrentCarInFront = IsInFront(currentCar.bearing, bearingBetween);
                bool isOtherCarInFront = IsInFront(otherCar.bearing, (bearingBetween + 180) % 360);

                // Получаем предыдущие записи о расстоянии между этими автомобилями
                var previousDistances = await _context.client_distance
                    .Where(cd =>
                        (cd.client_id1 == currentCar.username && cd.client_id2 == otherCar.username) ||
                        (cd.client_id1 == otherCar.username && cd.client_id2 == currentCar.username))
                    .OrderByDescending(cd => cd.timestamp)
                    .Skip(1) // Пропускаем самую последнюю
                    .Take(5) // Берем несколько предыдущих для стабильности определения
                    .ToListAsync();

                if (previousDistances.Any())
                {
                    // Проверка, что отношение "впереди/сзади" было стабильным некоторое время
                    // Если оно изменилось, мы меняем статус лидера

                    // Для текущего автомобиля
                    if (isCurrentCarInFront)
                    {
                        await UpdateIsLeaderStatus(currentCar.username, true);
                        await UpdateIsLeaderStatus(otherCar.username, false);
                    }
                    // Для другого автомобиля
                    else if (isOtherCarInFront)
                    {
                        await UpdateIsLeaderStatus(currentCar.username, false);
                        await UpdateIsLeaderStatus(otherCar.username, true);
                    }
                    // Если не ясно, кто впереди (едут параллельно), сохраняем текущий статус
                }
                else
                {
                    // Если нет предыдущих данных, устанавливаем статус лидера исходя из текущего положения
                    if (isCurrentCarInFront && !isOtherCarInFront)
                    {
                        await UpdateIsLeaderStatus(currentCar.username, true);
                        await UpdateIsLeaderStatus(otherCar.username, false);
                    }
                    else if (isOtherCarInFront && !isCurrentCarInFront)
                    {
                        await UpdateIsLeaderStatus(currentCar.username, false);
                        await UpdateIsLeaderStatus(otherCar.username, true);
                    }
                    // В случае неопределенности (оба впереди или оба сзади) оставляем как есть
                }
            }
            catch (Exception ex)
            {
                // Логирование ошибок
                Console.WriteLine($"Error determining leader: {ex.Message}");
            }
        }

        // Метод для определения, находится ли точка впереди по направлению движения
        private bool IsInFront(double carBearing, double bearingToOtherCar)
        {
            // Вычисляем разницу между направлением движения автомобиля и направлением на другой автомобиль
            double bearingDiff = Math.Abs(carBearing - bearingToOtherCar);

            // Нормализуем разницу
            if (bearingDiff > 180)
                bearingDiff = 360 - bearingDiff;

            // Если разница меньше 90 градусов, то другой автомобиль находится впереди текущего
            // Если больше 90 градусов, то другой автомобиль находится позади текущего
            return bearingDiff < 90;
        }

        // Метод для расчета направления от точки 1 к точке 2
        private double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
        {
            // Конвертируем в радианы
            double latRad1 = lat1 * (Math.PI / 180);
            double lonRad1 = lon1 * (Math.PI / 180);
            double latRad2 = lat2 * (Math.PI / 180);
            double lonRad2 = lon2 * (Math.PI / 180);

            // Вычисляем bearing
            double dLon = lonRad2 - lonRad1;
            double y = Math.Sin(dLon) * Math.Cos(latRad2);
            double x = Math.Cos(latRad1) * Math.Sin(latRad2) -
                      Math.Sin(latRad1) * Math.Cos(latRad2) * Math.Cos(dLon);
            double bearing = Math.Atan2(y, x);

            // Конвертируем в градусы
            bearing = bearing * (180 / Math.PI);

            // Нормализуем от 0 до 360
            bearing = (bearing + 360) % 360;

            return bearing;
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