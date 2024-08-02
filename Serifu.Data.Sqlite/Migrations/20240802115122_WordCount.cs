using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Serifu.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class WordCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "English_WordCount",
                table: "Quotes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Japanese_WordCount",
                table: "Quotes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "English_WordCount",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Japanese_WordCount",
                table: "Quotes");
        }
    }
}
