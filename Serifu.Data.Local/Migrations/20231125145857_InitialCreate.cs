﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Serifu.Data.Local.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    DateImported = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Translations",
                columns: table => new
                {
                    Language = table.Column<string>(type: "TEXT", nullable: false),
                    QuoteId = table.Column<long>(type: "INTEGER", nullable: false),
                    SpeakerName = table.Column<string>(type: "TEXT", nullable: false),
                    Context = table.Column<string>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    AudioFile_Path = table.Column<string>(type: "TEXT", nullable: true),
                    AudioFile_OriginalName = table.Column<string>(type: "TEXT", nullable: true),
                    AudioFile_LastModified = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Translations", x => new { x.QuoteId, x.Language });
                    table.ForeignKey(
                        name: "FK_Translations_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_Source",
                table: "Quotes",
                column: "Source");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Translations");

            migrationBuilder.DropTable(
                name: "Quotes");
        }
    }
}
