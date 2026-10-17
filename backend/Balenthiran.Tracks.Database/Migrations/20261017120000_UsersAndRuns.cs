using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balenthiran.Tracks.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20261017120000_UsersAndRuns")]
    public partial class UsersAndRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StravaAthleteId = table.Column<long>(type: "bigint", nullable: false),
                    StravaRefreshToken = table.Column<string>(type: "text", nullable: false),
                    SpotifyRefreshToken = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SportType = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ElapsedSeconds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Runs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Runs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RunSongs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Artists = table.Column<List<string>>(type: "text[]", nullable: false),
                    AlbumArtUrl = table.Column<string>(type: "text", nullable: true),
                    PlayedFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PlayedTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OffsetSeconds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunSongs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RunSongs_Runs_RunId",
                        column: x => x.RunId,
                        principalTable: "Runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_StravaAthleteId",
                table: "Users",
                column: "StravaAthleteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Runs_UserId_ActivityId",
                table: "Runs",
                columns: new[] { "UserId", "ActivityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Runs_UserId_StartedAt",
                table: "Runs",
                columns: new[] { "UserId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RunSongs_RunId_Position",
                table: "RunSongs",
                columns: new[] { "RunId", "Position" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RunSongs");

            migrationBuilder.DropTable(
                name: "Runs");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
