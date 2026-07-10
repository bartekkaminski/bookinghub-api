using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class BackfillOrganizationCreatedByPersonId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill CreatedByPersonId dla organizacji stworzonych przed wprowadzeniem tej kolumny.
            // Za twórcę uznajemy najstarszego aktywnego Admina w organizacji.
            migrationBuilder.Sql("""
                UPDATE "Organizations" o
                SET "CreatedByPersonId" = (
                    SELECT om."PersonId"
                    FROM "OrganizationMembers" om
                    JOIN "OrganizationMemberRoles" omr ON omr."OrganizationMemberId" = om."Id"
                    WHERE om."OrganizationId" = o."Id"
                      AND omr."Role" = 'Admin'
                      AND om."IsDeleted" = false
                      AND omr."IsDeleted" = false
                    ORDER BY om."CreatedAt"
                    LIMIT 1
                )
                WHERE o."CreatedByPersonId" IS NULL
                  AND o."IsDeleted" = false;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Organizations"
                SET "CreatedByPersonId" = NULL
                WHERE "CreatedByPersonId" IS NOT NULL;
                """);
        }
    }
}
