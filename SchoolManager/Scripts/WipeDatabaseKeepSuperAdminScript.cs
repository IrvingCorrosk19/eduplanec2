using Microsoft.EntityFrameworkCore;
using SchoolManager.Models;

namespace SchoolManager.Scripts;

/// <summary>
/// Misma lógica que Scripts/WipeKeepSuperAdmin.sql: conserva users con role superadmin, trunca el resto.
/// Ejecutar: dotnet run -- --wipe-database-keep-superadmin
/// </summary>
public static class WipeDatabaseKeepSuperAdminScript
{
    public static async Task RunAsync(SchoolDbContext context)
    {
        const string sql = """
            BEGIN;

            CREATE TEMP TABLE _superadmin_backup ON COMMIT DROP AS
            SELECT * FROM users WHERE lower(trim(role)) = 'superadmin';

            DO $$
            BEGIN
              IF NOT EXISTS (SELECT 1 FROM _superadmin_backup) THEN
                RAISE EXCEPTION 'No hay filas con role superadmin; abortado.';
              END IF;
            END $$;

            DO $$
            DECLARE
              stmt text;
            BEGIN
              SELECT 'TRUNCATE TABLE ' || string_agg(format('%I.%I', schemaname, tablename), ', ' ORDER BY tablename)
                     || ' RESTART IDENTITY CASCADE'
              INTO stmt
              FROM pg_tables
              WHERE schemaname = 'public'
                AND tablename <> '__EFMigrationsHistory';

              IF stmt IS NULL OR stmt LIKE 'TRUNCATE TABLE  RESTART IDENTITY CASCADE' THEN
                RAISE EXCEPTION 'No hay tablas para truncar en public (inesperado).';
              END IF;

              EXECUTE stmt;
            END $$;

            INSERT INTO users SELECT * FROM _superadmin_backup;

            COMMIT;
            """;

        await context.Database.ExecuteSqlRawAsync(sql);
    }
}
