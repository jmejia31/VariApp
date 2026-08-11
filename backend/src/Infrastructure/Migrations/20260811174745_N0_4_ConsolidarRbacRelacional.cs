using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class N0_4_ConsolidarRbacRelacional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RolPermisos_Roles_RolId",
                table: "RolPermisos");

            migrationBuilder.DropIndex(
                name: "IX_RolPermisos_Rol_Modulo_Accion",
                table: "RolPermisos");

            migrationBuilder.DropIndex(
                name: "IX_RolPermisos_RolId_Modulo_Accion",
                table: "RolPermisos");

            migrationBuilder.DeleteData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DropColumn(
                name: "Rol",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Accion",
                table: "RolPermisos");

            migrationBuilder.DropColumn(
                name: "Modulo",
                table: "RolPermisos");

            migrationBuilder.DropColumn(
                name: "Permitido",
                table: "RolPermisos");

            migrationBuilder.DropColumn(
                name: "Rol",
                table: "RolPermisos");

            migrationBuilder.AlterColumn<int>(
                name: "RolId",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RolId",
                table: "RolPermisos",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PermisoId",
                table: "RolPermisos",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RolPermisos_Roles_RolId",
                table: "RolPermisos",
                column: "RolId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RolPermisos_Roles_RolId",
                table: "RolPermisos");

            migrationBuilder.AlterColumn<int>(
                name: "RolId",
                table: "Usuarios",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Rol",
                table: "Usuarios",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "RolId",
                table: "RolPermisos",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "PermisoId",
                table: "RolPermisos",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "Accion",
                table: "RolPermisos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Modulo",
                table: "RolPermisos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Permitido",
                table: "RolPermisos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Rol",
                table: "RolPermisos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "Activo", "ActualizadoPorUsuarioId", "Bloqueado", "BloqueadoPorUsuarioId", "CreadoPorUsuarioId", "Eliminado", "EliminadoPorUsuarioId", "FechaActualizacion", "FechaBloqueo", "FechaCreacion", "FechaEliminacion", "FotoPerfilPublicId", "FotoPerfilUrl", "MotivoBloqueo", "NombreCompleto", "NombreUsuario", "PasswordHash", "Rol", "RolId" },
                values: new object[] { 1, true, null, false, null, null, false, null, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, "Administrador", "admin", "$2b$11$unl.Q/ZCV7KaW8i7BbocyemHNX9hdpAOqatkmKk2.b3PLzDjKAMuy", "Administrador", null });

            migrationBuilder.CreateIndex(
                name: "IX_RolPermisos_Rol_Modulo_Accion",
                table: "RolPermisos",
                columns: new[] { "Rol", "Modulo", "Accion" });

            migrationBuilder.CreateIndex(
                name: "IX_RolPermisos_RolId_Modulo_Accion",
                table: "RolPermisos",
                columns: new[] { "RolId", "Modulo", "Accion" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RolPermisos_Roles_RolId",
                table: "RolPermisos",
                column: "RolId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
