using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurant.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class Wave3_Orders : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "orders",
            schema: "restaurant",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                table_id = table.Column<Guid>(type: "uuid", nullable: false),
                waiter_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_orders", x => x.id);
                table.ForeignKey(
                    name: "fk_orders_table_table_id",
                    column: x => x.table_id,
                    principalSchema: "restaurant",
                    principalTable: "tables",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_orders_users_waiter_id",
                    column: x => x.waiter_id,
                    principalSchema: "identity",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "order_items",
            schema: "restaurant",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                dish_id = table.Column<Guid>(type: "uuid", nullable: false),
                dish_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false),
                order_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_order_items", x => x.id);
                table.ForeignKey(
                    name: "fk_order_items_dishes_dish_id",
                    column: x => x.dish_id,
                    principalSchema: "restaurant",
                    principalTable: "dishes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_order_items_orders_order_id",
                    column: x => x.order_id,
                    principalSchema: "restaurant",
                    principalTable: "orders",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_order_items_dish_id",
            schema: "restaurant",
            table: "order_items",
            column: "dish_id");

        migrationBuilder.CreateIndex(
            name: "ix_order_items_order_id",
            schema: "restaurant",
            table: "order_items",
            column: "order_id");

        migrationBuilder.CreateIndex(
            name: "ix_orders_table_id",
            schema: "restaurant",
            table: "orders",
            column: "table_id",
            unique: true,
            filter: "status = 'InProgress'");

        migrationBuilder.CreateIndex(
            name: "ix_orders_table_id_status",
            schema: "restaurant",
            table: "orders",
            columns: new[] { "table_id", "status" });

        migrationBuilder.CreateIndex(
            name: "ix_orders_waiter_id",
            schema: "restaurant",
            table: "orders",
            column: "waiter_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "order_items",
            schema: "restaurant");

        migrationBuilder.DropTable(
            name: "orders",
            schema: "restaurant");
    }
}
