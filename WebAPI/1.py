import os
import time
from datetime import datetime

# === Настройки ===
CONTAINER_NAME = "postgres"  # Имя контейнера PostgreSQL
DB_NAME = "SensorDataDb"  # Название базы данных
DB_USER = "postgres"  # Пользователь базы
BACKUP_DIR = "~/backups"  # Каталог для хранения бэкапов (измените, если нужно)
INTERVAL = 3600  # Интервал между бэкапами (в секундах) - 1 час

# Создаём папку для бэкапов, если её нет
if not os.path.exists(BACKUP_DIR):
    os.makedirs(BACKUP_DIR)

print(f" Запуск автоматического резервного копирования PostgreSQL в контейнере '{CONTAINER_NAME}'")

while True:
    # Формируем имя файла с датой и временем
    timestamp = datetime.now().strftime("%Y-%m-%d_%H-%M")
    backup_file = f"{BACKUP_DIR}/backup_{timestamp}.tar"

    # Команда для создания бэкапа через docker exec
    dump_command = f"docker exec {CONTAINER_NAME} pg_dump -U {DB_USER} -d {DB_NAME} > {backup_file}"

    print(f" Создание бэкапа: {backup_file}")
    
    # Запускаем команду
    os.system(dump_command)

    print(f" Бэкап успешно сохранён: {backup_file}")

    # Ожидание перед следующим бэкапом (1 час)
    time.sleep(INTERVAL)
