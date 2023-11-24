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
