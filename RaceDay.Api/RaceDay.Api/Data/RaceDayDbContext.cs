using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Role>(e =>
        {
            e.ToTable("Role");
            e.HasKey(x => x.RoleId);
            e.Property(x => x.RoleName).HasColumnType("varchar(50)");
            e.HasIndex(x => x.RoleName).IsUnique();
            e.HasData(
                new Role { RoleId = 1, RoleName = "Organiser" },
                new Role { RoleId = 2, RoleName = "Participant" });
        });

        b.Entity<Participant>(e =>
        {
            e.ToTable("Participant");
            e.HasKey(x => x.ParticipantId);
            e.Property(x => x.FullName).HasColumnType("varchar(100)");
            e.Property(x => x.Email).HasColumnType("varchar(100)");
            e.Property(x => x.PasswordHash).HasColumnType("varchar(255)");
            e.Property(x => x.ContactNumber).HasColumnType("varchar(20)");
            e.Property(x => x.DateRegistered).HasColumnType("datetime2").HasDefaultValueSql("SYSDATETIME()");
            e.Property(x => x.ProfilePicture).HasColumnType("varchar(300)");
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Role).WithMany(r => r.Participants)
                .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Event>(e =>
        {
            e.ToTable("Event");
            e.HasKey(x => x.EventId);
            e.Property(x => x.EventName).HasColumnType("varchar(150)");
            e.Property(x => x.EventDate).HasColumnType("date");
            e.Property(x => x.Location).HasColumnType("varchar(150)");
            e.Property(x => x.Description).HasColumnType("varchar(max)");
            e.Property(x => x.EventType).HasConversion<string>().HasColumnType("varchar(10)");
            e.Property(x => x.Distance).HasColumnType("decimal(6,2)");
            e.Property(x => x.CreatedDate).HasColumnType("datetime2").HasDefaultValueSql("SYSDATETIME()");
            e.Property(x => x.BannerImage).HasColumnType("varchar(300)");
            e.HasOne(x => x.Organiser).WithMany(p => p.OrganisedEvents)
                .HasForeignKey(x => x.OrganiserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Category>(e =>
        {
            e.ToTable("Category");
            e.HasKey(x => x.CategoryId);
            e.Property(x => x.CategoryName).HasColumnType("varchar(100)");
            e.Property(x => x.Distance).HasColumnType("decimal(6,2)");
            e.Property(x => x.EntryFee).HasColumnType("decimal(8,2)").HasDefaultValue(0m);
            e.HasOne(x => x.Event).WithMany(ev => ev.Categories)
                .HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolment");
            e.HasKey(x => x.EnrolmentId);
            e.Property(x => x.EnrolmentDate).HasColumnType("datetime2").HasDefaultValueSql("SYSDATETIME()");
            e.Property(x => x.Status).HasColumnType("varchar(20)").HasDefaultValue("Confirmed");
            // Prevents double booking (same as the UNIQUE constraint in the SQL script)
            e.HasIndex(x => new { x.ParticipantId, x.CategoryId }).IsUnique();
            e.HasOne(x => x.Participant).WithMany(p => p.Enrolments)
                .HasForeignKey(x => x.ParticipantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(c => c.Enrolments)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Result>(e =>
        {
            e.ToTable("Result");
            e.HasKey(x => x.ResultId);
            e.Property(x => x.FinishTime).HasColumnType("time(0)");
            e.Property(x => x.DateRecorded).HasColumnType("datetime2").HasDefaultValueSql("SYSDATETIME()");
            e.HasOne(x => x.Enrolment).WithOne(en => en.Result)
                .HasForeignKey<Result>(x => x.EnrolmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.EnrolmentId).IsUnique();
        });
    }
}
