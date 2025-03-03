# 1. Используем официальный .NET SDK для сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# 2. Копируем файлы проекта
COPY . .

# 3. Восстанавливаем зависимости
RUN dotnet restore "WebAPI.csproj"

# 4. Публикуем приложение
RUN dotnet publish "WebAPI.csproj" -c Release -o /app/publish

# 5. Используем легковесный образ для запуска
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# 6. Копируем собранное приложение
COPY --from=build /app/publish .

# 7. Запускаем
ENTRYPOINT ["dotnet", "WebAPI.dll"]
