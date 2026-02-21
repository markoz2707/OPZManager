using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OPZManager.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRequirementCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequirementCompliances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EquipmentMatchId = table.Column<int>(type: "integer", nullable: false),
                    RequirementId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Explanation = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequirementCompliances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequirementCompliances_EquipmentMatches_EquipmentMatchId",
                        column: x => x.EquipmentMatchId,
                        principalTable: "EquipmentMatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequirementCompliances_OPZRequirements_RequirementId",
                        column: x => x.RequirementId,
                        principalTable: "OPZRequirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequirementCompliances_EquipmentMatchId_RequirementId",
                table: "RequirementCompliances",
                columns: new[] { "EquipmentMatchId", "RequirementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequirementCompliances_RequirementId",
                table: "RequirementCompliances",
                column: "RequirementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequirementCompliances");
        }
    }
}
