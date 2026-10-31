using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balenthiran.Tracks.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20261031180000_StravaDescription")]
    public partial class StravaDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BEH-SHARE-2. Existing runners have no recorded scope (null = read-only, until they sign in
            // again) and the opt-in starts off for everyone.
            migrationBuilder.AddColumn<string>(
                name: "StravaScope",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WriteSongsToStravaDescription",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StravaScope",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WriteSongsToStravaDescription",
                table: "Users");
        }
    }
}
