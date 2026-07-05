using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoleMareHotel.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "BookingRequests");

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "BookingRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    OrganizationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    INN = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    OGRN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LegalAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactPersonName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactPersonPosition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactPersonPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactPersonEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContractNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContractDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.OrganizationId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_OrganizationId",
                table: "Bookings",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_OrganizationId",
                table: "BookingRequests",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_INN",
                table: "Organizations",
                column: "INN");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Name",
                table: "Organizations",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingRequests_Organizations_OrganizationId",
                table: "BookingRequests",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "OrganizationId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Organizations_OrganizationId",
                table: "Bookings",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "OrganizationId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingRequests_Organizations_OrganizationId",
                table: "BookingRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Organizations_OrganizationId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_OrganizationId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_BookingRequests_OrganizationId",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "BookingRequests");

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "BookingRequests",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
