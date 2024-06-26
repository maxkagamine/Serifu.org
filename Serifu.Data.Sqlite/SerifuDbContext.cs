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

using Kagamine.Extensions.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Serifu.Data.Sqlite;

public class SerifuDbContext : DbContext
{
    public SerifuDbContext(DbContextOptions options) : base(options)
    { }

    public required DbSet<Quote> Quotes { get; set; }

    public required DbSet<AudioFile> AudioFiles { get; set; }

    public required DbSet<AudioFileCache> AudioFileCache { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution) // Immutable records
        .EnableSensitiveDataLogging()
        .ConfigureWarnings(x => x.Ignore(
            CoreEventId.SensitiveDataLoggingEnabledWarning,
            CoreEventId.CollectionWithoutComparer));

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

            entity.Property(q => q.AlignmentData)
                .HasConversion(
                    model => model.AsBytes().ToArray(),
                    column => ValueArray.FromBytes<Alignment>(column));
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
                .HasConversion(model => (byte[])model, column => column);
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
