using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileCode",
                table: "Users",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ProfileCode",
                table: "Users",
                column: "ProfileCode",
                unique: true,
                filter: "\"IsDeleted\" = false AND \"ProfileCode\" != ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_ProfileCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProfileCode",
                table: "Users");
        }
    }
}
