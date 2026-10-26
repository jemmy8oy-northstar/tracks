using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balenthiran.Tracks.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20261026120000_RunSongHighlights")]
    public partial class RunSongHighlights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing songs default to false: runs synced before highlights read as having none.
            migrationBuilder.AddColumn<bool>(
                name: "IsFastestPace",
                table: "RunSongs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTopSpeed",
                table: "RunSongs",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFastestPace",
                table: "RunSongs");

            migrationBuilder.DropColumn(
                name: "IsTopSpeed",
                table: "RunSongs");
        }
    }
}
