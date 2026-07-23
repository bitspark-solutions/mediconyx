using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using mediconyx_api.Data;

#nullable disable

namespace mediconyx_api.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260721000300_AddBranchIdToWardsBeds")]
    public partial class AddBranchIdToWardsBeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nullable first so existing rows can be backfilled
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Wards",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Beds",
                type: "integer",
                nullable: true);

            // Backfill: point every existing ward/bed at its tenant's Main Branch
            migrationBuilder.Sql(
                """
                UPDATE "Wards" w SET "BranchId" = b."Id"
                FROM "Branches" b
                WHERE b."TenantId" = w."TenantId" AND b."IsMain" AND w."BranchId" IS NULL;

                UPDATE "Beds" w SET "BranchId" = b."Id"
                FROM "Branches" b
                WHERE b."TenantId" = w."TenantId" AND b."IsMain" AND w."BranchId" IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "Wards",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "Beds",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            // Ward codes: unique per tenant → unique per (tenant, branch)
            migrationBuilder.DropIndex(
                name: "IX_Wards_TenantId_Code",
                table: "Wards");

            migrationBuilder.CreateIndex(
                name: "IX_Wards_TenantId_BranchId",
                table: "Wards",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_Wards_TenantId_BranchId_Code",
                table: "Wards",
                columns: new[] { "TenantId", "BranchId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Beds_TenantId_BranchId",
                table: "Beds",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Wards_Branches_BranchId",
                table: "Wards",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Beds_Branches_BranchId",
                table: "Beds",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Wards_Branches_BranchId", table: "Wards");
            migrationBuilder.DropForeignKey(name: "FK_Beds_Branches_BranchId", table: "Beds");
            migrationBuilder.DropIndex(name: "IX_Wards_TenantId_BranchId", table: "Wards");
            migrationBuilder.DropIndex(name: "IX_Wards_TenantId_BranchId_Code", table: "Wards");
            migrationBuilder.DropIndex(name: "IX_Beds_TenantId_BranchId", table: "Beds");

            migrationBuilder.CreateIndex(
                name: "IX_Wards_TenantId_Code",
                table: "Wards",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.DropColumn(name: "BranchId", table: "Wards");
            migrationBuilder.DropColumn(name: "BranchId", table: "Beds");
        }
    }
}
