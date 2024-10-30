namespace WebAPI.Services
{
    public static class DistanceCalculator
    {   

        // используем формулу Хаверсина
        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Радиус Земли в километрах
            var latRad1 = ToRadians(lat1);
            var lonRad1 = ToRadians(lon1);
            var latRad2 = ToRadians(lat2);
            var lonRad2 = ToRadians(lon2);

            var latDiff = latRad2 - latRad1;
            var lonDiff = lonRad2 - lonRad1;

            var a = Math.Sin(latDiff / 2) * Math.Sin(latDiff / 2) +
                    Math.Cos(latRad1) * Math.Cos(latRad2) *
                    Math.Sin(lonDiff / 2) * Math.Sin(lonDiff / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c; // Расстояние в километрах
        }

        private static double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }
    }

}
