using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoopWorkshop.Backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryWeightsAndResultOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "TestCaseResults",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TaskCategoryWeights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskCategoryWeights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskCategoryWeights_TaskItems_TaskItemId",
                        column: x => x.TaskItemId,
                        principalTable: "TaskItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskCategoryWeights_TaskItemId_Category",
                table: "TaskCategoryWeights",
                columns: new[] { "TaskItemId", "Category" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskCategoryWeights");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "TestCaseResults");
        }
    }
}
