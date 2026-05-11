using Microsoft.EntityFrameworkCore;
using PasswordManagerSystem.Api.Domain.Entities;


namespace PasswordManagerSystem.Api.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Credential> Credentials => Set<Credential>();
    public DbSet<CredentialAccess> CredentialAccesses => Set<CredentialAccess>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
	public DbSet<CredentialUsageSession> CredentialUsageSessions => Set<CredentialUsageSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>().ToTable("roles");
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Company>().ToTable("companies");
        modelBuilder.Entity<Credential>().ToTable("credentials");
        modelBuilder.Entity<CredentialAccess>().ToTable("credential_access");
        modelBuilder.Entity<AuditLog>().ToTable("audit_log");
        modelBuilder.Entity<RefreshToken>().ToTable("refresh_tokens");

        modelBuilder.Entity<Role>(entity =>
        {
            entity.Property(e => e.AdGroupName).HasColumnName("ad_group_name");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.AdUsername).HasColumnName("ad_username");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.FirstLoginAt).HasColumnName("first_login_at");
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(e => e.RoleSyncedAt).HasColumnName("role_synced_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.Role)
                .WithMany(e => e.Users)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Credential>(entity =>
        {
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
			
			entity.Property(e => e.CredentialType)
				.HasColumnName("credential_type")
				.HasMaxLength(50)
				.IsRequired();
				
			entity.Property(e => e.EncryptedUsername).HasColumnName("encrypted_username");
            entity.Property(e => e.UsernameIv).HasColumnName("username_iv");
            entity.Property(e => e.UsernameTag).HasColumnName("username_tag");
            entity.Property(e => e.EncryptedPassword).HasColumnName("encrypted_password");
            entity.Property(e => e.PasswordIv).HasColumnName("password_iv");
            entity.Property(e => e.PasswordTag).HasColumnName("password_tag");
            entity.Property(e => e.ConnectionValue).HasColumnName("connection_value");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.LastAccessedAt).HasColumnName("last_accessed_at");

            entity.HasOne(e => e.Company)
                .WithMany(e => e.Credentials)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CreatedByUser)
                .WithMany(e => e.CreatedCredentials)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.UpdatedByUser)
                .WithMany(e => e.UpdatedCredentials)
                .HasForeignKey(e => e.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CredentialAccess>(entity =>
        {
            entity.Property(e => e.CredentialId).HasColumnName("credential_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CanView).HasColumnName("can_view");
            entity.Property(e => e.CanWrite).HasColumnName("can_write");
            entity.Property(e => e.CanDelete).HasColumnName("can_delete");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.Credential)
                .WithMany(e => e.AccessRules)
                .HasForeignKey(e => e.CredentialId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                .WithMany(e => e.CredentialAccessRules)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(e => e.UserAccessRules)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedByUser)
                .WithMany(e => e.CreatedAccessRules)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.AdUsername).HasColumnName("ad_username");
            entity.Property(e => e.Action).HasColumnName("action");
            entity.Property(e => e.TargetType).HasColumnName("target_type");
            entity.Property(e => e.TargetId).HasColumnName("target_id");
            entity.Property(e => e.CredentialId).HasColumnName("credential_id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.TargetUserId).HasColumnName("target_user_id");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.UserAgent).HasColumnName("user_agent");
            entity.Property(e => e.Success).HasColumnName("success");
            entity.Property(e => e.Details).HasColumnName("details");
            entity.Property(e => e.PreviousHash).HasColumnName("previous_hash");
            entity.Property(e => e.Hash).HasColumnName("hash");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.User)
                .WithMany(e => e.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Credential)
                .WithMany(e => e.AuditLogs)
                .HasForeignKey(e => e.CredentialId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Company)
                .WithMany(e => e.AuditLogs)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.TargetUser)
                .WithMany()
                .HasForeignKey(e => e.TargetUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id");

            entity.Property(e => e.TokenHash)
                .HasColumnName("token_hash")
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.ExpiresAt)
                .HasColumnName("expires_at");

            entity.Property(e => e.RevokedAt)
                .HasColumnName("revoked_at");

            entity.Property(e => e.ReplacedByTokenHash)
                .HasColumnName("replaced_by_token_hash")
                .HasMaxLength(128);

            entity.Property(e => e.CreatedByIp)
                .HasColumnName("created_by_ip")
                .HasMaxLength(45);

            entity.Property(e => e.RevokedByIp)
                .HasColumnName("revoked_by_ip")
                .HasMaxLength(45);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at");

            entity.HasIndex(e => e.TokenHash)
                .IsUnique();

            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
		
		modelBuilder.Entity<CredentialUsageSession>(entity =>
		{
			entity.ToTable("credential_usage_sessions");

			entity.HasKey(e => e.Id);

			entity.Property(e => e.Id)
				.HasColumnName("id");

			entity.Property(e => e.CredentialId)
				.HasColumnName("credential_id")
				.IsRequired();

			entity.Property(e => e.UserId)
				.HasColumnName("user_id")
				.IsRequired();

			entity.Property(e => e.AdUsername)
				.HasColumnName("ad_username")
				.HasMaxLength(255)
				.IsRequired();

			entity.Property(e => e.ConnectionValue)
				.HasColumnName("connection_value")
				.HasMaxLength(1000);

			entity.Property(e => e.ProcessId)
				.HasColumnName("process_id");

			entity.Property(e => e.StartedAt)
				.HasColumnName("started_at")
				.IsRequired();

			entity.Property(e => e.EndedAt)
				.HasColumnName("ended_at");

			entity.Property(e => e.Status)
				.HasColumnName("status")
				.HasMaxLength(30)
				.IsRequired();

			entity.HasIndex(e => new { e.CredentialId, e.Status })
				.HasDatabaseName("idx_usage_credential_status");

			entity.HasIndex(e => e.UserId)
				.HasDatabaseName("idx_usage_user_id");

			entity.HasIndex(e => e.StartedAt)
				.HasDatabaseName("idx_usage_started_at");

			entity.HasOne(e => e.Credential)
				.WithMany()
				.HasForeignKey(e => e.CredentialId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(e => e.User)
				.WithMany()
				.HasForeignKey(e => e.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		});
    }
}