using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurant.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Wave1_IngredientsTables : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "restaurant");

        migrationBuilder.CreateTable(
            name: "ingredients",
            schema: "restaurant",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                normalized_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_ingredients", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "tables",
            schema: "restaurant",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                number = table.Column<int>(type: "integer", nullable: false),
                capacity = table.Column<int>(type: "integer", nullable: false),
                description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tables", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_ingredients_normalized_name",
            schema: "restaurant",
            table: "ingredients",
            column: "normalized_name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_tables_number",
            schema: "restaurant",
            table: "tables",
            column: "number",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ingredients",
            schema: "restaurant");

        migrationBuilder.DropTable(
            name: "tables",
            schema: "restaurant");
    }
}
