using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Serifu.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AudioFileCache",
                columns: table => new
                {
                    OriginalUri = table.Column<string>(type: "TEXT", nullable: false),
                    ObjectName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioFileCache", x => x.OriginalUri);
                    table.ForeignKey(
                        name: "FK_AudioFileCache_sqlar_ObjectName",
                        column: x => x.ObjectName,
                        principalTable: "sqlar",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    English_SpeakerName = table.Column<string>(type: "TEXT", nullable: false),
                    English_Context = table.Column<string>(type: "TEXT", nullable: false),
                    English_Text = table.Column<string>(type: "TEXT", nullable: false),
                    English_Notes = table.Column<string>(type: "TEXT", nullable: false),
                    English_AudioFile = table.Column<string>(type: "TEXT", nullable: true),
                    Japanese_SpeakerName = table.Column<string>(type: "TEXT", nullable: false),
                    Japanese_Context = table.Column<string>(type: "TEXT", nullable: false),
                    Japanese_Text = table.Column<string>(type: "TEXT", nullable: false),
                    Japanese_Notes = table.Column<string>(type: "TEXT", nullable: false),
                    Japanese_AudioFile = table.Column<string>(type: "TEXT", nullable: true),
                    DateImported = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AudioFileCache_ObjectName",
                table: "AudioFileCache",
                column: "ObjectName");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_Source",
                table: "Quotes",
                column: "Source");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AudioFileCache");

            migrationBuilder.DropTable(
                name: "Quotes");
        }
    }
}
