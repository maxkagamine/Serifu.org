using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Serifu.Data.Local.Migrations
{
    /// <inheritdoc />
    public partial class TranslationsTableName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Translation_Quotes_QuoteId",
                table: "Translation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Translation",
                table: "Translation");

            migrationBuilder.RenameTable(
                name: "Translation",
                newName: "Translations");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Translations",
                table: "Translations",
                columns: new[] { "QuoteId", "Language" });

            migrationBuilder.AddForeignKey(
                name: "FK_Translations_Quotes_QuoteId",
                table: "Translations",
                column: "QuoteId",
                principalTable: "Quotes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Translations_Quotes_QuoteId",
                table: "Translations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Translations",
                table: "Translations");

            migrationBuilder.RenameTable(
                name: "Translations",
                newName: "Translation");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Translation",
                table: "Translation",
                columns: new[] { "QuoteId", "Language" });

            migrationBuilder.AddForeignKey(
                name: "FK_Translation_Quotes_QuoteId",
                table: "Translation",
                column: "QuoteId",
                principalTable: "Quotes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
