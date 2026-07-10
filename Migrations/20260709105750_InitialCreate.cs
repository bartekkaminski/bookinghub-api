using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AuthProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    PhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Persons_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Groups_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Groups_Persons_CreatedByPersonId",
                        column: x => x.CreatedByPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Locations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Locations_Persons_CreatedByPersonId",
                        column: x => x.CreatedByPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Priority = table.Column<int>(type: "integer", nullable: true),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationMembers_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationMembers_Persons_CreatedByPersonId",
                        column: x => x.CreatedByPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrganizationMembers_Persons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParentChildRelations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentChildRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParentChildRelations_Persons_ChildPersonId",
                        column: x => x.ChildPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParentChildRelations_Persons_CreatedByPersonId",
                        column: x => x.CreatedByPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ParentChildRelations_Persons_ParentPersonId",
                        column: x => x.ParentPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Priority = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Teams_Persons_CreatedByPersonId",
                        column: x => x.CreatedByPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GroupCostRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    MonthlyCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupCostRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupCostRates_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupCostRates_Persons_CreatedByPersonId",
                        column: x => x.CreatedByPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EventSeries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RecurrenceRule = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DefaultGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    DefaultEventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "GroupTraining"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSeries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventSeries_Groups_DefaultGroupId",
                        column: x => x.DefaultGroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventSeries_Locations_DefaultLocationId",
                        column: x => x.DefaultLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventSeries_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventSeries_Persons_CreatedByPersonId",
                        column: x => x.CreatedByPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GroupMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupMembers_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupMembers_OrganizationMembers_OrganizationMemberId",
                        column: x => x.OrganizationMemberId,
                        principalTable: "OrganizationMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupMembers_Persons_CreatedByPersonId",
                        column: x => x.CreatedByPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MemberAvailabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TimeFrom = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    TimeTo = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberAvailabilities_OrganizationMembers_OrganizationMember~",
                        column: x => x.OrganizationMemberId,
                        principalTable: "OrganizationMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberAvailabilities_Persons_CreatedByPersonId",
                        column: x => x.CreatedByPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationMemberRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationMemberRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationMemberRoles_OrganizationMembers_OrganizationMem~",
                        column: x => x.OrganizationMemberId,
                        principalTable: "OrganizationMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParticipantTrainers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainerMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantTrainers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParticipantTrainers_OrganizationMembers_ParticipantMemberId",
                        column: x => x.ParticipantMemberId,
                        principalTable: "OrganizationMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParticipantTrainers_OrganizationMembers_TrainerMemberId",
                        column: x => x.TrainerMemberId,
                        principalTable: "OrganizationMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParticipantTrainers_Persons_CreatedByPersonId",
                        column: x => x.CreatedByPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TrainerSessionRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainerMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    RatePerHour = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainerSessionRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainerSessionRates_OrganizationMembers_TrainerMemberId",
                        column: x => x.TrainerMemberId,
                        principalTable: "OrganizationMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainerSessionRates_Persons_CreatedByPersonId",
                        column: x => x.CreatedByPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TeamGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamGroups_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamGroups_Persons_CreatedByPersonId",
                        column: x => x.CreatedByPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TeamGroups_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMembers_OrganizationMembers_OrganizationMemberId",
                        column: x => x.OrganizationMemberId,
                        principalTable: "OrganizationMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Persons_CreatedByPersonId",
                        column: x => x.CreatedByPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamTrainers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainerMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamTrainers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamTrainers_OrganizationMembers_TrainerMemberId",
                        column: x => x.TrainerMemberId,
                        principalTable: "OrganizationMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamTrainers_Persons_CreatedByPersonId",
                        column: x => x.CreatedByPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TeamTrainers_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventSeriesId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "GroupTraining"),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Scheduled"),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Events_EventSeries_EventSeriesId",
                        column: x => x.EventSeriesId,
                        principalTable: "EventSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Events_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Events_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Events_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Events_Persons_CreatedByPersonId",
                        column: x => x.CreatedByPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EventEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Enrolled"),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventEnrollments_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventEnrollments_OrganizationMembers_OrganizationMemberId",
                        column: x => x.OrganizationMemberId,
                        principalTable: "OrganizationMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventEnrollments_Persons_CreatedByPersonId",
                        column: x => x.CreatedByPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EventTeamEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Enrolled"),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTeamEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventTeamEnrollments_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventTeamEnrollments_Persons_CreatedByPersonId",
                        column: x => x.CreatedByPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventTeamEnrollments_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventTrainers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTrainers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventTrainers_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventTrainers_OrganizationMembers_OrganizationMemberId",
                        column: x => x.OrganizationMemberId,
                        principalTable: "OrganizationMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventTrainers_Persons_CreatedByPersonId",
                        column: x => x.CreatedByPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsAutomatic = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RelatedEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_Events_RelatedEventId",
                        column: x => x.RelatedEventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Messages_Messages_ParentMessageId",
                        column: x => x.ParentMessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Messages_OrganizationMembers_SenderMemberId",
                        column: x => x.SenderMemberId,
                        principalTable: "OrganizationMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Messages_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Messages_Persons_CreatedByPersonId",
                        column: x => x.CreatedByPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CancellationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventEnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    ReviewedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CancellationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CancellationRequests_EventEnrollments_EventEnrollmentId",
                        column: x => x.EventEnrollmentId,
                        principalTable: "EventEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CancellationRequests_OrganizationMembers_RequestedByMemberId",
                        column: x => x.RequestedByMemberId,
                        principalTable: "OrganizationMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CancellationRequests_Persons_CreatedByPersonId",
                        column: x => x.CreatedByPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CancellationRequests_Persons_ReviewedByPersonId",
                        column: x => x.ReviewedByPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MessageRecipients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageRecipients_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessageRecipients_OrganizationMembers_RecipientMemberId",
                        column: x => x.RecipientMemberId,
                        principalTable: "OrganizationMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CancellationRequests_CreatedByPersonId",
                table: "CancellationRequests",
                column: "CreatedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationRequests_EventEnrollmentId",
                table: "CancellationRequests",
                column: "EventEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationRequests_EventEnrollmentId_Status",
                table: "CancellationRequests",
                columns: new[] { "EventEnrollmentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CancellationRequests_RequestedByMemberId",
                table: "CancellationRequests",
                column: "RequestedByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationRequests_ReviewedByPersonId",
                table: "CancellationRequests",
                column: "ReviewedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_EventEnrollments_CreatedByPersonId",
                table: "EventEnrollments",
                column: "CreatedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_EventEnrollments_EventId",
                table: "EventEnrollments",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventEnrollments_EventId_OrganizationMemberId",
                table: "EventEnrollments",
                columns: new[] { "EventId", "OrganizationMemberId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_EventEnrollments_OrganizationMemberId",
                table: "EventEnrollments",
                column: "OrganizationMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_CreatedByPersonId",
                table: "Events",
                column: "CreatedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventSeriesId",
                table: "Events",
                column: "EventSeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_GroupId",
                table: "Events",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_LocationId",
                table: "Events",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_OrganizationId",
                table: "Events",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_StartTime",
                table: "Events",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_EventSeries_CreatedByPersonId",
                table: "EventSeries",
                column: "CreatedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSeries_DefaultGroupId",
                table: "EventSeries",
                column: "DefaultGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSeries_DefaultLocationId",
                table: "EventSeries",
                column: "DefaultLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_EventSeries_OrganizationId",
                table: "EventSeries",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTeamEnrollments_CreatedByPersonId",
                table: "EventTeamEnrollments",
                column: "CreatedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTeamEnrollments_EventId",
                table: "EventTeamEnrollments",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTeamEnrollments_EventId_TeamId",
                table: "EventTeamEnrollments",
                columns: new[] { "EventId", "TeamId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_EventTeamEnrollments_TeamId",
                table: "EventTeamEnrollments",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTrainers_CreatedByPersonId",
                table: "EventTrainers",
                column: "CreatedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTrainers_EventId",
                table: "EventTrainers",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTrainers_EventId_OrganizationMemberId",
                table: "EventTrainers",
                columns: new[] { "EventId", "OrganizationMemberId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_EventTrainers_OrganizationMemberId",
                table: "EventTrainers",
                column: "OrganizationMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupCostRates_CreatedByPersonId",
                table: "GroupCostRates",
                column: "CreatedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupCostRates_GroupId",
                table: "GroupCostRates",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupCostRates_GroupId_ValidFrom",
                table: "GroupCostRates",
                columns: new[] { "GroupId", "ValidFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_CreatedByPersonId",
                table: "GroupMembers",
                column: "CreatedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_GroupId",
                table: "GroupMembers",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_GroupId_OrganizationMemberId",
                table: "GroupMembers",
                columns: new[] { "GroupId", "OrganizationMemberId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_OrganizationMemberId",
                table: "GroupMembers",
                column: "OrganizationMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_CreatedByPersonId",
                table: "Groups",
                column: "CreatedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_OrganizationId",
                table: "Groups",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_CreatedByPersonId",
                table: "Locations",
                column: "CreatedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_OrganizationId",
                table: "Locations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberAvailabilities_CreatedByPersonId",
                table: "MemberAvailabilities",
                column: "CreatedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberAvailabilities_OrganizationMemberId",
                table: "MemberAvailabilities",
                column: "OrganizationMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberAvailabilities_OrganizationMemberId_DayOfWeek",
                table: "MemberAvailabilities",
                columns: new[] { "OrganizationMemberId", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_MessageRecipients_MessageId_RecipientMemberId",
                table: "MessageRecipients",
                columns: new[] { "MessageId", "RecipientMemberId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_MessageRecipients_RecipientMemberId",
                table: "MessageRecipients",
                column: "RecipientMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageRecipients_RecipientMemberId_IsRead",
                table: "MessageRecipients",
                columns: new[] { "RecipientMemberId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_CreatedByPersonId",
                table: "Messages",
                column: "CreatedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_OrganizationId",
                table: "Messages",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ParentMessageId",
                table: "Messages",
                column: "ParentMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_RelatedEventId",
                table: "Messages",
                column: "RelatedEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderMemberId",
                table: "Messages",
                column: "SenderMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderMemberId_SentAt",
                table: "Messages",
                columns: new[] { "SenderMemberId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberRoles_OrganizationMemberId",
                table: "OrganizationMemberRoles",
                column: "OrganizationMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberRoles_OrganizationMemberId_Role",
                table: "OrganizationMemberRoles",
                columns: new[] { "OrganizationMemberId", "Role" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_CreatedByPersonId",
                table: "OrganizationMembers",
                column: "CreatedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_OrganizationId",
                table: "OrganizationMembers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_OrganizationId_PersonId",
                table: "OrganizationMembers",
                columns: new[] { "OrganizationId", "PersonId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_PersonId",
                table: "OrganizationMembers",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentChildRelations_ChildPersonId",
                table: "ParentChildRelations",
                column: "ChildPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentChildRelations_CreatedByPersonId",
                table: "ParentChildRelations",
                column: "CreatedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentChildRelations_ParentPersonId",
                table: "ParentChildRelations",
                column: "ParentPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentChildRelations_ParentPersonId_ChildPersonId",
                table: "ParentChildRelations",
                columns: new[] { "ParentPersonId", "ChildPersonId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantTrainers_CreatedByPersonId",
                table: "ParticipantTrainers",
                column: "CreatedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantTrainers_ParticipantMemberId",
                table: "ParticipantTrainers",
                column: "ParticipantMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantTrainers_ParticipantMemberId_TrainerMemberId",
                table: "ParticipantTrainers",
                columns: new[] { "ParticipantMemberId", "TrainerMemberId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantTrainers_TrainerMemberId",
                table: "ParticipantTrainers",
                column: "TrainerMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_UserId",
                table: "Persons",
                column: "UserId",
                unique: true,
                filter: "\"UserId\" IS NOT NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_TeamGroups_CreatedByPersonId",
                table: "TeamGroups",
                column: "CreatedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamGroups_GroupId",
                table: "TeamGroups",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamGroups_TeamId",
                table: "TeamGroups",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamGroups_TeamId_GroupId",
                table: "TeamGroups",
                columns: new[] { "TeamId", "GroupId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_CreatedByPersonId",
                table: "TeamMembers",
                column: "CreatedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_OrganizationMemberId",
                table: "TeamMembers",
                column: "OrganizationMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_TeamId",
                table: "TeamMembers",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_TeamId_OrganizationMemberId",
                table: "TeamMembers",
                columns: new[] { "TeamId", "OrganizationMemberId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CreatedByPersonId",
                table: "Teams",
                column: "CreatedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_OrganizationId",
                table: "Teams",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamTrainers_CreatedByPersonId",
                table: "TeamTrainers",
                column: "CreatedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamTrainers_TeamId",
                table: "TeamTrainers",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamTrainers_TeamId_TrainerMemberId",
                table: "TeamTrainers",
                columns: new[] { "TeamId", "TrainerMemberId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_TeamTrainers_TrainerMemberId",
                table: "TeamTrainers",
                column: "TrainerMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerSessionRates_CreatedByPersonId",
                table: "TrainerSessionRates",
                column: "CreatedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerSessionRates_TrainerMemberId",
                table: "TrainerSessionRates",
                column: "TrainerMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerSessionRates_TrainerMemberId_ValidFrom",
                table: "TrainerSessionRates",
                columns: new[] { "TrainerMemberId", "ValidFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ExternalId_AuthProvider",
                table: "Users",
                columns: new[] { "ExternalId", "AuthProvider" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CancellationRequests");

            migrationBuilder.DropTable(
                name: "EventTeamEnrollments");

            migrationBuilder.DropTable(
                name: "EventTrainers");

            migrationBuilder.DropTable(
                name: "GroupCostRates");

            migrationBuilder.DropTable(
                name: "GroupMembers");

            migrationBuilder.DropTable(
                name: "MemberAvailabilities");

            migrationBuilder.DropTable(
                name: "MessageRecipients");

            migrationBuilder.DropTable(
                name: "OrganizationMemberRoles");

            migrationBuilder.DropTable(
                name: "ParentChildRelations");

            migrationBuilder.DropTable(
                name: "ParticipantTrainers");

            migrationBuilder.DropTable(
                name: "TeamGroups");

            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropTable(
                name: "TeamTrainers");

            migrationBuilder.DropTable(
                name: "TrainerSessionRates");

            migrationBuilder.DropTable(
                name: "EventEnrollments");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "OrganizationMembers");

            migrationBuilder.DropTable(
                name: "EventSeries");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "Persons");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
