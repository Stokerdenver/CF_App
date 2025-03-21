using System;

namespace WebAPI.Services
{
    public class KalmanFilter
    {
        private double _q; // Процессный шум
        private double _r; // Шум измерений
        private double _p; // Ошибка оценки
        private double _x; // Текущая оценка
        private bool _initialized;

        public KalmanFilter(double q = 0.1, double r = 0.1)
        {
            _q = q;
            _r = r;
            _p = 1;
            _x = 0;
            _initialized = false;
        }

        public double Update(double measurement)
        {
            if (!_initialized)
            {
                _x = measurement;
                _initialized = true;
                return measurement;
            }

            // Предсказание
            _p = _p + _q;

            // Обновление
            double k = _p / (_p + _r); // Коэффициент Калмана
            _x = _x + k * (measurement - _x);
            _p = (1 - k) * _p;

            return _x;
        }

        public void Reset()
        {
            _p = 1;
            _x = 0;
            _initialized = false;
        }
    }
} 