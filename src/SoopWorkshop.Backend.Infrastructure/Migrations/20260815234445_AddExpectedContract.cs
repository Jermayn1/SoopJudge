using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoopWorkshop.Backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExpectedContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedSignatures",
                table: "TaskItems");

            migrationBuilder.AddColumn<string>(
                name: "ExpectedClassName",
                table: "TaskItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TaskExpectedMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Signature = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskExpectedMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskExpectedMethods_TaskItems_TaskItemId",
                        column: x => x.TaskItemId,
                        principalTable: "TaskItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskExpectedMethods_TaskItemId",
                table: "TaskExpectedMethods",
                column: "TaskItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskExpectedMethods");

            migrationBuilder.DropColumn(
                name: "ExpectedClassName",
                table: "TaskItems");

            migrationBuilder.AddColumn<string>(
                name: "ExpectedSignatures",
                table: "TaskItems",
                type: "text",
                nullable: true);
        }
    }
}
