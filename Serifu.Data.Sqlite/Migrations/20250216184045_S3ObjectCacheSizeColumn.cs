using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Serifu.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class S3ObjectCacheSizeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Size",
                table: "S3ObjectCache",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Size",
                table: "S3ObjectCache");
        }
    }
}
