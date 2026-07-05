using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoleMareHotel.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDamageFieldsFromAct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompensationStatus",
                table: "Acts");

            migrationBuilder.DropColumn(
                name: "DamageAmount",
                table: "Acts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompensationStatus",
                table: "Acts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DamageAmount",
                table: "Acts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }
    }
}
