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
            .HasIndex(q => q.SpeakerEnglish);

        modelBuilder.Entity<Quote>()
            .HasIndex(q => q.SpeakerJapanese);
    }
}
