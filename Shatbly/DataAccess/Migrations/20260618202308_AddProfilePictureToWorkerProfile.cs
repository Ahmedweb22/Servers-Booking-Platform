using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shatbly.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePictureToWorkerProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "ServiceCategories");

            migrationBuilder.AddColumn<string>(
                name: "ProfilePicturePath",
                table: "WorkerProfiles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePicturePath",
                table: "WorkerProfiles");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ServiceCategories",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
