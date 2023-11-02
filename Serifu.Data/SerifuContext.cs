using Microsoft.EntityFrameworkCore;
using Serifu.Data.Entities;

namespace Serifu.Data;

public class SerifuContext : DbContext
{
    public SerifuContext(DbContextOptions<SerifuContext> options) : base(options)
    { }

    public required DbSet<VoiceLine> VoiceLines { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VoiceLine>()
            .Property(v => v.Source)
            .HasConversion<string>();

        modelBuilder.Entity<VoiceLine>()
            .HasIndex(v => v.Source);

        modelBuilder.Entity<VoiceLine>()
            .HasIndex(v => v.SpeakerEnglish);

        modelBuilder.Entity<VoiceLine>()
            .HasIndex(v => v.SpeakerJapanese);
    }
}
