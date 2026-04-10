-- Vacía todas las tablas en public excepto __EFMigrationsHistory.
-- Conserva filas de users con role = 'superadmin' (minúsculas).
-- Uso (desde PowerShell):
--   $env:PGPASSWORD='...'
--   & "C:\Program Files\PostgreSQL\18\bin\psql.exe" -h HOST -p 5432 -U admin -d schoolmanager_hx5i -v ON_ERROR_STOP=1 -f WipeKeepSuperAdmin.sql

BEGIN;

CREATE TEMP TABLE _superadmin_backup ON COMMIT DROP AS
SELECT * FROM users WHERE lower(trim(role)) = 'superadmin';

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM _superadmin_backup) THEN
    RAISE EXCEPTION 'No hay filas con role superadmin; abortado (no se borró nada en este bloque).';
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

  RAISE NOTICE '%', stmt;
  EXECUTE stmt;
END $$;

INSERT INTO users SELECT * FROM _superadmin_backup;

COMMIT;
