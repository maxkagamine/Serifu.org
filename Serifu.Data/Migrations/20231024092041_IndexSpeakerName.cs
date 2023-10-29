using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Serifu.Data.Migrations
{
    /// <inheritdoc />
    public partial class IndexSpeakerName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Quotes_SpeakerEnglish",
                table: "Quotes",
                column: "SpeakerEnglish");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_SpeakerJapanese",
                table: "Quotes",
                column: "SpeakerJapanese");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quotes_SpeakerEnglish",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_SpeakerJapanese",
                table: "Quotes");
        }
    }
}
