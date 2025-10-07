using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechChallengeGame.Data.Migrations
{
    /// <inheritdoc />
    public partial class primeira_migracao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Game",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "VARCHAR", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "varchar(100)", nullable: false),
                    Genre = table.Column<string>(type: "varchar(100)", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ReleaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Game", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Log",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ApplicationName = table.Column<string>(type: "text", nullable: true),
                    Message = table.Column<string>(type: "text", nullable: true),
                    MessageTemplate = table.Column<string>(type: "text", nullable: true),
                    Level = table.Column<string>(type: "varchar(128)", nullable: true),
                    TimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Exception = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Log", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Promotion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "VARCHAR", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promotion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserLibrary",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLibrary", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromotionGame",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionGame", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotionGame_Game_GameId",
                        column: x => x.GameId,
                        principalTable: "Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromotionGame_Promotion_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LibraryItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserLibraryId = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    PurchasePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryItem_Game_GameId",
                        column: x => x.GameId,
                        principalTable: "Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LibraryItem_UserLibrary_UserLibraryId",
                        column: x => x.UserLibraryId,
                        principalTable: "UserLibrary",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Game_Name",
                table: "Game",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryItem_GameId",
                table: "LibraryItem",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryItem_UserLibraryId",
                table: "LibraryItem",
                column: "UserLibraryId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionGame_GameId",
                table: "PromotionGame",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionGame_PromotionId",
                table: "PromotionGame",
                column: "PromotionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LibraryItem");

            migrationBuilder.DropTable(
                name: "Log");

            migrationBuilder.DropTable(
                name: "PromotionGame");

            migrationBuilder.DropTable(
                name: "UserLibrary");

            migrationBuilder.DropTable(
                name: "Game");

            migrationBuilder.DropTable(
                name: "Promotion");
        }
    }
}
