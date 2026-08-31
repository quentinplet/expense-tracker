using System;
using API.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Data.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Écrite à la main plutôt que scaffoldée telle quelle : le renommage
    /// RecurringExpense → RecurringTransaction fait croire à l'outil qu'il s'agit
    /// d'une nouvelle entité (Drop + Create de la table), ce qui aurait supprimé
    /// des lignes réelles. RenameTable/RenameConstraint préservent les données —
    /// voir §7 du CLAUDE.md (relire la migration avant de l'appliquer).
    /// </remarks>
    public partial class RenameRecurringExpenseToRecurringTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_RecurringExpenses_RecurringExpenseId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_RecurringExpenses_AspNetUsers_UserId",
                table: "RecurringExpenses");

            migrationBuilder.DropForeignKey(
                name: "FK_RecurringExpenses_Categories_CategoryId",
                table: "RecurringExpenses");

            migrationBuilder.RenameTable(
                name: "RecurringExpenses",
                newName: "RecurringTransactions");

            migrationBuilder.RenameColumn(
                name: "RecurringExpenseId",
                table: "Transactions",
                newName: "RecurringTransactionId");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_RecurringExpenseId",
                table: "Transactions",
                newName: "IX_Transactions_RecurringTransactionId");

            migrationBuilder.RenameIndex(
                name: "IX_RecurringExpenses_CategoryId",
                table: "RecurringTransactions",
                newName: "IX_RecurringTransactions_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_RecurringExpenses_UserId",
                table: "RecurringTransactions",
                newName: "IX_RecurringTransactions_UserId");

            migrationBuilder.Sql(
                "ALTER TABLE \"RecurringTransactions\" RENAME CONSTRAINT \"PK_RecurringExpenses\" TO \"PK_RecurringTransactions\";");

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringTransactions_AspNetUsers_UserId",
                table: "RecurringTransactions",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringTransactions_Categories_CategoryId",
                table: "RecurringTransactions",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_RecurringTransactions_RecurringTransactionId",
                table: "Transactions",
                column: "RecurringTransactionId",
                principalTable: "RecurringTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_RecurringTransactions_RecurringTransactionId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_RecurringTransactions_AspNetUsers_UserId",
                table: "RecurringTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_RecurringTransactions_Categories_CategoryId",
                table: "RecurringTransactions");

            migrationBuilder.Sql(
                "ALTER TABLE \"RecurringTransactions\" RENAME CONSTRAINT \"PK_RecurringTransactions\" TO \"PK_RecurringExpenses\";");

            migrationBuilder.RenameIndex(
                name: "IX_RecurringTransactions_CategoryId",
                table: "RecurringTransactions",
                newName: "IX_RecurringExpenses_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_RecurringTransactions_UserId",
                table: "RecurringTransactions",
                newName: "IX_RecurringExpenses_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_RecurringTransactionId",
                table: "Transactions",
                newName: "IX_Transactions_RecurringExpenseId");

            migrationBuilder.RenameColumn(
                name: "RecurringTransactionId",
                table: "Transactions",
                newName: "RecurringExpenseId");

            migrationBuilder.RenameTable(
                name: "RecurringTransactions",
                newName: "RecurringExpenses");

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringExpenses_AspNetUsers_UserId",
                table: "RecurringExpenses",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringExpenses_Categories_CategoryId",
                table: "RecurringExpenses",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_RecurringExpenses_RecurringExpenseId",
                table: "Transactions",
                column: "RecurringExpenseId",
                principalTable: "RecurringExpenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
