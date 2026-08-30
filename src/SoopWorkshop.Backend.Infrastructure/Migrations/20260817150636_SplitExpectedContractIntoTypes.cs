using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoopWorkshop.Backend.Infrastructure.Migrations
{
    /// <summary>
    /// Der Aufgaben-Vertrag bekommt eine Ebene: statt eines einzelnen
    /// ExpectedClassName auf der Aufgabe und einer flachen Methodenliste gibt es
    /// jetzt beliebig viele geforderte Klassen mit je eigenen Methoden.
    ///
    /// Von Hand nachgearbeitet. Das Gerüst hätte die Daten verloren: es löscht
    /// ExpectedClassName und benennt TaskItemId einfach in TaskExpectedTypeId um -
    /// die bestehenden Methoden zeigten danach auf Typen, die es nicht gibt.
    /// Hier wird stattdessen umgezogen.
    /// </summary>
    public partial class SplitExpectedContractIntoTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskExpectedTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskExpectedTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskExpectedTypes_TaskItems_TaskItemId",
                        column: x => x.TaskItemId,
                        principalTable: "TaskItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskExpectedTypes_TaskItemId",
                table: "TaskExpectedTypes",
                column: "TaskItemId");

            // Jeder bisher geforderte Klassenname wird zu genau einem Typ.
            migrationBuilder.Sql("""
                INSERT INTO "TaskExpectedTypes" ("Id", "TaskItemId", "Name", "Order")
                SELECT gen_random_uuid(), "Id", "ExpectedClassName", 1
                FROM "TaskItems"
                WHERE "ExpectedClassName" IS NOT NULL AND "ExpectedClassName" <> '';
                """);

            // Erst nullable anlegen, damit die vorhandenen Zeilen überleben und
            // im nächsten Schritt zugeordnet werden können.
            migrationBuilder.AddColumn<Guid>(
                name: "TaskExpectedTypeId",
                table: "TaskExpectedMethods",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "TaskExpectedMethods" m
                SET "TaskExpectedTypeId" = t."Id"
                FROM "TaskExpectedTypes" t
                WHERE t."TaskItemId" = m."TaskItemId";
                """);

            // Methoden ohne Klassennamen kann das neue Modell nicht abbilden -
            // eine Methode gehört jetzt immer zu einer Klasse. Im Bestand gibt
            // es keine solche Zeile (nachgesehen); die Anweisung steht hier,
            // damit die Migration auch auf einer fremden Datenbank durchläuft
            // statt an der Nicht-Null-Bedingung zu scheitern.
            migrationBuilder.Sql("""
                DELETE FROM "TaskExpectedMethods" WHERE "TaskExpectedTypeId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "TaskExpectedTypeId",
                table: "TaskExpectedMethods",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropForeignKey(
                name: "FK_TaskExpectedMethods_TaskItems_TaskItemId",
                table: "TaskExpectedMethods");

            migrationBuilder.DropIndex(
                name: "IX_TaskExpectedMethods_TaskItemId",
                table: "TaskExpectedMethods");

            migrationBuilder.DropColumn(
                name: "TaskItemId",
                table: "TaskExpectedMethods");

            migrationBuilder.CreateIndex(
                name: "IX_TaskExpectedMethods_TaskExpectedTypeId",
                table: "TaskExpectedMethods",
                column: "TaskExpectedTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskExpectedMethods_TaskExpectedTypes_TaskExpectedTypeId",
                table: "TaskExpectedMethods",
                column: "TaskExpectedTypeId",
                principalTable: "TaskExpectedTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Zuletzt, damit die Daten oben noch daraus gelesen werden konnten.
            migrationBuilder.DropColumn(
                name: "ExpectedClassName",
                table: "TaskItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Der Rückweg ist zwangsläufig verlustbehaftet: das alte Modell
            // kennt nur eine Klasse je Aufgabe. Übernommen wird die erste,
            // die Methoden aller Klassen hängen danach wieder flach an der
            // Aufgabe.
            migrationBuilder.AddColumn<string>(
                name: "ExpectedClassName",
                table: "TaskItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "TaskItems" i
                SET "ExpectedClassName" = (
                    SELECT t."Name" FROM "TaskExpectedTypes" t
                    WHERE t."TaskItemId" = i."Id"
                    ORDER BY t."Order"
                    LIMIT 1);
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "TaskItemId",
                table: "TaskExpectedMethods",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "TaskExpectedMethods" m
                SET "TaskItemId" = t."TaskItemId"
                FROM "TaskExpectedTypes" t
                WHERE t."Id" = m."TaskExpectedTypeId";
                """);

            migrationBuilder.Sql("""
                DELETE FROM "TaskExpectedMethods" WHERE "TaskItemId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "TaskItemId",
                table: "TaskExpectedMethods",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropForeignKey(
                name: "FK_TaskExpectedMethods_TaskExpectedTypes_TaskExpectedTypeId",
                table: "TaskExpectedMethods");

            migrationBuilder.DropIndex(
                name: "IX_TaskExpectedMethods_TaskExpectedTypeId",
                table: "TaskExpectedMethods");

            migrationBuilder.DropColumn(
                name: "TaskExpectedTypeId",
                table: "TaskExpectedMethods");

            migrationBuilder.CreateIndex(
                name: "IX_TaskExpectedMethods_TaskItemId",
                table: "TaskExpectedMethods",
                column: "TaskItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskExpectedMethods_TaskItems_TaskItemId",
                table: "TaskExpectedMethods",
                column: "TaskItemId",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropTable(
                name: "TaskExpectedTypes");
        }
    }
}
