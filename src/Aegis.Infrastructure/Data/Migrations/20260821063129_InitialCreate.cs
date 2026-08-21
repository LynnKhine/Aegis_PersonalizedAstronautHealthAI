using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aegis.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Astronauts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    NASAId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MissionStartDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Astronauts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BiometricReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AstronautId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MetricType = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<double>(type: "REAL", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ZScore = table.Column<double>(type: "REAL", nullable: false),
                    Severity = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BiometricReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BiometricReadings_Astronauts_AstronautId",
                        column: x => x.AstronautId,
                        principalTable: "Astronauts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonalBaselines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AstronautId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MetricType = table.Column<string>(type: "TEXT", nullable: false),
                    Mean = table.Column<double>(type: "REAL", nullable: false),
                    StdDev = table.Column<double>(type: "REAL", nullable: false),
                    SumOfSquaredDeviations = table.Column<double>(type: "REAL", nullable: false),
                    SampleCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalBaselines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalBaselines_Astronauts_AstronautId",
                        column: x => x.AstronautId,
                        principalTable: "Astronauts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InterventionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AstronautId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TriggeredByReadingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: false),
                    ImmediateActionsJson = table.Column<string>(type: "TEXT", nullable: false),
                    MonitoringFrequency = table.Column<string>(type: "TEXT", nullable: false),
                    EscalateToFlightSurgeon = table.Column<bool>(type: "INTEGER", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterventionPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterventionPlans_Astronauts_AstronautId",
                        column: x => x.AstronautId,
                        principalTable: "Astronauts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterventionPlans_BiometricReadings_TriggeredByReadingId",
                        column: x => x.TriggeredByReadingId,
                        principalTable: "BiometricReadings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Astronauts_NASAId",
                table: "Astronauts",
                column: "NASAId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BiometricReadings_AstronautId_MetricType",
                table: "BiometricReadings",
                columns: new[] { "AstronautId", "MetricType" });

            migrationBuilder.CreateIndex(
                name: "IX_BiometricReadings_RecordedAt",
                table: "BiometricReadings",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InterventionPlans_AstronautId",
                table: "InterventionPlans",
                column: "AstronautId");

            migrationBuilder.CreateIndex(
                name: "IX_InterventionPlans_TriggeredByReadingId",
                table: "InterventionPlans",
                column: "TriggeredByReadingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonalBaselines_AstronautId_MetricType",
                table: "PersonalBaselines",
                columns: new[] { "AstronautId", "MetricType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterventionPlans");

            migrationBuilder.DropTable(
                name: "PersonalBaselines");

            migrationBuilder.DropTable(
                name: "BiometricReadings");

            migrationBuilder.DropTable(
                name: "Astronauts");
        }
    }
}
