using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceQR.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Lecturer",
                table: "Lecturer");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EmailSignInToken",
                table: "EmailSignInToken");

            migrationBuilder.RenameTable(
                name: "Lecturer",
                newName: "Lecturers");

            migrationBuilder.RenameTable(
                name: "EmailSignInToken",
                newName: "EmailSignInTokens");

            migrationBuilder.RenameIndex(
                name: "IX_EmailSignInToken_StaffEmail_TokenHash",
                table: "EmailSignInTokens",
                newName: "IX_EmailSignInTokens_StaffEmail_TokenHash");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Lecturers",
                table: "Lecturers",
                column: "StaffEmail");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EmailSignInTokens",
                table: "EmailSignInTokens",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Lecturers",
                table: "Lecturers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EmailSignInTokens",
                table: "EmailSignInTokens");

            migrationBuilder.RenameTable(
                name: "Lecturers",
                newName: "Lecturer");

            migrationBuilder.RenameTable(
                name: "EmailSignInTokens",
                newName: "EmailSignInToken");

            migrationBuilder.RenameIndex(
                name: "IX_EmailSignInTokens_StaffEmail_TokenHash",
                table: "EmailSignInToken",
                newName: "IX_EmailSignInToken_StaffEmail_TokenHash");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Lecturer",
                table: "Lecturer",
                column: "StaffEmail");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EmailSignInToken",
                table: "EmailSignInToken",
                column: "Id");
        }
    }
}
