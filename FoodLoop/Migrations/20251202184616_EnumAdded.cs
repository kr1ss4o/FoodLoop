using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodLoop.Migrations
{
    /// <inheritdoc />
    public partial class EnumAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientNameSnapshot",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ClientPhoneSnapshot",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "OfferPriceSnapshot",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "OfferTitleSnapshot",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Reservations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientNameSnapshot",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientPhoneSnapshot",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OfferPriceSnapshot",
                table: "Reservations",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfferTitleSnapshot",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
