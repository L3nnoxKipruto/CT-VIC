using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataEntryApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Entries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PatientId = table.Column<string>(type: "TEXT", nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<double>(type: "REAL", nullable: false),
                    Height = table.Column<double>(type: "REAL", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AdditionalNotes = table.Column<string>(type: "TEXT", nullable: false),
                    IsAcuteAbdomen = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsAbdominopelvicTrauma = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsAbdominopelvicMasses = table.Column<bool>(type: "INTEGER", nullable: false),
                    SpecificHistory = table.Column<string>(type: "TEXT", nullable: false),
                    ProtocolName = table.Column<string>(type: "TEXT", nullable: false),
                    KV = table.Column<double>(type: "REAL", nullable: false),
                    MAS = table.Column<double>(type: "REAL", nullable: false),
                    SliceThickness = table.Column<double>(type: "REAL", nullable: false),
                    RotationTime = table.Column<double>(type: "REAL", nullable: false),
                    Pitch = table.Column<double>(type: "REAL", nullable: false),
                    BeamWidth = table.Column<double>(type: "REAL", nullable: false),
                    ScanningRange = table.Column<double>(type: "REAL", nullable: false),
                    CTDIvol = table.Column<double>(type: "REAL", nullable: false),
                    DLP = table.Column<double>(type: "REAL", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ScanningMode = table.Column<string>(type: "TEXT", nullable: false),
                    IsAecUsed = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReferencePhantom = table.Column<string>(type: "TEXT", nullable: false),
                    IsImageQualityAccepted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entries", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Entries");
        }
    }
}
