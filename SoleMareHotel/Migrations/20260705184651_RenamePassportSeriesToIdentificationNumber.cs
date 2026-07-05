using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoleMareHotel.Migrations
{
    /// <inheritdoc />
    public partial class RenamePassportSeriesToIdentificationNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PassportSeries",
                table: "Guests",
                newName: "IdentificationNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IdentificationNumber",
                table: "Guests",
                newName: "PassportSeries");
        }
    }
}
