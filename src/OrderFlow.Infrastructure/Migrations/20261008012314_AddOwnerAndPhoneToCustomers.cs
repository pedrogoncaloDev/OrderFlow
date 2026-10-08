using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerAndPhoneToCustomers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Atenção: o default zerado só serve para tabela vazia (a FK abaixo rejeitaria linhas antigas).
            // Até esta migration não existia endpoint para cadastrar clientes, então não há dados a migrar.
            migrationBuilder.DropIndex(
                name: "ix_customers_email",
                table: "customers");

            migrationBuilder.AddColumn<Guid>(
                name: "owner_id",
                table: "customers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "phone",
                table: "customers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_customers_owner_id_email",
                table: "customers",
                columns: new[] { "owner_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customers_owner_id_name",
                table: "customers",
                columns: new[] { "owner_id", "name" });

            migrationBuilder.AddForeignKey(
                name: "fk_customers_users_owner_id",
                table: "customers",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_customers_users_owner_id",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_owner_id_email",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_owner_id_name",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "owner_id",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "phone",
                table: "customers");

            migrationBuilder.CreateIndex(
                name: "ix_customers_email",
                table: "customers",
                column: "email",
                unique: true);
        }
    }
}
