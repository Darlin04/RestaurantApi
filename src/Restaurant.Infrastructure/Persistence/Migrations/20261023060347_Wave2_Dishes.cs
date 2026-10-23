using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurant.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Wave2_Dishes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "dishes",
            schema: "restaurant",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                normalized_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                servings = table.Column<int>(type: "integer", nullable: false),
                category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_dishes", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "dish_ingredients",
            schema: "restaurant",
            columns: table => new
            {
                ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                dish_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_dish_ingredients", x => new { x.dish_id, x.ingredient_id });
                table.ForeignKey(
                    name: "fk_dish_ingredients_dishes_dish_id",
                    column: x => x.dish_id,
                    principalSchema: "restaurant",
                    principalTable: "dishes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_dish_ingredients_ingredient_ingredient_id",
                    column: x => x.ingredient_id,
                    principalSchema: "restaurant",
                    principalTable: "ingredients",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_dish_ingredients_ingredient_id",
            schema: "restaurant",
            table: "dish_ingredients",
            column: "ingredient_id");

        migrationBuilder.CreateIndex(
            name: "ix_dishes_normalized_name",
            schema: "restaurant",
            table: "dishes",
            column: "normalized_name",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "dish_ingredients",
            schema: "restaurant");

        migrationBuilder.DropTable(
            name: "dishes",
            schema: "restaurant");
    }
}
