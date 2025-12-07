using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mediconyx_api.Migrations
{
    /// <inheritdoc />
    public partial class UserManagementIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ModuleAnalyticsEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ModuleAppointmentEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ModuleBillingEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ModuleEmergencyEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ModuleHrPayrollEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ModuleInventoryEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ModuleIpdEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ModuleLaboratoryEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ModuleOpdEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ModulePharmacyEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ModuleRadiologyEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ModuleTelemedicineEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Subdomain",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionStartDate",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserLicenses",
                table: "Tenants",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                table: "Users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId",
                table: "Users",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_IsActive",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Role",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ModuleAnalyticsEnabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ModuleAppointmentEnabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ModuleBillingEnabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ModuleEmergencyEnabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ModuleHrPayrollEnabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ModuleInventoryEnabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ModuleIpdEnabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ModuleLaboratoryEnabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ModuleOpdEnabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ModulePharmacyEnabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ModuleRadiologyEnabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ModuleTelemedicineEnabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Subdomain",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SubscriptionStartDate",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "UserLicenses",
                table: "Tenants");
        }
    }
}
