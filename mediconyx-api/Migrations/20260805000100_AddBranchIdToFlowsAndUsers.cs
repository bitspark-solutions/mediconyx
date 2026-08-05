using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using mediconyx_api.Data;

#nullable disable

namespace mediconyx_api.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260805000100_AddBranchIdToFlowsAndUsers")]
    public partial class AddBranchIdToFlowsAndUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nullable first so existing rows can be backfilled
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Appointments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Admissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrimaryBranchId",
                table: "Users",
                type: "integer",
                nullable: true);

            // Backfill: admissions inherit their bed's branch; appointments/invoices
            // have no branch signal in existing data, so they go to the tenant's Main Branch.
            migrationBuilder.Sql(
                """
                UPDATE "Admissions" a SET "BranchId" = b."BranchId"
                FROM "Beds" b
                WHERE b."Id" = a."BedId" AND a."BranchId" IS NULL;

                UPDATE "Admissions" a SET "BranchId" = br."Id"
                FROM "Branches" br
                WHERE br."TenantId" = a."TenantId" AND br."IsMain" AND a."BranchId" IS NULL;

                UPDATE "Appointments" a SET "BranchId" = br."Id"
                FROM "Branches" br
                WHERE br."TenantId" = a."TenantId" AND br."IsMain" AND a."BranchId" IS NULL;

                UPDATE "Invoices" i SET "BranchId" = br."Id"
                FROM "Branches" br
                WHERE br."TenantId" = i."TenantId" AND br."IsMain" AND i."BranchId" IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "Appointments",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "Admissions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BranchId",
                table: "Invoices",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TenantId_BranchId",
                table: "Appointments",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_TenantId_BranchId",
                table: "Admissions",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_BranchId",
                table: "Invoices",
                columns: new[] { "TenantId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_PrimaryBranchId",
                table: "Users",
                column: "PrimaryBranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Branches_BranchId",
                table: "Appointments",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Admissions_Branches_BranchId",
                table: "Admissions",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Branches_BranchId",
                table: "Invoices",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Branches_PrimaryBranchId",
                table: "Users",
                column: "PrimaryBranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Appointments_Branches_BranchId", table: "Appointments");
            migrationBuilder.DropForeignKey(name: "FK_Admissions_Branches_BranchId", table: "Admissions");
            migrationBuilder.DropForeignKey(name: "FK_Invoices_Branches_BranchId", table: "Invoices");
            migrationBuilder.DropForeignKey(name: "FK_Users_Branches_PrimaryBranchId", table: "Users");
            migrationBuilder.DropIndex(name: "IX_Appointments_TenantId_BranchId", table: "Appointments");
            migrationBuilder.DropIndex(name: "IX_Admissions_TenantId_BranchId", table: "Admissions");
            migrationBuilder.DropIndex(name: "IX_Invoices_TenantId_BranchId", table: "Invoices");
            migrationBuilder.DropIndex(name: "IX_Users_PrimaryBranchId", table: "Users");
            migrationBuilder.DropColumn(name: "BranchId", table: "Appointments");
            migrationBuilder.DropColumn(name: "BranchId", table: "Admissions");
            migrationBuilder.DropColumn(name: "BranchId", table: "Invoices");
            migrationBuilder.DropColumn(name: "PrimaryBranchId", table: "Users");
        }
    }
}
