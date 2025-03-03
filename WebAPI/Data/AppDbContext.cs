namespace WebAPI.Data
{
    using Microsoft.EntityFrameworkCore;
    using WebAPI.Models;

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<SensorData> sensordata { get; set; }
        public DbSet<Car> car { get; set; }
        public DbSet<User> user { get; set; }
        public DbSet<MainData> main_data { get; set; }
        public DbSet<WeatherData> weather_data { get; set; }
        public DbSet<ClientDistance> client_distance { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasMany(u => u.Cars)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.user_id);

            modelBuilder.Entity<ClientDistance>()
                .Property(c => c.id)
                .ValueGeneratedOnAdd();

        }


    }
}
