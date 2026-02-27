-- Example: return database name and compatibility level (run against each selected database)
SELECT DB_NAME() AS CurrentDatabase, name AS CompatibilityLevel
FROM sys.database_scoped_configurations
WHERE name = 'DEFAULT_FULLTEXT_LANGUAGE';
