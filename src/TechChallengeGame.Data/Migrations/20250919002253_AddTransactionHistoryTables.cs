using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechChallengeGame.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionHistoryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistoryPayment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoryPayment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransactionGame",
                columns: table => new
                {
                    HistoryPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionGame", x => new { x.HistoryPaymentId, x.GameId });
                    table.ForeignKey(
                        name: "FK_TransactionGame_Game_GameId",
                        column: x => x.GameId,
                        principalTable: "Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransactionGame_HistoryPayment_HistoryPaymentId",
                        column: x => x.HistoryPaymentId,
                        principalTable: "HistoryPayment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransactionGame_Promotion_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotion",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionGame_GameId",
                table: "TransactionGame",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionGame_PromotionId",
                table: "TransactionGame",
                column: "PromotionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransactionGame");

            migrationBuilder.DropTable(
                name: "HistoryPayment");
        }
    }
}
