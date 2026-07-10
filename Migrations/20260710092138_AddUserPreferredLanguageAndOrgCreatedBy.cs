using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPreferredLanguageAndOrgCreatedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredLanguage",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByPersonId",
                table: "Organizations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_CreatedByPersonId",
                table: "Organizations",
                column: "CreatedByPersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_Persons_CreatedByPersonId",
                table: "Organizations",
                column: "CreatedByPersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_Persons_CreatedByPersonId",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_CreatedByPersonId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "PreferredLanguage",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedByPersonId",
                table: "Organizations");
        }
    }
}
