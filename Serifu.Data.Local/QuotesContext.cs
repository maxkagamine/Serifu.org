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

namespace Serifu.Data.Local;

public class QuotesContext : DbContext
{
    public required DbSet<Quote> Quotes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Shared database, consuming projects are expected to start in their project dirs
        optionsBuilder.UseSqlite("Data Source=../quotes.db");
    }

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
    }
}
