using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceEventSeriesWithSeriesGroupId : Migration
    {
        private const string ArchiveTableName = "_Archived_EventSeries_20260717";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Usuń FK Events → EventSeries (kolumna zostaje, wartości GUID zachowane)
            migrationBuilder.DropForeignKey(
                name: "FK_Events_EventSeries_EventSeriesId",
                table: "Events");

            // 2. Przemianuj kolumnę — wartości EventSeriesId stają się SeriesGroupId 1:1
            migrationBuilder.RenameColumn(
                name: "EventSeriesId",
                table: "Events",
                newName: "SeriesGroupId");

            migrationBuilder.RenameIndex(
                name: "IX_Events_EventSeriesId",
                table: "Events",
                newName: "IX_Events_SeriesGroupId");

            // 3. Archiwizuj tabelę EventSeries (nie hard-delete — możliwy odczyt historyczny)
            migrationBuilder.RenameTable(
                name: "EventSeries",
                newName: ArchiveTableName);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: ArchiveTableName,
                newName: "EventSeries");

            migrationBuilder.RenameColumn(
                name: "SeriesGroupId",
                table: "Events",
                newName: "EventSeriesId");

            migrationBuilder.RenameIndex(
                name: "IX_Events_SeriesGroupId",
                table: "Events",
                newName: "IX_Events_EventSeriesId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_EventSeries_EventSeriesId",
                table: "Events",
                column: "EventSeriesId",
                principalTable: "EventSeries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
