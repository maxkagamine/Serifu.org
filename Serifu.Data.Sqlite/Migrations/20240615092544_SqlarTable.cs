using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Serifu.Data.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class SqlarTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sqlar",
                columns: table => new
                {
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    mode = table.Column<int>(type: "INTEGER", nullable: false),
                    mtime = table.Column<long>(type: "INTEGER", nullable: false),
                    sz = table.Column<int>(type: "INTEGER", nullable: false),
                    data = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sqlar", x => x.name);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sqlar");
        }
    }
}
