using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace mediconyx_api.Migrations
{
    /// <inheritdoc />
    public partial class AddIpdWardsBedsAdmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Wards
            migrationBuilder.CreateTable(
                name: "Wards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WardType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Floor = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Capacity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wards_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Wards_TenantId_IsActive",
                table: "Wards",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Wards_TenantId_WardType",
                table: "Wards",
                columns: new[] { "TenantId", "WardType" });

            migrationBuilder.CreateIndex(
                name: "IX_Wards_TenantId_Code",
                table: "Wards",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            // Beds
            migrationBuilder.CreateTable(
                name: "Beds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    WardId = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Beds_Wards_WardId",
                        column: x => x.WardId,
                        principalTable: "Wards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Beds_TenantId_Status",
                table: "Beds",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Beds_WardId_Number",
                table: "Beds",
                columns: new[] { "WardId", "Number" },
                unique: true);

            // Admissions
            migrationBuilder.CreateTable(
                name: "Admissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    PatientId = table.Column<int>(type: "integer", nullable: false),
                    BedId = table.Column<int>(type: "integer", nullable: false),
                    AdmissionType = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    AdmissionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedDischargeDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttendingDoctorId = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DiagnosisOnAdmission = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DischargeDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DischargeNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Admissions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Admissions_Beds_BedId",
                        column: x => x.BedId,
                        principalTable: "Beds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Admissions_DoctorProfiles_AttendingDoctorId",
                        column: x => x.AttendingDoctorId,
                        principalTable: "DoctorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_TenantId_Status",
                table: "Admissions",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_TenantId_PatientId_Status",
                table: "Admissions",
                columns: new[] { "TenantId", "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_TenantId_BedId",
                table: "Admissions",
                columns: new[] { "TenantId", "BedId" });

            // AdmissionTransfers
            migrationBuilder.CreateTable(
                name: "AdmissionTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdmissionId = table.Column<int>(type: "integer", nullable: false),
                    FromBedId = table.Column<int>(type: "integer", nullable: false),
                    ToBedId = table.Column<int>(type: "integer", nullable: false),
                    TransferredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TransferredByUserId = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmissionTransfers_Admissions_AdmissionId",
                        column: x => x.AdmissionId,
                        principalTable: "Admissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdmissionTransfers_Beds_FromBedId",
                        column: x => x.FromBedId,
                        principalTable: "Beds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionTransfers_Beds_ToBedId",
                        column: x => x.ToBedId,
                        principalTable: "Beds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionTransfers_AdmissionId_TransferredAt",
                table: "AdmissionTransfers",
                columns: new[] { "AdmissionId", "TransferredAt" });

            // DischargeSummaries
            migrationBuilder.CreateTable(
                name: "DischargeSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdmissionId = table.Column<int>(type: "integer", nullable: false),
                    DischargingDoctorId = table.Column<int>(type: "integer", nullable: false),
                    DischargeDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConditionAtDischarge = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DischargeInstructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FollowUpDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MedicationsOnDischarge = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DischargeSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DischargeSummaries_Admissions_AdmissionId",
                        column: x => x.AdmissionId,
                        principalTable: "Admissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DischargeSummaries_DoctorProfiles_DischargingDoctorId",
                        column: x => x.DischargingDoctorId,
                        principalTable: "DoctorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DischargeSummaries_AdmissionId",
                table: "DischargeSummaries",
                column: "AdmissionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DischargeSummaries");
            migrationBuilder.DropTable(name: "AdmissionTransfers");
            migrationBuilder.DropTable(name: "Admissions");
            migrationBuilder.DropTable(name: "Beds");
            migrationBuilder.DropTable(name: "Wards");
        }
    }
}
