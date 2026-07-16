using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDisciplinesAndMemberRanks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationMembers_OrganizationRanks_RankId",
                table: "OrganizationMembers");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationMembers_RankId",
                table: "OrganizationMembers");

            migrationBuilder.DropColumn(
                name: "RankId",
                table: "OrganizationMembers");

            migrationBuilder.AddColumn<Guid>(
                name: "DisciplineId",
                table: "OrganizationRanks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Disciplines",
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
                    table.PrimaryKey("PK_Disciplines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Disciplines_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemberRanks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisciplineId = table.Column<Guid>(type: "uuid", nullable: false),
                    RankId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberRanks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberRanks_Disciplines_DisciplineId",
                        column: x => x.DisciplineId,
                        principalTable: "Disciplines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberRanks_OrganizationMembers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "OrganizationMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberRanks_OrganizationRanks_RankId",
                        column: x => x.RankId,
                        principalTable: "OrganizationRanks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRanks_DisciplineId",
                table: "OrganizationRanks",
                column: "DisciplineId");

            migrationBuilder.CreateIndex(
                name: "IX_Disciplines_OrganizationId",
                table: "Disciplines",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberRanks_DisciplineId",
                table: "MemberRanks",
                column: "DisciplineId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberRanks_MemberId",
                table: "MemberRanks",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberRanks_MemberId_DisciplineId",
                table: "MemberRanks",
                columns: new[] { "MemberId", "DisciplineId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_MemberRanks_RankId",
                table: "MemberRanks",
                column: "RankId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationRanks_Disciplines_DisciplineId",
                table: "OrganizationRanks",
                column: "DisciplineId",
                principalTable: "Disciplines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationRanks_Disciplines_DisciplineId",
                table: "OrganizationRanks");

            migrationBuilder.DropTable(
                name: "MemberRanks");

            migrationBuilder.DropTable(
                name: "Disciplines");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationRanks_DisciplineId",
                table: "OrganizationRanks");

            migrationBuilder.DropColumn(
                name: "DisciplineId",
                table: "OrganizationRanks");

            migrationBuilder.AddColumn<Guid>(
                name: "RankId",
                table: "OrganizationMembers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_RankId",
                table: "OrganizationMembers",
                column: "RankId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationMembers_OrganizationRanks_RankId",
                table: "OrganizationMembers",
                column: "RankId",
                principalTable: "OrganizationRanks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
