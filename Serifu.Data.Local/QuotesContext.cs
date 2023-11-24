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

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Serifu.Data.Local;

public class QuotesContext : DbContext
{
    public QuotesContext(IOptions<LocalDataOptions> options)
        : base(new DbContextOptionsBuilder().UseSqlite(new SqliteConnectionStringBuilder() { DataSource = options.Value.DatabasePath }.ToString()).Options)
    { }

    public required DbSet<Quote> Quotes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Quote>()
            .Property(q => q.Id)
            .ValueGeneratedNever();

        modelBuilder.Entity<Quote>()
            .Property(q => q.Source)
            .HasConversion<string>();

        modelBuilder.Entity<Quote>()
            .HasIndex(q => q.Source);

        modelBuilder.Entity<Quote>()
            .HasMany(q => q.Translations)
            .WithOne()
            .HasForeignKey("QuoteId");

        modelBuilder.Entity<Translation>()
            .HasKey("QuoteId", nameof(Translation.Language));

        modelBuilder.Entity<Translation>()
            .OwnsOne(t => t.AudioFile);
    }
}
