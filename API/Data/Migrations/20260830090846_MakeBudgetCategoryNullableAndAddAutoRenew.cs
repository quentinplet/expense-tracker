using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeBudgetCategoryNullableAndAddAutoRenew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Budgets_UserId_Year_Month_CategoryId",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "Budgets");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "Budgets",
                newName: "AmountLimit");

            migrationBuilder.AlterColumn<string>(
                name: "Month",
                table: "Budgets",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "Budgets",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<bool>(
                name: "AutoRenew",
                table: "Budgets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_UserId_Month",
                table: "Budgets",
                columns: new[] { "UserId", "Month" },
                unique: true,
                filter: "\"CategoryId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_UserId_Month_CategoryId",
                table: "Budgets",
                columns: new[] { "UserId", "Month", "CategoryId" },
                unique: true,
                filter: "\"CategoryId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Budgets_UserId_Month",
                table: "Budgets");

            migrationBuilder.DropIndex(
                name: "IX_Budgets_UserId_Month_CategoryId",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "AutoRenew",
                table: "Budgets");

            migrationBuilder.RenameColumn(
                name: "AmountLimit",
                table: "Budgets",
                newName: "Amount");

            migrationBuilder.AlterColumn<int>(
                name: "Month",
                table: "Budgets",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(7)",
                oldMaxLength: 7);

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "Budgets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "Budgets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_UserId_Year_Month_CategoryId",
                table: "Budgets",
                columns: new[] { "UserId", "Year", "Month", "CategoryId" },
                unique: true);
        }
    }
}
