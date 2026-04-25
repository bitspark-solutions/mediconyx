using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mediconyx_api.Migrations
{
    /// <inheritdoc />
    public partial class TenantMissingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessRegistrationNumber",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatingHours",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                table: "Tenants",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "BusinessRegistrationNumber", table: "Tenants");
            migrationBuilder.DropColumn(name: "Description", table: "Tenants");
            migrationBuilder.DropColumn(name: "LogoUrl", table: "Tenants");
            migrationBuilder.DropColumn(name: "OperatingHours", table: "Tenants");
            migrationBuilder.DropColumn(name: "TimeZone", table: "Tenants");
        }
    }
}
