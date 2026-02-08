using Microsoft.EntityFrameworkCore;
using NotificationsTelegram.Models;

public class NotificationsDbContext : DbContext
{
    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options)
    {
    }

    public DbSet<DocumentType> DocumentTypes { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // DocumentType configuration
        modelBuilder.Entity<DocumentType>(entity =>
        {
            entity.HasIndex(e => e.Code).HasDatabaseName("IX_DocumentTypes_Code");
        });

        // Notification configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_Notifications_Status");
            entity.HasIndex(e => e.IdAuthorize).HasDatabaseName("IX_Notifications_IdAuthorize");
            entity.HasIndex(e => e.IdSolicit).HasDatabaseName("IX_Notifications_IdSolicit");
            entity.HasIndex(e => e.Folio).HasDatabaseName("IX_Notifications_Folio");
            entity.HasIndex(e => e.TelegramChatId).HasDatabaseName("IX_Notifications_TelegramChatId");
            entity.HasIndex(e => e.AccessToken).IsUnique().HasFilter("[AccessToken] IS NOT NULL").HasDatabaseName("IX_Notifications_AccessToken");

            entity.HasOne(e => e.DocumentType)
                  .WithMany(d => d.Notifications)
                  .HasForeignKey(e => e.IdDocumentType)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // NotificationLog configuration
        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.HasIndex(e => e.IdNotification).HasDatabaseName("IX_NotificationLogs_IdNotification");

            entity.HasOne(e => e.Notification)
                  .WithMany(n => n.Logs)
                  .HasForeignKey(e => e.IdNotification)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
