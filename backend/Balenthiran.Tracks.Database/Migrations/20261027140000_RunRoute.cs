using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balenthiran.Tracks.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20261027140000_RunRoute")]
    public partial class RunRoute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing runs stay null on both: no route yet, and StreamsReadAt null is what the
            // backfill looks for (BEH-SYNC-6), so they get their highlights and route after deploy.
            migrationBuilder.AddColumn<string>(
                name: "Route",
                table: "Runs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StreamsReadAt",
                table: "Runs",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Route",
                table: "Runs");

            migrationBuilder.DropColumn(
                name: "StreamsReadAt",
                table: "Runs");
        }
    }
}
