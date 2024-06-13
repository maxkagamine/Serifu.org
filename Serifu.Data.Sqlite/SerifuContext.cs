// Copyright (c) Max Kagamine
//
// This program is free software: you can redistribute it and/or modify it under
// the terms of version 3 of the GNU Affero General Public License as published
// by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
// details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see https://www.gnu.org/licenses/.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Runtime.InteropServices;

namespace Serifu.Data.Sqlite;

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
                .HasConversion(
                    model => ImmutableCollectionsMarshal.AsArray(model),
                    column => ImmutableCollectionsMarshal.AsImmutableArray(column))
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
