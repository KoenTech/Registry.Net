using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OCIRegistry.Migrations
{
    /// <inheritdoc />
    public partial class BlobRepo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlobRepository",
                columns: table => new
                {
                    BlobsId = table.Column<string>(type: "TEXT", nullable: false),
                    RepositoriesId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlobRepository", x => new { x.BlobsId, x.RepositoriesId });
                    table.ForeignKey(
                        name: "FK_BlobRepository_Blobs_BlobsId",
                        column: x => x.BlobsId,
                        principalTable: "Blobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BlobRepository_Repositories_RepositoriesId",
                        column: x => x.RepositoriesId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlobRepository_RepositoriesId",
                table: "BlobRepository",
                column: "RepositoriesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlobRepository");
        }
    }
}
