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

        modelBuilder.Entity<Translation>()
            .OwnsOne(t => t.AudioFile);
    }
}
