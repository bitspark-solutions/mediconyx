using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace mediconyx_api.Migrations
{
    /// <inheritdoc />
    public partial class AddBedChargesAndRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add DefaultDailyRate to Wards
            migrationBuilder.AddColumn<decimal>(
                name: "DefaultDailyRate",
                table: "Wards",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            // Add DailyRate to Beds
            migrationBuilder.AddColumn<decimal>(
                name: "DailyRate",
                table: "Beds",
                type: "numeric(12,2)",
                nullable: true);

            // BedCharges table — one row per day per admission
            migrationBuilder.CreateTable(
                name: "BedCharges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AdmissionId = table.Column<int>(type: "integer", nullable: false),
                    BedId = table.Column<int>(type: "integer", nullable: false),
                    BedIdOnDay = table.Column<int>(type: "integer", nullable: false),
                    ChargeDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DailyRate = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BedCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BedCharges_Admissions_AdmissionId",
                        column: x => x.AdmissionId,
                        principalTable: "Admissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BedCharges_Beds_BedId",
                        column: x => x.BedId,
                        principalTable: "Beds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BedCharges_AdmissionId_ChargeDate",
                table: "BedCharges",
                columns: new[] { "AdmissionId", "ChargeDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BedCharges_TenantId_ChargeDate",
                table: "BedCharges",
                columns: new[] { "TenantId", "ChargeDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BedCharges");
            migrationBuilder.DropColumn(name: "DefaultDailyRate", table: "Wards");
            migrationBuilder.DropColumn(name: "DailyRate", table: "Beds");
        }
    }
}
