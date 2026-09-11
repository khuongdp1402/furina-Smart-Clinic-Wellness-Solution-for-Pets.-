-- Application role used by the API at runtime. Deliberately NOT superuser
-- and NOT bypassrls — this is the whole point of TASK-11's AC-1/AC-4: a
-- superuser or BYPASSRLS role skips row-level security policies entirely,
-- which would make RLS pure decoration. Migrations run as the `postgres`
-- superuser (via docker-compose's default user); only the app connects as
-- furina_app.
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'furina_app') THEN
        CREATE ROLE furina_app WITH LOGIN PASSWORD 'furina_app_dev_pw' NOSUPERUSER NOBYPASSRLS NOCREATEDB NOCREATEROLE;
    END IF;
END
$$;

GRANT CONNECT ON DATABASE furina TO furina_app;
GRANT USAGE ON SCHEMA public TO furina_app;

-- New tables created by future migrations should also be usable by furina_app
-- without a manual GRANT each time.
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO furina_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO furina_app;
