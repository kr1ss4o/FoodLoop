using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodLoop.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipientFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsForSomeoneElse",
                table: "Reservations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RecipientFullName",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientPhone",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsForSomeoneElse",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RecipientFullName",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RecipientPhone",
                table: "Reservations");
        }
    }
}
