using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balenthiran.Tracks.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20261026130000_RunContext")]
    public partial class RunContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing runs stay null: runs synced before BEH-HIGH-2 say nothing about their context.
            migrationBuilder.AddColumn<string>(
                name: "ContextName",
                table: "Runs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContextType",
                table: "Runs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContextName",
                table: "Runs");

            migrationBuilder.DropColumn(
                name: "ContextType",
                table: "Runs");
        }
    }
}
