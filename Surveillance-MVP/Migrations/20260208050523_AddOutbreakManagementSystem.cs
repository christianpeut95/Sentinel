using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Surveillance_MVP.Migrations
{
    /// <inheritdoc />
    public partial class AddOutbreakManagementSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OutbreakId",
                table: "Notes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Outbreaks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PrimaryDiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PrimaryLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PrimaryEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeadInvestigatorId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outbreaks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Outbreaks_AspNetUsers_LeadInvestigatorId",
                        column: x => x.LeadInvestigatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Outbreaks_Diseases_PrimaryDiseaseId",
                        column: x => x.PrimaryDiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Outbreaks_Events_PrimaryEventId",
                        column: x => x.PrimaryEventId,
                        principalTable: "Events",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Outbreaks_Locations_PrimaryLocationId",
                        column: x => x.PrimaryLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OutbreakCaseDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutbreakId = table.Column<int>(type: "int", nullable: false),
                    Classification = table.Column<int>(type: "int", nullable: false),
                    CriteriaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutbreakCaseDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutbreakCaseDefinitions_Outbreaks_OutbreakId",
                        column: x => x.OutbreakId,
                        principalTable: "Outbreaks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutbreakSearchQueries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutbreakId = table.Column<int>(type: "int", nullable: false),
                    QueryName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    QueryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAutoLink = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastRunDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastRunMatchCount = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutbreakSearchQueries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutbreakSearchQueries_Outbreaks_OutbreakId",
                        column: x => x.OutbreakId,
                        principalTable: "Outbreaks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutbreakTeamMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutbreakId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RemovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutbreakTeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutbreakTeamMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OutbreakTeamMembers_Outbreaks_OutbreakId",
                        column: x => x.OutbreakId,
                        principalTable: "Outbreaks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutbreakTimelines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutbreakId = table.Column<int>(type: "int", nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    RelatedCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RelatedNoteId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutbreakTimelines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutbreakTimelines_Outbreaks_OutbreakId",
                        column: x => x.OutbreakId,
                        principalTable: "Outbreaks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutbreakCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutbreakId = table.Column<int>(type: "int", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Classification = table.Column<int>(type: "int", nullable: true),
                    LinkMethod = table.Column<int>(type: "int", nullable: false),
                    SearchQueryId = table.Column<int>(type: "int", nullable: true),
                    LinkedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LinkedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnlinkedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UnlinkedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnlinkReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutbreakCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutbreakCases_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OutbreakCases_OutbreakSearchQueries_SearchQueryId",
                        column: x => x.SearchQueryId,
                        principalTable: "OutbreakSearchQueries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OutbreakCases_Outbreaks_OutbreakId",
                        column: x => x.OutbreakId,
                        principalTable: "Outbreaks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notes_OutbreakId",
                table: "Notes",
                column: "OutbreakId");

            migrationBuilder.CreateIndex(
                name: "IX_OutbreakCaseDefinitions_OutbreakId",
                table: "OutbreakCaseDefinitions",
                column: "OutbreakId");

            migrationBuilder.CreateIndex(
                name: "IX_OutbreakCases_CaseId",
                table: "OutbreakCases",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_OutbreakCases_OutbreakId",
                table: "OutbreakCases",
                column: "OutbreakId");

            migrationBuilder.CreateIndex(
                name: "IX_OutbreakCases_SearchQueryId",
                table: "OutbreakCases",
                column: "SearchQueryId");

            migrationBuilder.CreateIndex(
                name: "IX_Outbreaks_LeadInvestigatorId",
                table: "Outbreaks",
                column: "LeadInvestigatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Outbreaks_PrimaryDiseaseId",
                table: "Outbreaks",
                column: "PrimaryDiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Outbreaks_PrimaryEventId",
                table: "Outbreaks",
                column: "PrimaryEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Outbreaks_PrimaryLocationId",
                table: "Outbreaks",
                column: "PrimaryLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_OutbreakSearchQueries_OutbreakId",
                table: "OutbreakSearchQueries",
                column: "OutbreakId");

            migrationBuilder.CreateIndex(
                name: "IX_OutbreakTeamMembers_OutbreakId",
                table: "OutbreakTeamMembers",
                column: "OutbreakId");

            migrationBuilder.CreateIndex(
                name: "IX_OutbreakTeamMembers_UserId",
                table: "OutbreakTeamMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OutbreakTimelines_OutbreakId",
                table: "OutbreakTimelines",
                column: "OutbreakId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Outbreaks_OutbreakId",
                table: "Notes",
                column: "OutbreakId",
                principalTable: "Outbreaks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Outbreaks_OutbreakId",
                table: "Notes");

            migrationBuilder.DropTable(
                name: "OutbreakCaseDefinitions");

            migrationBuilder.DropTable(
                name: "OutbreakCases");

            migrationBuilder.DropTable(
                name: "OutbreakTeamMembers");

            migrationBuilder.DropTable(
                name: "OutbreakTimelines");

            migrationBuilder.DropTable(
                name: "OutbreakSearchQueries");

            migrationBuilder.DropTable(
                name: "Outbreaks");

            migrationBuilder.DropIndex(
                name: "IX_Notes_OutbreakId",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "OutbreakId",
                table: "Notes");
        }
    }
}
