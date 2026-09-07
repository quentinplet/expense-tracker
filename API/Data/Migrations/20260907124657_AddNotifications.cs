using System;
using API.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:frequency", "daily,weekly,monthly,yearly")
                .Annotation("Npgsql:Enum:notification_type", "recurring_transaction_generated,budget_threshold_reached")
                .Annotation("Npgsql:Enum:transaction_type", "expense,income")
                .OldAnnotation("Npgsql:Enum:frequency", "daily,weekly,monthly,yearly")
                .OldAnnotation("Npgsql:Enum:transaction_type", "expense,income");

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<NotificationType>(type: "notification_type", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    BudgetId = table.Column<Guid>(type: "uuid", nullable: true),
                    ThresholdPercent = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_BudgetId",
                table: "Notifications",
                column: "BudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TransactionId",
                table: "Notifications",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:frequency", "daily,weekly,monthly,yearly")
                .Annotation("Npgsql:Enum:transaction_type", "expense,income")
                .OldAnnotation("Npgsql:Enum:frequency", "daily,weekly,monthly,yearly")
                .OldAnnotation("Npgsql:Enum:notification_type", "recurring_transaction_generated,budget_threshold_reached")
                .OldAnnotation("Npgsql:Enum:transaction_type", "expense,income");
        }
    }
}
