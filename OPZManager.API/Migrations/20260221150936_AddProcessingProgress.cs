using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OPZManager.API.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessingProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProcessingProgress",
                table: "KnowledgeDocuments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingStep",
                table: "KnowledgeDocuments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessingProgress",
                table: "KnowledgeDocuments");

            migrationBuilder.DropColumn(
                name: "ProcessingStep",
                table: "KnowledgeDocuments");
        }
    }
}
