using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Serifu.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class IndexQuoteAudioFileFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Quotes_English_AudioFile",
                table: "Quotes",
                column: "English_AudioFile");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_Japanese_AudioFile",
                table: "Quotes",
                column: "Japanese_AudioFile");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quotes_English_AudioFile",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_Japanese_AudioFile",
                table: "Quotes");
        }
    }
}
