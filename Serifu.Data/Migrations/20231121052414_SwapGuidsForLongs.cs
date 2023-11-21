using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Serifu.Data.Migrations
{
    /// <inheritdoc />
    public partial class SwapGuidsForLongs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Quotes");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "Quotes",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Quotes",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Quotes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
