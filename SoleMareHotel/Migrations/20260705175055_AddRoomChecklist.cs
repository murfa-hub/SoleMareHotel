using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoleMareHotel.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomChecklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoomChecklists",
                columns: table => new
                {
                    RoomChecklistId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    HousekeeperName = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CheckDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AllItemsOk = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CleaningTaskId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomChecklists", x => x.RoomChecklistId);
                    table.ForeignKey(
                        name: "FK_RoomChecklists_CleaningTasks_CleaningTaskId",
                        column: x => x.CleaningTaskId,
                        principalTable: "CleaningTasks",
                        principalColumn: "CleaningTaskId");
                    table.ForeignKey(
                        name: "FK_RoomChecklists_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "RoomId");
                });

            migrationBuilder.CreateTable(
                name: "RoomChecklistItems",
                columns: table => new
                {
                    RoomChecklistItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomChecklistId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpectedQuantity = table.Column<int>(type: "int", nullable: false),
                    ActualQuantity = table.Column<int>(type: "int", nullable: false),
                    IsOk = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomChecklistItems", x => x.RoomChecklistItemId);
                    table.ForeignKey(
                        name: "FK_RoomChecklistItems_RoomChecklists_RoomChecklistId",
                        column: x => x.RoomChecklistId,
                        principalTable: "RoomChecklists",
                        principalColumn: "RoomChecklistId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoomChecklistItems_RoomChecklistId",
                table: "RoomChecklistItems",
                column: "RoomChecklistId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomChecklists_CheckDate",
                table: "RoomChecklists",
                column: "CheckDate");

            migrationBuilder.CreateIndex(
                name: "IX_RoomChecklists_CleaningTaskId",
                table: "RoomChecklists",
                column: "CleaningTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomChecklists_HousekeeperName",
                table: "RoomChecklists",
                column: "HousekeeperName");

            migrationBuilder.CreateIndex(
                name: "IX_RoomChecklists_RoomId",
                table: "RoomChecklists",
                column: "RoomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoomChecklistItems");

            migrationBuilder.DropTable(
                name: "RoomChecklists");
        }
    }
}
