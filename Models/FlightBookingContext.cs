using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Models;

public partial class FlightBookingContext : DbContext
{
    public FlightBookingContext()
    {
    }

    public FlightBookingContext(DbContextOptions<FlightBookingContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AircraftType> AircraftTypes { get; set; }

    public virtual DbSet<Airline> Airlines { get; set; }

    public virtual DbSet<Airport> Airports { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<BookingSeat> BookingSeats { get; set; }

    public virtual DbSet<Flight> Flights { get; set; }

    public virtual DbSet<Seat> Seats { get; set; }

    public virtual DbSet<SeatClass> SeatClasses { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public DbSet<Payment> Payments { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("server=(local); database=FlightBooking; uid=sa; pwd=123; Trusted_Connection=True; Encrypt=False");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AircraftType>(entity =>
        {
            entity.HasKey(e => e.AircraftTypeId).HasName("PK__aircraft__7F5C1091E227CCCB");

            entity.ToTable("aircraft_types");

            entity.Property(e => e.AircraftTypeId).HasColumnName("aircraft_type_id");
            entity.Property(e => e.AircraftModel)
                .HasMaxLength(50)
                .HasColumnName("aircraft_model");
            entity.Property(e => e.BusinessSeats).HasColumnName("business_seats");
            entity.Property(e => e.EconomySeats).HasColumnName("economy_seats");
            entity.Property(e => e.FirstClassSeats).HasColumnName("first_class_seats");
            entity.Property(e => e.Manufacturer)
                .HasMaxLength(50)
                .HasColumnName("manufacturer");
            entity.Property(e => e.SeatMapLayout)
                .HasMaxLength(20)
                .HasColumnName("seat_map_layout");
            entity.Property(e => e.TotalSeats).HasColumnName("total_seats");
        });

        modelBuilder.Entity<Airline>(entity =>
        {
            entity.HasKey(e => e.AirlineId).HasName("PK__airlines__A016BF80A699215B");

            entity.ToTable("airlines");

            entity.HasIndex(e => e.AirlineCode, "UQ__airlines__7E72435649F5EB1B").IsUnique();

            entity.Property(e => e.AirlineId).HasColumnName("airline_id");
            entity.Property(e => e.AirlineCode)
                .HasMaxLength(10)
                .HasColumnName("airline_code");
            entity.Property(e => e.AirlineName)
                .HasMaxLength(100)
                .HasColumnName("airline_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.LogoUrl)
                .HasMaxLength(255)
                .HasColumnName("logo_url");
        });

        modelBuilder.Entity<Airport>(entity =>
        {
            entity.HasKey(e => e.AirportId).HasName("PK__airports__C795D516C911C753");

            entity.ToTable("airports");

            entity.HasIndex(e => e.AirportCode, "UQ__airports__E949ADC76B684E40").IsUnique();

            entity.Property(e => e.AirportId).HasColumnName("airport_id");
            entity.Property(e => e.AirportCode)
                .HasMaxLength(10)
                .HasColumnName("airport_code");
            entity.Property(e => e.AirportName)
                .HasMaxLength(100)
                .HasColumnName("airport_name");
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .HasColumnName("city");
            entity.Property(e => e.Country)
                .HasMaxLength(50)
                .HasColumnName("country");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Timezone)
                .HasMaxLength(50)
                .HasColumnName("timezone");
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK__bookings__5DE3A5B15613F43D");

            entity.ToTable("bookings");

            entity.HasIndex(e => e.BookingReference, "IX_bookings_reference");

            entity.HasIndex(e => e.BookingStatus, "IX_bookings_status");

            entity.HasIndex(e => e.UserId, "IX_bookings_user");

            entity.HasIndex(e => e.BookingReference, "UQ__bookings__BADA4559CFA36B8E").IsUnique();

            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.BookingDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("booking_date");
            entity.Property(e => e.BookingReference)
                .HasMaxLength(20)
                .HasColumnName("booking_reference");
            entity.Property(e => e.BookingStatus)
                .HasMaxLength(20)
                .HasDefaultValue("CONFIRMED")
                .HasColumnName("booking_status");
            entity.Property(e => e.FlightId).HasColumnName("flight_id");
            entity.Property(e => e.Notes)
                .HasColumnType("ntext")
                .HasColumnName("notes");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .HasDefaultValue("PENDING")
                .HasColumnName("payment_status");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Flight).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.FlightId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bookings_flight");

            entity.HasOne(d => d.User).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bookings_user");
        });

        modelBuilder.Entity<BookingSeat>(entity =>
        {
            entity.HasKey(e => e.BookingSeatId).HasName("PK__booking___C073D47DEA505F8B");

            entity.ToTable("booking_seats", tb =>
                {
                    tb.HasTrigger("tr_restore_seat_availability");
                    tb.HasTrigger("tr_update_seat_availability");
                });

            entity.HasIndex(e => new { e.BookingId, e.SeatId }, "UQ_booking_seats").IsUnique();

            entity.Property(e => e.BookingSeatId).HasColumnName("booking_seat_id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.PassengerIdNumber)
                .HasMaxLength(50)
                .HasColumnName("passenger_id_number");
            entity.Property(e => e.PassengerName)
                .HasMaxLength(100)
                .HasColumnName("passenger_name");
            entity.Property(e => e.SeatId).HasColumnName("seat_id");
            entity.Property(e => e.SeatPrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("seat_price");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingSeats)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_booking_seats_booking");

            entity.HasOne(d => d.Seat).WithMany(p => p.BookingSeats)
                .HasForeignKey(d => d.SeatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_booking_seats_seat");
        });

        modelBuilder.Entity<Flight>(entity =>
        {
            entity.HasKey(e => e.FlightId).HasName("PK__flights__E3705765D1845355");

            entity.ToTable("flights");

            entity.HasIndex(e => new { e.DepartureAirportId, e.ArrivalAirportId, e.DepartureTime }, "IX_flights_route_date");

            entity.HasIndex(e => e.Status, "IX_flights_status");

            entity.Property(e => e.FlightId).HasColumnName("flight_id");
            entity.Property(e => e.AircraftTypeId).HasColumnName("aircraft_type_id");
            entity.Property(e => e.AirlineId).HasColumnName("airline_id");
            entity.Property(e => e.ArrivalAirportId).HasColumnName("arrival_airport_id");
            entity.Property(e => e.ArrivalTime).HasColumnName("arrival_time");
            entity.Property(e => e.BasePrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("base_price");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.DepartureAirportId).HasColumnName("departure_airport_id");
            entity.Property(e => e.DepartureTime).HasColumnName("departure_time");
            entity.Property(e => e.FlightNumber)
                .HasMaxLength(20)
                .HasColumnName("flight_number");
            entity.Property(e => e.Gate)
                .HasMaxLength(10)
                .HasColumnName("gate");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("SCHEDULED")
                .HasColumnName("status");

            entity.HasOne(d => d.AircraftType).WithMany(p => p.Flights)
                .HasForeignKey(d => d.AircraftTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_flights_aircraft");

            entity.HasOne(d => d.Airline).WithMany(p => p.Flights)
                .HasForeignKey(d => d.AirlineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_flights_airline");

            entity.HasOne(d => d.ArrivalAirport).WithMany(p => p.FlightArrivalAirports)
                .HasForeignKey(d => d.ArrivalAirportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_flights_arrival");

            entity.HasOne(d => d.DepartureAirport).WithMany(p => p.FlightDepartureAirports)
                .HasForeignKey(d => d.DepartureAirportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_flights_departure");
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasKey(e => e.SeatId).HasName("PK__seats__906DED9C4F3271E6");

            entity.ToTable("seats");

            entity.HasIndex(e => e.ClassId, "IX_seats_class");

            entity.HasIndex(e => new { e.FlightId, e.IsAvailable }, "IX_seats_flight_available");

            entity.HasIndex(e => new { e.FlightId, e.SeatNumber }, "UQ_seats_flight_number").IsUnique();

            entity.Property(e => e.SeatId).HasColumnName("seat_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.ExtraFee)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("extra_fee");
            entity.Property(e => e.FlightId).HasColumnName("flight_id");
            entity.Property(e => e.IsAisle)
                .HasDefaultValue(false)
                .HasColumnName("is_aisle");
            entity.Property(e => e.IsAvailable)
                .HasDefaultValue(true)
                .HasColumnName("is_available");
            entity.Property(e => e.IsEmergencyExit)
                .HasDefaultValue(false)
                .HasColumnName("is_emergency_exit");
            entity.Property(e => e.IsWindow)
                .HasDefaultValue(false)
                .HasColumnName("is_window");
            entity.Property(e => e.SeatColumn)
                .HasMaxLength(2)
                .HasColumnName("seat_column");
            entity.Property(e => e.SeatNumber)
                .HasMaxLength(10)
                .HasColumnName("seat_number");
            entity.Property(e => e.SeatRow).HasColumnName("seat_row");

            entity.HasOne(d => d.Class).WithMany(p => p.Seats)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_seats_class");

            entity.HasOne(d => d.Flight).WithMany(p => p.Seats)
                .HasForeignKey(d => d.FlightId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_seats_flight");
        });

        modelBuilder.Entity<SeatClass>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("PK__seat_cla__FDF47986A02EAC4B");

            entity.ToTable("seat_classes");

            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.ClassDescription)
                .HasMaxLength(100)
                .HasColumnName("class_description");
            entity.Property(e => e.ClassName)
                .HasMaxLength(20)
                .HasColumnName("class_name");
            entity.Property(e => e.PriceMultiplier)
                .HasDefaultValue(1.0m)
                .HasColumnType("decimal(3, 2)")
                .HasColumnName("price_multiplier");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__users__B9BE370F2366BD93");

            entity.ToTable("users", tb => tb.HasTrigger("tr_users_update_timestamp"));

            entity.HasIndex(e => e.Email, "IX_users_email");

            entity.HasIndex(e => e.Username, "IX_users_username");

            entity.HasIndex(e => e.Email, "UQ__users__AB6E616475C24318").IsUnique();

            entity.HasIndex(e => e.Username, "UQ__users__F3DBC57289A37B25").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .HasColumnName("gender");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_at");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");
        });

        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.TransactionId)
            .IsUnique();

        // Notification configurations
        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.Status });

        modelBuilder.Entity<Notification>()
            .HasIndex(n => n.CreatedAt);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
