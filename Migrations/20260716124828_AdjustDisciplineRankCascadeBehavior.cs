using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdjustDisciplineRankCascadeBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberRanks_OrganizationRanks_RankId",
                table: "MemberRanks");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationRanks_Disciplines_DisciplineId",
                table: "OrganizationRanks");

            migrationBuilder.AddForeignKey(
                name: "FK_MemberRanks_OrganizationRanks_RankId",
                table: "MemberRanks",
                column: "RankId",
                principalTable: "OrganizationRanks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationRanks_Disciplines_DisciplineId",
                table: "OrganizationRanks",
                column: "DisciplineId",
                principalTable: "Disciplines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberRanks_OrganizationRanks_RankId",
                table: "MemberRanks");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationRanks_Disciplines_DisciplineId",
                table: "OrganizationRanks");

            migrationBuilder.AddForeignKey(
                name: "FK_MemberRanks_OrganizationRanks_RankId",
                table: "MemberRanks",
                column: "RankId",
                principalTable: "OrganizationRanks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationRanks_Disciplines_DisciplineId",
                table: "OrganizationRanks",
                column: "DisciplineId",
                principalTable: "Disciplines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
