using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsFeedBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddedUrlHashing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExternalArticles_Url",
                table: "ExternalArticles");

            migrationBuilder.AddColumn<string>(
                name: "UrlHash",
                table: "ExternalArticles",
                type: "char(64)",
                nullable: false,
                computedColumnSql: "lower(hex(sha2(`Url`, 256)))",
                stored: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalArticles_UrlHash",
                table: "ExternalArticles",
                column: "UrlHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExternalArticles_UrlHash",
                table: "ExternalArticles");

            migrationBuilder.DropColumn(
                name: "UrlHash",
                table: "ExternalArticles");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalArticles_Url",
                table: "ExternalArticles",
                column: "Url",
                unique: true);
        }
    }
}
