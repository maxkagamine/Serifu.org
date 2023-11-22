using Microsoft.EntityFrameworkCore;
using Serifu.Data.Entities;

namespace Serifu.Data;

public class QuotesContext(DbContextOptions<QuotesContext> options) : DbContext(options)
{
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
