using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryApp.Infrastructure.Migrations
{
    /// <summary>
    /// ERP-N0.4: consolida RBAC relacional sin borrar usuarios ni grants efectivos.
    /// La existencia de RolPermiso representa permiso concedido; la ausencia, denegado.
    /// </summary>
    public partial class N0_4_ConsolidarRbacRelacional : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Asegura roles requeridos para poder transformar datos legacy antes de
            // convertir las FK relacionales en NOT NULL. No se elimina ningún usuario.
            migrationBuilder.Sql(@"
INSERT INTO Roles (Nombre, NombreNormalizado, Descripcion, EsSistema, EsAdministrador, Activo, Eliminado, FechaCreacion)
SELECT 'Administrador', 'ADMINISTRADOR', 'Rol de sistema con grants administrativos explícitos.', 1, 1, 1, 0, UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM Roles WHERE NombreNormalizado = 'ADMINISTRADOR');
");

            migrationBuilder.Sql(@"
INSERT INTO Roles (Nombre, NombreNormalizado, Descripcion, EsSistema, EsAdministrador, Activo, Eliminado, FechaCreacion)
SELECT 'Vendedor', 'VENDEDOR', 'Rol de sistema para operación comercial con permisos administrables.', 1, 0, 1, 0, UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM Roles WHERE NombreNormalizado = 'VENDEDOR')
  AND (
      EXISTS (SELECT 1 FROM Usuarios WHERE UPPER(TRIM(Rol)) = 'VENDEDOR')
      OR EXISTS (SELECT 1 FROM RolPermisos WHERE Rol = 2)
  );
");

            // Preserva roles dinámicos que aún existan únicamente en Usuarios.Rol.
            migrationBuilder.Sql(@"
INSERT INTO Roles (Nombre, NombreNormalizado, Descripcion, EsSistema, EsAdministrador, Activo, Eliminado, FechaCreacion)
SELECT src.Nombre, src.NombreNormalizado, 'Rol migrado desde Usuarios.Rol por ERP-N0.4.', 0, 0, 1, 0, UTC_TIMESTAMP(6)
FROM (
    SELECT MIN(TRIM(Rol)) AS Nombre, UPPER(TRIM(Rol)) AS NombreNormalizado
    FROM Usuarios
    WHERE Rol IS NOT NULL AND TRIM(Rol) <> ''
    GROUP BY UPPER(TRIM(Rol))
) src
LEFT JOIN Roles r ON r.NombreNormalizado = src.NombreNormalizado
WHERE r.Id IS NULL;
");

            migrationBuilder.Sql(@"
UPDATE Usuarios u
JOIN Roles r ON r.NombreNormalizado = UPPER(TRIM(u.Rol))
SET u.RolId = r.Id
WHERE u.RolId IS NULL;
");

            // RolUsuario histórico: Administrador=1, Vendedor=2.
            migrationBuilder.Sql(@"
UPDATE RolPermisos rp
JOIN Roles r ON (
    (rp.Rol = 1 AND r.NombreNormalizado = 'ADMINISTRADOR') OR
    (rp.Rol = 2 AND r.NombreNormalizado = 'VENDEDOR')
)
SET rp.RolId = r.Id
WHERE rp.Permitido = 1 AND rp.RolId IS NULL;
");

            migrationBuilder.Sql(@"
UPDATE RolPermisos rp
JOIN Permisos p ON p.Modulo = rp.Modulo AND p.Accion = rp.Accion
SET rp.PermisoId = p.Id
WHERE rp.Permitido = 1 AND rp.PermisoId IS NULL;
");

            // En el modelo N0.4 una denegación es ausencia de grant, no una fila Permitido=false.
            migrationBuilder.Sql("DELETE FROM RolPermisos WHERE Permitido = 0;");

            // Fail closed: si queda información que no puede representarse en el RBAC
            // normalizado se aborta la migración antes de retirar columnas legacy.
            migrationBuilder.Sql(@"
SET @n04_bad_users := (
    SELECT COUNT(*)
    FROM Usuarios u
    WHERE u.RolId IS NULL
       OR NOT EXISTS (SELECT 1 FROM Roles r WHERE r.Id = u.RolId)
);
");
            migrationBuilder.Sql(@"
SET @n04_guard := IF(
    @n04_bad_users = 0,
    'SELECT 1',
    'SIGNAL SQLSTATE ''45000'' SET MESSAGE_TEXT = ''ERP-N0.4 bloqueada: existen usuarios sin RolId relacional válido'''
);
");
            migrationBuilder.Sql("PREPARE n04_stmt FROM @n04_guard;");
            migrationBuilder.Sql("EXECUTE n04_stmt;");
            migrationBuilder.Sql("DEALLOCATE PREPARE n04_stmt;");

            migrationBuilder.Sql(@"
SET @n04_bad_grants := (
    SELECT COUNT(*)
    FROM RolPermisos rp
    WHERE rp.RolId IS NULL
       OR rp.PermisoId IS NULL
       OR NOT EXISTS (SELECT 1 FROM Roles r WHERE r.Id = rp.RolId)
       OR NOT EXISTS (SELECT 1 FROM Permisos p WHERE p.Id = rp.PermisoId)
);
");
            migrationBuilder.Sql(@"
SET @n04_guard := IF(
    @n04_bad_grants = 0,
    'SELECT 1',
    'SIGNAL SQLSTATE ''45000'' SET MESSAGE_TEXT = ''ERP-N0.4 bloqueada: existen grants legacy sin mapeo relacional válido'''
);
");
            migrationBuilder.Sql("PREPARE n04_stmt FROM @n04_guard;");
            migrationBuilder.Sql("EXECUTE n04_stmt;");
            migrationBuilder.Sql("DEALLOCATE PREPARE n04_stmt;");

            migrationBuilder.DropForeignKey(
                name: "FK_RolPermisos_Roles_RolId",
                table: "RolPermisos");

            migrationBuilder.DropIndex(
                name: "IX_RolPermisos_Rol_Modulo_Accion",
                table: "RolPermisos");

            migrationBuilder.DropIndex(
                name: "IX_RolPermisos_RolId_Modulo_Accion",
                table: "RolPermisos");

            migrationBuilder.DropColumn(name: "Rol", table: "Usuarios");
            migrationBuilder.DropColumn(name: "Accion", table: "RolPermisos");
            migrationBuilder.DropColumn(name: "Modulo", table: "RolPermisos");
            migrationBuilder.DropColumn(name: "Permitido", table: "RolPermisos");
            migrationBuilder.DropColumn(name: "Rol", table: "RolPermisos");

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

            // EsAdministrador no autoriza por bypass. Si ya existe catálogo de permisos,
            // materializa grants explícitos para todos los administradores activos.
            migrationBuilder.Sql(@"
INSERT INTO RolPermisos (RolId, PermisoId)
SELECT r.Id, p.Id
FROM Roles r
CROSS JOIN Permisos p
LEFT JOIN RolPermisos rp ON rp.RolId = r.Id AND rp.PermisoId = p.Id
WHERE r.EsAdministrador = 1
  AND r.Activo = 1
  AND r.Eliminado = 0
  AND p.Activo = 1
  AND p.Eliminado = 0
  AND rp.Id IS NULL;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // El formato legacy solo admite RolUsuario Administrador/Vendedor en RolPermisos.
            // Bloquea un downgrade que colapsaría roles dinámicos y perdería seguridad.
            migrationBuilder.Sql(@"
SET @n04_down_unsupported := (
    SELECT COUNT(*)
    FROM RolPermisos rp
    JOIN Roles r ON r.Id = rp.RolId
    WHERE r.NombreNormalizado NOT IN ('ADMINISTRADOR', 'VENDEDOR')
);
");
            migrationBuilder.Sql(@"
SET @n04_guard := IF(
    @n04_down_unsupported = 0,
    'SELECT 1',
    'SIGNAL SQLSTATE ''45000'' SET MESSAGE_TEXT = ''ERP-N0.4 downgrade bloqueado: existen grants de roles dinámicos no representables en RolUsuario legacy'''
);
");
            migrationBuilder.Sql("PREPARE n04_stmt FROM @n04_guard;");
            migrationBuilder.Sql("EXECUTE n04_stmt;");
            migrationBuilder.Sql("DEALLOCATE PREPARE n04_stmt;");

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

            migrationBuilder.AddColumn<int>(name: "Accion", table: "RolPermisos", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<int>(name: "Modulo", table: "RolPermisos", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<bool>(name: "Permitido", table: "RolPermisos", type: "tinyint(1)", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<int>(name: "Rol", table: "RolPermisos", type: "int", nullable: false, defaultValue: 0);

            migrationBuilder.Sql(@"
UPDATE Usuarios u
JOIN Roles r ON r.Id = u.RolId
SET u.Rol = LEFT(r.Nombre, 30);
");

            migrationBuilder.Sql(@"
UPDATE RolPermisos rp
JOIN Roles r ON r.Id = rp.RolId
JOIN Permisos p ON p.Id = rp.PermisoId
SET rp.Rol = CASE WHEN r.NombreNormalizado = 'ADMINISTRADOR' THEN 1 ELSE 2 END,
    rp.Modulo = p.Modulo,
    rp.Accion = p.Accion,
    rp.Permitido = 1;
");

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
