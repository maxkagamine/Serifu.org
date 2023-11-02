using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Serifu.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenamedQuotesToVoiceLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(name: "Quotes", newName: "VoiceLines");
            migrationBuilder.RenameColumn(table: "VoiceLines", name: "QuoteEnglish", newName: "TextEnglish");
            migrationBuilder.RenameColumn(table: "VoiceLines", name: "QuoteJapanese", newName: "TextJapanese");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(table: "VoiceLines", name: "TextEnglish", newName: "QuoteEnglish");
            migrationBuilder.RenameColumn(table: "VoiceLines", name: "TextJapanese", newName: "QuoteJapanese");
            migrationBuilder.RenameTable(name: "VoiceLines", newName: "Quotes");
        }
    }
}
