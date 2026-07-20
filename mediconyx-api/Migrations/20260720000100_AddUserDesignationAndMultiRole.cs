using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace mediconyx_api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDesignationAndMultiRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // User: Designation + PrimaryDepartmentId
            migrationBuilder.AddColumn<string>(
                name: "Designation",
                table: "Users",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrimaryDepartmentId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PrimaryDepartmentId",
                table: "Users",
                column: "PrimaryDepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Departments_PrimaryDepartmentId",
                table: "Users",
                column: "PrimaryDepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Department: HeadDoctorId
            migrationBuilder.AddColumn<int>(
                name: "HeadDoctorId",
                table: "Departments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_HeadDoctorId",
                table: "Departments",
                column: "HeadDoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_DoctorProfiles_HeadDoctorId",
                table: "Departments",
                column: "HeadDoctorId",
                principalTable: "DoctorProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // UserRoleAssignments: extra roles per user
            migrationBuilder.CreateTable(
                name: "UserRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GrantedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoleAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_UserId_Role",
                table: "UserRoleAssignments",
                columns: new[] { "UserId", "Role" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "UserRoleAssignments");

            migrationBuilder.DropForeignKey(name: "FK_Departments_DoctorProfiles_HeadDoctorId", table: "Departments");
            migrationBuilder.DropIndex(name: "IX_Departments_HeadDoctorId", table: "Departments");
            migrationBuilder.DropColumn(name: "HeadDoctorId", table: "Departments");

            migrationBuilder.DropForeignKey(name: "FK_Users_Departments_PrimaryDepartmentId", table: "Users");
            migrationBuilder.DropIndex(name: "IX_Users_PrimaryDepartmentId", table: "Users");
            migrationBuilder.DropColumn(name: "PrimaryDepartmentId", table: "Users");
            migrationBuilder.DropColumn(name: "Designation", table: "Users");
        }
    }
}
