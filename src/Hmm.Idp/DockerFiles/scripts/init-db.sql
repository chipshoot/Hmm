-- Database Initialization Script for Hmm.Idp
-- This script creates the database if it doesn't exist
-- Run this before starting the application

USE master;
GO

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'HmmIdp')
BEGIN
    CREATE DATABASE [HmmIdp];
    PRINT 'Database HmmIdp created successfully.';
END
ELSE
BEGIN
    PRINT 'Database HmmIdp already exists.';
END
GO

USE [HmmIdp];
GO

PRINT 'Database initialization complete.';
GO
