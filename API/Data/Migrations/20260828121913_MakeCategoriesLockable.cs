using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeCategoriesLockable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_UserId_Name",
                table: "Categories");

            migrationBuilder.RenameColumn(
                name: "IsSystem",
                table: "Categories",
                newName: "IsLocked");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_UserId_Name_Type",
                table: "Categories",
                columns: new[] { "UserId", "Name", "Type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_UserId_Name_Type",
                table: "Categories");

            migrationBuilder.RenameColumn(
                name: "IsLocked",
                table: "Categories",
                newName: "IsSystem");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_UserId_Name",
                table: "Categories",
                columns: new[] { "UserId", "Name" },
                unique: true);
        }
    }
}
