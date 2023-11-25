using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Serifu.Data.Local.Migrations
{
    /// <inheritdoc />
    public partial class KancolleQuotesView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                create view KancolleQuotes as
                select
                    Id,
                    en.SpeakerName as SpeakerName_EN,
                    ja.SpeakerName as SpeakerName_JA,
                    en.Context as Context_EN,
                    ja.Context as Context_JA,
                    en.Text as Text_EN,
                    ja.Text as Text_JA,
                    en.Notes,
                    ja.AudioFile_Path,
                    ja.AudioFile_OriginalName,
                    ja.AudioFile_LastModified,
                    DateImported
                from Quotes
                left join Translations en on en.QuoteId = Id and en.Language = 'en'
                left join Translations ja on ja.QuoteId = Id and ja.Language = 'ja'
                where Source = 'Kancolle'
                order by Id
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("drop view KancolleQuotes");
        }
    }
}
