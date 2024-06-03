//using AirlineReservationSystem_Backend.Models;
//using Microsoft.EntityFrameworkCore;

//public class ApplicationDbContext : DbContext
//{
//    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

//    public DbSet<Plane> Planes { get; set; }
//    public DbSet<Seat> Seats { get; set; }
//    public DbSet<Reservation> Reservations { get; set; }
//    public DbSet<User> Users { get; set; }
//    public DbSet<ReservationSeat> ReservationSeats { get; set; }

//    protected override void OnModelCreating(ModelBuilder modelBuilder)
//    {
//        base.OnModelCreating(modelBuilder);

//        modelBuilder.Entity<Plane>()
//            .HasMany(p => p.Seats)
//            .WithOne(s => s.Plane)
//            .HasForeignKey(s => s.PlaneId);

//        modelBuilder.Entity<Reservation>().ToTable("Reservation");

//        modelBuilder.Entity<Seat>(entity =>
//        {
//            entity.Property(e => e.UserId)
//                .HasMaxLength(450)
//                .IsRequired(false);

//            entity.ToTable("Seat"); // Correctly placed ToTable method
//        });


//        modelBuilder.Entity<User>().ToTable("User");

//        modelBuilder.Entity<ReservationSeat>()
//            .HasKey(rs => new { rs.ReservationId, rs.SeatId });
//    }
//}
