using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shatbly.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddIdCardPhotoToWorkerProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdCardPhotoPath",
                table: "WorkerProfiles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdCardPhotoPath",
                table: "WorkerProfiles");
        }
    }
}
