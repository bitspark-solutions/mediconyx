using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mediconyx_api.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorProfilePublicFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "DoctorProfiles",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "DoctorProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearsOfExperience",
                table: "DoctorProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LanguagesJson",
                table: "DoctorProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualificationsJson",
                table: "DoctorProfiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "QualificationsJson", table: "DoctorProfiles");
            migrationBuilder.DropColumn(name: "LanguagesJson", table: "DoctorProfiles");
            migrationBuilder.DropColumn(name: "YearsOfExperience", table: "DoctorProfiles");
            migrationBuilder.DropColumn(name: "PhotoUrl", table: "DoctorProfiles");
            migrationBuilder.DropColumn(name: "Bio", table: "DoctorProfiles");
        }
    }
}
