using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Serifu.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class QuoteAudioFileFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EF doesn't support FKs on complex properties (https://github.com/dotnet/efcore/issues/31245).
            // Also sqlite doesn't support altering a table's constraints, so we have to copy.

            migrationBuilder.DropIndex(
                name: "IX_Quotes_Source",
                table: "Quotes");

            migrationBuilder.RenameTable(
                name: "Quotes",
                newName: "Quotes_Temp");

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    English_SpeakerName = table.Column<string>(type: "TEXT", nullable: false),
                    English_Context = table.Column<string>(type: "TEXT", nullable: false),
                    English_Text = table.Column<string>(type: "TEXT", nullable: false),
                    English_WordCount = table.Column<int>(type: "INTEGER", nullable: false),
                    English_Notes = table.Column<string>(type: "TEXT", nullable: false),
                    English_AudioFile = table.Column<string>(type: "TEXT", nullable: true),
                    Japanese_SpeakerName = table.Column<string>(type: "TEXT", nullable: false),
                    Japanese_Context = table.Column<string>(type: "TEXT", nullable: false),
                    Japanese_Text = table.Column<string>(type: "TEXT", nullable: false),
                    Japanese_WordCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Japanese_Notes = table.Column<string>(type: "TEXT", nullable: false),
                    Japanese_AudioFile = table.Column<string>(type: "TEXT", nullable: true),
                    AlignmentData = table.Column<byte[]>(type: "BLOB", nullable: false),
                    DateImported = table.Column<DateTime>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);

                    table.ForeignKey(
                        name: "FK_Quotes_sqlar_English_AudioFile",
                        column: x => x.English_AudioFile,
                        principalTable: "sqlar",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Restrict);

                    table.ForeignKey(
                        name: "FK_Quotes_sqlar_Japanese_AudioFile",
                        column: x => x.Japanese_AudioFile,
                        principalTable: "sqlar",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_Source",
                table: "Quotes",
                column: "Source");

            migrationBuilder.Sql("""
                INSERT INTO Quotes (
                    Id,
                    Source,
                    English_SpeakerName,
                    English_Context,
                    English_Text,
                    English_WordCount,
                    English_Notes,
                    English_AudioFile,
                    Japanese_SpeakerName,
                    Japanese_Context,
                    Japanese_Text,
                    Japanese_WordCount,
                    Japanese_Notes,
                    Japanese_AudioFile,
                    AlignmentData,
                    DateImported
                )
                SELECT
                    Id,
                    Source,
                    English_SpeakerName,
                    English_Context,
                    English_Text,
                    English_WordCount,
                    English_Notes,
                    English_AudioFile,
                    Japanese_SpeakerName,
                    Japanese_Context,
                    Japanese_Text,
                    Japanese_WordCount,
                    Japanese_Notes,
                    Japanese_AudioFile,
                    AlignmentData,
                    DateImported
                FROM Quotes_Temp;
                """);

            migrationBuilder.DropTable("Quotes_Temp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
