using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcurementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddProcurementRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcurementRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceId = table.Column<long>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<long>(type: "INTEGER", nullable: true),
                    CustomItemName = table.Column<string>(type: "TEXT", nullable: true),
                    CustomSpecification = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Purpose = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    AuditedById = table.Column<long>(type: "INTEGER", nullable: true),
                    AuditedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RefusalReason = table.Column<string>(type: "TEXT", nullable: true),
                    PurchasedById = table.Column<long>(type: "INTEGER", nullable: true),
                    PurchasedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcurementRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcurementRequests_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcurementRequests_Users_AuditedById",
                        column: x => x.AuditedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcurementRequests_Users_PurchasedById",
                        column: x => x.PurchasedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcurementRequests_Users_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementRequests_AuditedById",
                table: "ProcurementRequests",
                column: "AuditedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementRequests_ItemId",
                table: "ProcurementRequests",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementRequests_PurchasedById",
                table: "ProcurementRequests",
                column: "PurchasedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementRequests_SourceId",
                table: "ProcurementRequests",
                column: "SourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcurementRequests");
        }
    }
}
