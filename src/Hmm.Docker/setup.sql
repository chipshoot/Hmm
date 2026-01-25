/* 1. Create Database and Schema */
:r /init-db.sql

/* 2. Seed Data */
USE hmm;
GO

/* Run the seeding script */
:r /seed-data.sql
GO

PRINT 'Database setup completed successfully.';
GO
