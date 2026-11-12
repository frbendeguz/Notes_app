using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Notes.Api.Data.Entities;

namespace Notes.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<NoteShare> NoteShares => Set<NoteShare>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // D7: timestamps are UTC. SQLite stores DateTime as text without a kind, so mark values read back as UTC.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Note: HasMaxLength is not enforced by SQLite; server-side validation enforces the limits.
        // D2: uniqueness uses normalized columns (Text.Normalize), never SQLite NOCASE.

        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.UserName).IsRequired().HasMaxLength(Limits.UserNameMax);
            e.Property(u => u.NormalizedUserName).IsRequired().HasMaxLength(Limits.UserNameMax);
            e.Property(u => u.PasswordHash).IsRequired();
            e.HasIndex(u => u.NormalizedUserName).IsUnique();
        });

        modelBuilder.Entity<Folder>(e =>
        {
            e.Property(f => f.Name).IsRequired().HasMaxLength(Limits.FolderNameMax);
            e.Property(f => f.NormalizedName).IsRequired().HasMaxLength(Limits.FolderNameMax);
            e.HasIndex(f => new { f.OwnerId, f.NormalizedName }).IsUnique();

            e.HasOne(f => f.Owner)
                .WithMany(u => u.Folders)
                .HasForeignKey(f => f.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Note>(e =>
        {
            e.Property(n => n.Title).IsRequired().HasMaxLength(Limits.TitleMax);
            e.Property(n => n.Content).IsRequired().HasMaxLength(Limits.ContentMax);

            e.HasOne(n => n.Owner)
                .WithMany(u => u.OwnedNotes)
                .HasForeignKey(n => n.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            // D4: deleting a folder keeps its notes, they become folderless.
            e.HasOne(n => n.Folder)
                .WithMany(f => f.Notes)
                .HasForeignKey(n => n.FolderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<NoteShare>(e =>
        {
            e.Property(s => s.Role).HasConversion<int>();
            e.HasIndex(s => new { s.NoteId, s.UserId }).IsUnique();

            // D5: deleting a note or a user removes the related shares.
            e.HasOne(s => s.Note)
                .WithMany(n => n.Shares)
                .HasForeignKey(s => s.NoteId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(s => s.User)
                .WithMany(u => u.SharesReceived)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : DateTime.SpecifyKind(v, DateTimeKind.Utc),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
}
