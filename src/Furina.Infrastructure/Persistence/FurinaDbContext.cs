using System.Text.Json;
using Furina.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Furina.Infrastructure.Persistence;

public class FurinaDbContext(DbContextOptions<FurinaDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<ServiceCatalog> ServiceCatalog => Set<ServiceCatalog>();
    public DbSet<ClinicServicePrice> ClinicServicePrices => Set<ClinicServicePrice>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<PetWeightLog> PetWeightLogs => Set<PetWeightLog>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<MedicalRecordAuditLog> MedicalRecordAuditLogs => Set<MedicalRecordAuditLog>();
    public DbSet<VaccinationRecord> VaccinationRecords => Set<VaccinationRecord>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<AppointmentStatusAuditLog> AppointmentStatusAuditLogs => Set<AppointmentStatusAuditLog>();
    public DbSet<AppointmentReminderLog> AppointmentReminderLogs => Set<AppointmentReminderLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(e =>
        {
            e.ToTable("tenants");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.Slug).HasColumnName("slug").IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(x => x.Email).HasColumnName("email").IsRequired();
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
            e.Property(x => x.FullName).HasColumnName("full_name");
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            e.HasOne(x => x.Tenant).WithMany(t => t.Users)
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("roles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            e.HasOne(x => x.Tenant).WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.ToTable("user_roles");
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.RoleId).HasColumnName("role_id");
            e.HasOne(x => x.User).WithMany(u => u.UserRoles)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Role).WithMany(r => r.UserRoles)
                .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tenant).WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.TokenHash).HasColumnName("token_hash").IsRequired();
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            e.Property(x => x.RevokedAt).HasColumnName("revoked_at");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasOne(x => x.User).WithMany(u => u.RefreshTokens)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tenant).WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Clinic>(e =>
        {
            e.ToTable("clinics");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.Address).HasColumnName("address");
            e.Property(x => x.Phone).HasColumnName("phone");
            e.Property(x => x.Is24hEmergency).HasColumnName("is_24h_emergency");
            e.Property(x => x.IsArchived).HasColumnName("is_archived");

            // Stored as a single jsonb column rather than EF's OwnsMany().ToJson()
            // — a plain converter is simpler to reason about for a list this
            // small, and avoids OwnsMany's own change-tracking quirks for a
            // field that's always replaced wholesale, never mutated in place.
            var openingHoursComparer = new ValueComparer<List<OpeningHour>>(
                (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
                v => v.Aggregate(0, (hash, o) => HashCode.Combine(hash, o.DayOfWeek, o.IsClosed, o.OpenTime, o.CloseTime)),
                v => v.ToList());
            e.Property(x => x.OpeningHours)
                .HasColumnName("opening_hours")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<OpeningHour>>(v, (JsonSerializerOptions?)null) ?? new())
                .Metadata.SetValueComparer(openingHoursComparer);

            e.HasOne(x => x.Tenant).WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Appointment>(e =>
        {
            e.ToTable("appointments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(x => x.ClinicId).HasColumnName("clinic_id").IsRequired();
            e.Property(x => x.PetId).HasColumnName("pet_id").IsRequired();
            e.Property(x => x.ServiceCatalogId).HasColumnName("service_catalog_id");
            e.Property(x => x.VetUserId).HasColumnName("vet_user_id");
            e.Property(x => x.StartTime).HasColumnName("start_time");
            e.Property(x => x.EndTime).HasColumnName("end_time");
            e.Property(x => x.Status).HasColumnName("status").IsRequired();
            e.HasIndex(x => new { x.ClinicId, x.StartTime });
            e.HasIndex(x => new { x.VetUserId, x.StartTime });
            e.HasOne(x => x.Clinic).WithMany()
                .HasForeignKey(x => x.ClinicId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Pet).WithMany()
                .HasForeignKey(x => x.PetId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ServiceCatalog).WithMany()
                .HasForeignKey(x => x.ServiceCatalogId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.VetUser).WithMany()
                .HasForeignKey(x => x.VetUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Tenant).WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ServiceCatalog>(e =>
        {
            e.ToTable("service_catalog");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.DefaultPrice).HasColumnName("default_price").HasColumnType("numeric(12,2)");
            e.Property(x => x.DefaultDurationMinutes).HasColumnName("default_duration_minutes");
            e.Property(x => x.IsArchived).HasColumnName("is_archived");
            e.HasOne(x => x.Tenant).WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClinicServicePrice>(e =>
        {
            e.ToTable("clinic_service_prices");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(x => x.ClinicId).HasColumnName("clinic_id").IsRequired();
            e.Property(x => x.ServiceCatalogId).HasColumnName("service_catalog_id").IsRequired();
            e.Property(x => x.Price).HasColumnName("price").HasColumnType("numeric(12,2)");
            e.Property(x => x.DurationMinutes).HasColumnName("duration_minutes");
            e.HasIndex(x => new { x.ClinicId, x.ServiceCatalogId }).IsUnique();
            e.HasOne(x => x.Clinic).WithMany()
                .HasForeignKey(x => x.ClinicId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ServiceCatalog).WithMany()
                .HasForeignKey(x => x.ServiceCatalogId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tenant).WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Pet>(e =>
        {
            e.ToTable("pets");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(x => x.OwnerId).HasColumnName("owner_id").IsRequired();
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.Species).HasColumnName("species").IsRequired();
            e.Property(x => x.Breed).HasColumnName("breed");
            e.Property(x => x.DateOfBirth).HasColumnName("date_of_birth");
            e.Property(x => x.Gender).HasColumnName("gender");
            e.Property(x => x.PhotoUrl).HasColumnName("photo_url");
            e.Property(x => x.MicrochipId).HasColumnName("microchip_id");
            e.HasIndex(x => x.OwnerId);
            e.HasOne(x => x.Owner).WithMany()
                .HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Tenant).WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PetWeightLog>(e =>
        {
            e.ToTable("pet_weight_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(x => x.PetId).HasColumnName("pet_id").IsRequired();
            e.Property(x => x.WeightKg).HasColumnName("weight_kg").HasColumnType("numeric(6,2)");
            e.Property(x => x.MeasuredAt).HasColumnName("measured_at");
            e.HasIndex(x => new { x.PetId, x.MeasuredAt });
            e.HasOne(x => x.Pet).WithMany(p => p.WeightLogs)
                .HasForeignKey(x => x.PetId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tenant).WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Visit>(e =>
        {
            e.ToTable("visits");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(x => x.PetId).HasColumnName("pet_id").IsRequired();
            e.Property(x => x.ClinicId).HasColumnName("clinic_id").IsRequired();
            e.Property(x => x.VetUserId).HasColumnName("vet_user_id").IsRequired();
            e.Property(x => x.VisitDate).HasColumnName("visit_date");
            e.HasIndex(x => x.PetId);
            e.HasOne(x => x.Pet).WithMany()
                .HasForeignKey(x => x.PetId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Clinic).WithMany()
                .HasForeignKey(x => x.ClinicId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.VetUser).WithMany()
                .HasForeignKey(x => x.VetUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Tenant).WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MedicalRecord>(e =>
        {
            e.ToTable("medical_records");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(x => x.VisitId).HasColumnName("visit_id").IsRequired();
            e.Property(x => x.PetId).HasColumnName("pet_id").IsRequired();
            e.Property(x => x.VetUserId).HasColumnName("vet_user_id").IsRequired();
            e.Property(x => x.Subjective).HasColumnName("subjective");
            e.Property(x => x.Objective).HasColumnName("objective");
            e.Property(x => x.Assessment).HasColumnName("assessment");
            e.Property(x => x.Plan).HasColumnName("plan");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => x.VisitId).IsUnique();
            e.HasIndex(x => x.PetId);
            e.HasOne(x => x.Visit).WithMany()
                .HasForeignKey(x => x.VisitId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Pet).WithMany()
                .HasForeignKey(x => x.PetId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.VetUser).WithMany()
                .HasForeignKey(x => x.VetUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Tenant).WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MedicalRecordAuditLog>(e =>
        {
            e.ToTable("medical_record_audit_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(x => x.MedicalRecordId).HasColumnName("medical_record_id").IsRequired();
            e.Property(x => x.ChangedByUserId).HasColumnName("changed_by_user_id").IsRequired();
            e.Property(x => x.ChangedAt).HasColumnName("changed_at");
            e.Property(x => x.PreviousSubjective).HasColumnName("previous_subjective");
            e.Property(x => x.PreviousObjective).HasColumnName("previous_objective");
            e.Property(x => x.PreviousAssessment).HasColumnName("previous_assessment");
            e.Property(x => x.PreviousPlan).HasColumnName("previous_plan");
            e.HasIndex(x => x.MedicalRecordId);
            e.HasOne(x => x.MedicalRecord).WithMany()
                .HasForeignKey(x => x.MedicalRecordId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tenant).WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VaccinationRecord>(e =>
        {
            e.ToTable("vaccination_records");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(x => x.PetId).HasColumnName("pet_id").IsRequired();
            e.Property(x => x.VaccineName).HasColumnName("vaccine_name").IsRequired();
            e.Property(x => x.DateGiven).HasColumnName("date_given");
            e.Property(x => x.NextDueDate).HasColumnName("next_due_date");
            e.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
            e.HasIndex(x => new { x.PetId, x.NextDueDate });
            e.HasOne(x => x.Pet).WithMany()
                .HasForeignKey(x => x.PetId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tenant).WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NotificationLog>(e =>
        {
            e.ToTable("notification_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(x => x.VaccinationRecordId).HasColumnName("vaccination_record_id").IsRequired();
            e.Property(x => x.MilestoneDay).HasColumnName("milestone_day");
            e.Property(x => x.SentAt).HasColumnName("sent_at");
            e.HasIndex(x => new { x.VaccinationRecordId, x.MilestoneDay }).IsUnique();
            e.HasOne(x => x.VaccinationRecord).WithMany()
                .HasForeignKey(x => x.VaccinationRecordId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tenant).WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Prescription>(e =>
        {
            e.ToTable("prescriptions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(x => x.VisitId).HasColumnName("visit_id").IsRequired();
            e.Property(x => x.PetId).HasColumnName("pet_id").IsRequired();
            e.Property(x => x.VetUserId).HasColumnName("vet_user_id").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at");

            // Same pattern as Clinic.OpeningHours (TASK-15): a plain jsonb
            // column via a value converter, not EF's OwnsMany().ToJson() —
            // JSON array order is preserved by System.Text.Json, which is
            // exactly what AC-1's "đọc lại đúng thứ tự đã nhập" needs.
            var itemsComparer = new ValueComparer<List<PrescriptionItem>>(
                (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
                v => v.Aggregate(0, (hash, i) => HashCode.Combine(hash, i.DrugName, i.Dosage, i.Frequency, i.DurationDays)),
                v => v.ToList());
            e.Property(x => x.Items)
                .HasColumnName("items")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<PrescriptionItem>>(v, (JsonSerializerOptions?)null) ?? new())
                .Metadata.SetValueComparer(itemsComparer);

            e.HasIndex(x => x.VisitId).IsUnique();
            e.HasIndex(x => new { x.PetId, x.CreatedAt });
            e.HasOne(x => x.Visit).WithMany()
                .HasForeignKey(x => x.VisitId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Pet).WithMany()
                .HasForeignKey(x => x.PetId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Tenant).WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppointmentStatusAuditLog>(e =>
        {
            e.ToTable("appointment_status_audit_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(x => x.AppointmentId).HasColumnName("appointment_id").IsRequired();
            e.Property(x => x.FromStatus).HasColumnName("from_status").IsRequired();
            e.Property(x => x.ToStatus).HasColumnName("to_status").IsRequired();
            e.Property(x => x.ChangedByUserId).HasColumnName("changed_by_user_id").IsRequired();
            e.Property(x => x.ChangedAt).HasColumnName("changed_at");
            e.Property(x => x.Reason).HasColumnName("reason");
            e.HasIndex(x => x.AppointmentId);
            e.HasOne(x => x.Appointment).WithMany()
                .HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tenant).WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppointmentReminderLog>(e =>
        {
            e.ToTable("appointment_reminder_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(x => x.AppointmentId).HasColumnName("appointment_id").IsRequired();
            e.Property(x => x.SentAt).HasColumnName("sent_at");
            e.HasIndex(x => x.AppointmentId).IsUnique();
            e.HasOne(x => x.Appointment).WithMany()
                .HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tenant).WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Staff>(e =>
        {
            e.ToTable("staff");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            e.Property(x => x.ClinicId).HasColumnName("clinic_id").IsRequired();
            e.Property(x => x.JobTitle).HasColumnName("job_title");
            e.HasIndex(x => new { x.UserId, x.ClinicId }).IsUnique();
            e.HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Clinic).WithMany()
                .HasForeignKey(x => x.ClinicId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tenant).WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Shift>(e =>
        {
            e.ToTable("shifts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
            e.Property(x => x.StaffId).HasColumnName("staff_id").IsRequired();
            e.Property(x => x.DayOfWeek).HasColumnName("day_of_week");
            e.Property(x => x.StartTime).HasColumnName("start_time");
            e.Property(x => x.EndTime).HasColumnName("end_time");
            e.HasIndex(x => new { x.StaffId, x.DayOfWeek });
            e.HasOne(x => x.Staff).WithMany(s => s.Shifts)
                .HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tenant).WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
