using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoopWorkshop.Backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTestCaseComparisons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TestCaseComparisons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestCaseResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    Call = table.Column<string>(type: "text", nullable: false),
                    Expected = table.Column<string>(type: "text", nullable: false),
                    Actual = table.Column<string>(type: "text", nullable: false),
                    Passed = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestCaseComparisons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestCaseComparisons_TestCaseResults_TestCaseResultId",
                        column: x => x.TestCaseResultId,
                        principalTable: "TestCaseResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestCaseComparisons_TestCaseResultId",
                table: "TestCaseComparisons",
                column: "TestCaseResultId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestCaseComparisons");
        }
    }
}
