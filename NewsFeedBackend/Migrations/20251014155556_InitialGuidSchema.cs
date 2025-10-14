using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsFeedBackend.Migrations
{
    /// <inheritdoc />
    public partial class InitialGuidSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExternalArticles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Url = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Source = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Language = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PublishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Summary = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Score = table.Column<double>(type: "double", nullable: true),
                    RawJson = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalArticles", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RegistrationDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Email = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserArticleActions",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ArticleId = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Action = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserArticleActions", x => new { x.UserId, x.ArticleId, x.Action });
                    table.ForeignKey(
                        name: "FK_UserArticleActions_ExternalArticles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "ExternalArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserArticleActions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Keyword = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => new { x.UserId, x.Keyword });
                    table.ForeignKey(
                        name: "FK_UserPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PreferredLanguage = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreferredCountry = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultCategory = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultTimeWindow = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalArticles_Language_Category_PublishedAt",
                table: "ExternalArticles",
                columns: new[] { "Language", "Category", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalArticles_PublishedAt",
                table: "ExternalArticles",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalArticles_Url",
                table: "ExternalArticles",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserArticleActions_ArticleId",
                table: "UserArticleActions",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserArticleActions_UserId_Action_OccurredAt",
                table: "UserArticleActions",
                columns: new[] { "UserId", "Action", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_Keyword",
                table: "UserPreferences",
                column: "Keyword");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserArticleActions");

            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.DropTable(
                name: "ExternalArticles");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
