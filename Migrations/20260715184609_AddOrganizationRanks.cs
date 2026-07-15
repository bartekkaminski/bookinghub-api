using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationRanks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RankId",
                table: "OrganizationMembers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrganizationRanks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationRanks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationRanks_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_RankId",
                table: "OrganizationMembers",
                column: "RankId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRanks_OrganizationId",
                table: "OrganizationRanks",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationMembers_OrganizationRanks_RankId",
                table: "OrganizationMembers",
                column: "RankId",
                principalTable: "OrganizationRanks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationMembers_OrganizationRanks_RankId",
                table: "OrganizationMembers");

            migrationBuilder.DropTable(
                name: "OrganizationRanks");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationMembers_RankId",
                table: "OrganizationMembers");

            migrationBuilder.DropColumn(
                name: "RankId",
                table: "OrganizationMembers");
        }
    }
}
