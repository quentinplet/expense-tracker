using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNameToAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Seed.SeedUsers ne rejoue pas sur une base qui a déjà des utilisateurs
            // (John, admin) : backfill explicite plutôt que les laisser à "".
            migrationBuilder.Sql("""
                UPDATE "AspNetUsers" SET "FirstName" = 'John', "LastName" = 'Doe' WHERE "UserName" = 'john';
                UPDATE "AspNetUsers" SET "FirstName" = 'Admin', "LastName" = 'User' WHERE "UserName" = 'admin';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "AspNetUsers");
        }
    }
}
