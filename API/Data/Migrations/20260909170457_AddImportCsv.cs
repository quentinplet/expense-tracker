using System;
using API.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImportCsv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:frequency", "daily,weekly,monthly,yearly")
                .Annotation("Npgsql:Enum:import_status", "pending,committed,failed")
                .Annotation("Npgsql:Enum:notification_type", "recurring_transaction_generated,budget_threshold_reached")
                .Annotation("Npgsql:Enum:transaction_type", "expense,income")
                .OldAnnotation("Npgsql:Enum:frequency", "daily,weekly,monthly,yearly")
                .OldAnnotation("Npgsql:Enum:notification_type", "recurring_transaction_generated,budget_threshold_reached")
                .OldAnnotation("Npgsql:Enum:transaction_type", "expense,income");

            migrationBuilder.AddColumn<string>(
                name: "DedupHash",
                table: "Transactions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ImportBatchId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ImportBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RowCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<ImportStatus>(type: "import_status", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportBatches_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ImportBatchId",
                table: "Transactions",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId_DedupHash",
                table: "Transactions",
                columns: new[] { "UserId", "DedupHash" },
                unique: true,
                filter: "\"DedupHash\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_UserId_ImportedAt",
                table: "ImportBatches",
                columns: new[] { "UserId", "ImportedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_ImportBatches_ImportBatchId",
                table: "Transactions",
                column: "ImportBatchId",
                principalTable: "ImportBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_ImportBatches_ImportBatchId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "ImportBatches");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ImportBatchId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_UserId_DedupHash",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "DedupHash",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ImportBatchId",
                table: "Transactions");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:frequency", "daily,weekly,monthly,yearly")
                .Annotation("Npgsql:Enum:notification_type", "recurring_transaction_generated,budget_threshold_reached")
                .Annotation("Npgsql:Enum:transaction_type", "expense,income")
                .OldAnnotation("Npgsql:Enum:frequency", "daily,weekly,monthly,yearly")
                .OldAnnotation("Npgsql:Enum:import_status", "pending,committed,failed")
                .OldAnnotation("Npgsql:Enum:notification_type", "recurring_transaction_generated,budget_threshold_reached")
                .OldAnnotation("Npgsql:Enum:transaction_type", "expense,income");
        }
    }
}
