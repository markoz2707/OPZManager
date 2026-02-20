using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OPZManager.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicAccessEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnonymousSessionId",
                table: "OPZDocuments",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LeadCaptures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    MarketingConsent = table.Column<bool>(type: "boolean", nullable: false),
                    AnonymousSessionId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    OPZDocumentId = table.Column<int>(type: "integer", nullable: true),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    DownloadToken = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DownloadTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadCaptures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadCaptures_OPZDocuments_OPZDocumentId",
                        column: x => x.OPZDocumentId,
                        principalTable: "OPZDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OPZVerificationResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OPZDocumentId = table.Column<int>(type: "integer", nullable: false),
                    OverallScore = table.Column<int>(type: "integer", nullable: false),
                    Grade = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    CompletenessJson = table.Column<string>(type: "text", nullable: true),
                    ComplianceJson = table.Column<string>(type: "text", nullable: true),
                    TechnicalJson = table.Column<string>(type: "text", nullable: true),
                    GapAnalysisJson = table.Column<string>(type: "text", nullable: true),
                    SummaryText = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OPZVerificationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OPZVerificationResults_OPZDocuments_OPZDocumentId",
                        column: x => x.OPZDocumentId,
                        principalTable: "OPZDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OPZDocuments_AnonymousSessionId",
                table: "OPZDocuments",
                column: "AnonymousSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadCaptures_AnonymousSessionId",
                table: "LeadCaptures",
                column: "AnonymousSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadCaptures_DownloadToken",
                table: "LeadCaptures",
                column: "DownloadToken");

            migrationBuilder.CreateIndex(
                name: "IX_LeadCaptures_Email",
                table: "LeadCaptures",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_LeadCaptures_OPZDocumentId",
                table: "LeadCaptures",
                column: "OPZDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_OPZVerificationResults_OPZDocumentId",
                table: "OPZVerificationResults",
                column: "OPZDocumentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeadCaptures");

            migrationBuilder.DropTable(
                name: "OPZVerificationResults");

            migrationBuilder.DropIndex(
                name: "IX_OPZDocuments_AnonymousSessionId",
                table: "OPZDocuments");

            migrationBuilder.DropColumn(
                name: "AnonymousSessionId",
                table: "OPZDocuments");
        }
    }
}
