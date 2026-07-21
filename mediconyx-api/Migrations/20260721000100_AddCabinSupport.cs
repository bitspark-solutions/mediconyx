using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using mediconyx_api.Data;

#nullable disable

namespace mediconyx_api.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260721000100_AddCabinSupport")]
    public partial class AddCabinSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BedKind: 0 = Bed (default), 1 = Cabin
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Beds",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Cabin category when Kind = Cabin (see CabinTypes: Shared/Standard/Deluxe/VIP/VVIP/Suite)
            migrationBuilder.AddColumn<string>(
                name: "CabinType",
                table: "Beds",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // JSON array of amenity names, e.g. ["AC","Attached Bathroom","TV"]
            migrationBuilder.AddColumn<string>(
                name: "Facilities",
                table: "Beds",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Beds_TenantId_Kind",
                table: "Beds",
                columns: new[] { "TenantId", "Kind" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Beds_TenantId_Kind",
                table: "Beds");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Beds");

            migrationBuilder.DropColumn(
                name: "CabinType",
                table: "Beds");

            migrationBuilder.DropColumn(
                name: "Facilities",
                table: "Beds");
        }
    }
}
