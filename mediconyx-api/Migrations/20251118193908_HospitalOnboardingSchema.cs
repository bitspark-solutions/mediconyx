using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace mediconyx_api.Migrations
{
    /// <inheritdoc />
    public partial class HospitalOnboardingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoRenewal",
                table: "SalesRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ContractDuration",
                table: "SalesRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractStartDate",
                table: "SalesRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountApplied",
                table: "SalesRequests",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "SalesRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentTerms",
                table: "SalesRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecialTerms",
                table: "SalesRequests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "SalesRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserLicenses",
                table: "SalesRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DocumentType",
                table: "SalesRequestAttachments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMandatory",
                table: "SalesRequestAttachments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "HospitalProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SalesRequestId = table.Column<int>(type: "integer", nullable: false),
                    HospitalLegalName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LicenseNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TaxId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    HospitalType = table.Column<int>(type: "integer", nullable: false),
                    BedCapacity = table.Column<int>(type: "integer", nullable: false),
                    AddressLine1 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AddressLine2 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StateProvince = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrimaryContactName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PrimaryContactEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PrimaryContactPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SecondaryContactName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SecondaryContactEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SecondaryContactPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EstablishedYear = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HospitalProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HospitalProfiles_SalesRequests_SalesRequestId",
                        column: x => x.SalesRequestId,
                        principalTable: "SalesRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModuleSelections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SalesRequestId = table.Column<int>(type: "integer", nullable: false),
                    OpdManagement = table.Column<bool>(type: "boolean", nullable: false),
                    IpdManagement = table.Column<bool>(type: "boolean", nullable: false),
                    EmergencyServices = table.Column<bool>(type: "boolean", nullable: false),
                    PharmacyManagement = table.Column<bool>(type: "boolean", nullable: false),
                    Laboratory = table.Column<bool>(type: "boolean", nullable: false),
                    RadiologyImaging = table.Column<bool>(type: "boolean", nullable: false),
                    BillingInvoicing = table.Column<bool>(type: "boolean", nullable: false),
                    InventoryManagement = table.Column<bool>(type: "boolean", nullable: false),
                    HrPayroll = table.Column<bool>(type: "boolean", nullable: false),
                    AppointmentScheduling = table.Column<bool>(type: "boolean", nullable: false),
                    Telemedicine = table.Column<bool>(type: "boolean", nullable: false),
                    AnalyticsReporting = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleSelections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuleSelections_SalesRequests_SalesRequestId",
                        column: x => x.SalesRequestId,
                        principalTable: "SalesRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechnicalRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SalesRequestId = table.Column<int>(type: "integer", nullable: false),
                    ExpectedUserCount = table.Column<int>(type: "integer", nullable: false),
                    ConcurrentUsers = table.Column<int>(type: "integer", nullable: false),
                    DepartmentCount = table.Column<int>(type: "integer", nullable: false),
                    DepartmentsList = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ExistingEmrSystem = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IntegrationRequirements = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DataMigrationRequired = table.Column<bool>(type: "boolean", nullable: false),
                    EstimatedPatientRecords = table.Column<int>(type: "integer", nullable: true),
                    PreferredGoLiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ItContactName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ItContactEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ItContactPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicalRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnicalRequirements_SalesRequests_SalesRequestId",
                        column: x => x.SalesRequestId,
                        principalTable: "SalesRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesRequests_TenantId",
                table: "SalesRequests",
                column: "TenantId",
                filter: "\"TenantId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HospitalProfiles_LicenseNumber",
                table: "HospitalProfiles",
                column: "LicenseNumber");

            migrationBuilder.CreateIndex(
                name: "IX_HospitalProfiles_SalesRequestId",
                table: "HospitalProfiles",
                column: "SalesRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HospitalProfiles_TaxId",
                table: "HospitalProfiles",
                column: "TaxId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleSelections_SalesRequestId",
                table: "ModuleSelections",
                column: "SalesRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalRequirements_SalesRequestId",
                table: "TechnicalRequirements",
                column: "SalesRequestId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesRequests_Tenants_TenantId",
                table: "SalesRequests",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesRequests_Tenants_TenantId",
                table: "SalesRequests");

            migrationBuilder.DropTable(
                name: "HospitalProfiles");

            migrationBuilder.DropTable(
                name: "ModuleSelections");

            migrationBuilder.DropTable(
                name: "TechnicalRequirements");

            migrationBuilder.DropIndex(
                name: "IX_SalesRequests_TenantId",
                table: "SalesRequests");

            migrationBuilder.DropColumn(
                name: "AutoRenewal",
                table: "SalesRequests");

            migrationBuilder.DropColumn(
                name: "ContractDuration",
                table: "SalesRequests");

            migrationBuilder.DropColumn(
                name: "ContractStartDate",
                table: "SalesRequests");

            migrationBuilder.DropColumn(
                name: "DiscountApplied",
                table: "SalesRequests");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "SalesRequests");

            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                table: "SalesRequests");

            migrationBuilder.DropColumn(
                name: "SpecialTerms",
                table: "SalesRequests");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SalesRequests");

            migrationBuilder.DropColumn(
                name: "UserLicenses",
                table: "SalesRequests");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "SalesRequestAttachments");

            migrationBuilder.DropColumn(
                name: "IsMandatory",
                table: "SalesRequestAttachments");
        }
    }
}
