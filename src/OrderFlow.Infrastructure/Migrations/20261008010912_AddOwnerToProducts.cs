using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerToProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Atenção: o default zerado só serve para tabela vazia (a FK abaixo rejeitaria linhas antigas).
            // Até esta migration não existia endpoint para cadastrar produtos, então não há dados a migrar.
            migrationBuilder.AddColumn<Guid>(
                name: "owner_id",
                table: "products",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_products_owner_id_name",
                table: "products",
                columns: new[] { "owner_id", "name" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_price_non_negative",
                table: "products",
                sql: "price >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_stock_quantity_non_negative",
                table: "products",
                sql: "stock_quantity >= 0");

            migrationBuilder.AddForeignKey(
                name: "fk_products_users_owner_id",
                table: "products",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_products_users_owner_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_owner_id_name",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_price_non_negative",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_stock_quantity_non_negative",
                table: "products");

            migrationBuilder.DropColumn(
                name: "owner_id",
                table: "products");
        }
    }
}
