using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace OPZManager.API.Migrations
{
    /// <inheritdoc />
    public partial class ChangeEmbeddingDimensionsTo1024 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clear old chunks (768-dim) before changing column to 1024-dim
            migrationBuilder.Sql("DELETE FROM \"KnowledgeChunks\";");
            migrationBuilder.Sql("UPDATE \"KnowledgeDocuments\" SET \"Status\" = 'Oczekuje', \"ChunkCount\" = 0, \"ProcessingProgress\" = 0, \"ProcessingStep\" = NULL;");

            migrationBuilder.AlterColumn<Vector>(
                name: "Embedding",
                table: "KnowledgeChunks",
                type: "vector(1024)",
                nullable: true,
                oldClrType: typeof(Vector),
                oldType: "vector(768)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Vector>(
                name: "Embedding",
                table: "KnowledgeChunks",
                type: "vector(768)",
                nullable: true,
                oldClrType: typeof(Vector),
                oldType: "vector(1024)",
                oldNullable: true);
        }
    }
}
