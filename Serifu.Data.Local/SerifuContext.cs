﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Serifu.Data.Local;

public class SerifuContext : DbContext
{
    public SerifuContext(DbContextOptions options) : base(options)
    {
        SavedChanges += OnSavedChanges;
    }

    public required DbSet<Quote> Quotes { get; set; }

    public required DbSet<AudioFile> AudioFiles { get; set; }

    public required DbSet<AudioFileCache> AudioFileCache { get; set; }

    private void OnSavedChanges(object? sender, SavedChangesEventArgs e)
    {
        ChangeTracker.Clear(); // Immutable records
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder
        .UseSqlite("Data Source=../Serifu.db; Pooling=false")
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution) // Immutable records
        .EnableSensitiveDataLogging();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Quote>(entity =>
        {
            entity.Property(q => q.Id)
                .ValueGeneratedNever();

            entity.Property(q => q.Source)
                .HasConversion<string>();

            entity.HasIndex(q => q.Source);

            entity.ComplexProperty(q => q.English);

            entity.ComplexProperty(q => q.Japanese);
        });

        modelBuilder.Entity<AudioFile>(entity =>
        {
            entity.ToTable("sqlar");

            entity.HasKey(a => a.ObjectName);
            entity.Property(a => a.ObjectName)
                .HasColumnName("name");

            entity.Property(a => a.Mode)
                .HasColumnName("mode");

            entity.Property(a => a.DateImported)
                .HasColumnName("mtime")
                .HasConversion(
                    model => new DateTimeOffset(model).ToUnixTimeSeconds(),
                    column => DateTimeOffset.FromUnixTimeSeconds(column).UtcDateTime);

            entity.Property(a => a.Size)
                .HasColumnName("sz");

            entity.Property(a => a.Data)
                .HasColumnName("data")
                .HasConversion(model => (byte[])model, column => column)
                .Metadata.SetValueComparer(ValueComparer.CreateDefault<object>(false));
        });


        modelBuilder.Entity<AudioFileCache>(entity =>
        {
            entity.HasKey(c => c.OriginalUri);

            entity.HasOne<AudioFile>()
                .WithMany()
                .HasForeignKey(c => c.ObjectName)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
