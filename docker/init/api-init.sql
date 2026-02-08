/***** SQL Server version of database script *****/

-- Create database if not exists
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'hmm')
BEGIN
    CREATE DATABASE hmm;
END
GO

USE hmm;
GO

-- Drop existing tables if they exist (in FK-safe order)
IF OBJECT_ID('dbo.NoteTagRefs', 'U') IS NOT NULL DROP TABLE dbo.NoteTagRefs;
IF OBJECT_ID('dbo.Notes', 'U') IS NOT NULL DROP TABLE dbo.Notes;
IF OBJECT_ID('dbo.NoteCatalogs', 'U') IS NOT NULL DROP TABLE dbo.NoteCatalogs;
IF OBJECT_ID('dbo.Tags', 'U') IS NOT NULL DROP TABLE dbo.Tags;
IF OBJECT_ID('dbo.Authors', 'U') IS NOT NULL DROP TABLE dbo.Authors;
IF OBJECT_ID('dbo.Contacts', 'U') IS NOT NULL DROP TABLE dbo.Contacts;
GO

/****** Table: Contacts ******/
CREATE TABLE dbo.Contacts(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Contact NVARCHAR(MAX) NULL,
    IsActivated BIT NOT NULL DEFAULT 1,
    Description NVARCHAR(1000) NULL
);
GO

/****** Table: Authors ******/
CREATE TABLE dbo.Authors(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AccountName NVARCHAR(256) NOT NULL,
    Role INT NOT NULL,
    ContactInfo INT NULL,
    IsActivated BIT NOT NULL,
    Description NVARCHAR(1000) NULL,
    CONSTRAINT FK_Authors_Contacts FOREIGN KEY(ContactInfo) REFERENCES dbo.Contacts(Id) ON DELETE NO ACTION
);
GO

CREATE UNIQUE NONCLUSTERED INDEX idx_authors_accountname ON dbo.Authors (AccountName ASC);
GO

/****** Table: Tags ******/
CREATE TABLE dbo.Tags(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    IsActivated BIT NOT NULL,
    Description NVARCHAR(1000) NULL
);
GO

CREATE UNIQUE NONCLUSTERED INDEX idx_TagName ON dbo.Tags(Name);
GO

/****** Table: NoteCatalogs ******/
CREATE TABLE dbo.NoteCatalogs(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    [Schema] XML NOT NULL,
    Format NVARCHAR(50) NOT NULL DEFAULT 'plain_text',
    IsDefault BIT NOT NULL DEFAULT 0,
    Description NVARCHAR(1000) NULL
);
GO

CREATE UNIQUE NONCLUSTERED INDEX idx_UniqueCatalogName ON dbo.NoteCatalogs(Name);
GO

/****** Table: Notes ******/
CREATE TABLE dbo.Notes(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Subject NVARCHAR(1000) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    CatalogId INT NOT NULL,
    AuthorId INT NOT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CreateDate DATETIME2 NOT NULL,
    LastModifiedDate DATETIME2 NOT NULL,
    CreatedBy NVARCHAR(256) NULL,
    LastModifiedBy NVARCHAR(256) NULL,
    Description NVARCHAR(1000) NULL,
    Ts ROWVERSION NOT NULL,
    CONSTRAINT FK_Notes_NoteCatalogs FOREIGN KEY(CatalogId) REFERENCES dbo.NoteCatalogs(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Notes_Authors FOREIGN KEY(AuthorId) REFERENCES dbo.Authors(Id) ON DELETE NO ACTION
);
GO

CREATE NONCLUSTERED INDEX IX_Notes_AuthorId ON dbo.Notes(AuthorId);
GO

CREATE NONCLUSTERED INDEX IX_Notes_CatalogId ON dbo.Notes(CatalogId);
GO

/****** Table: NoteTagRefs ******/
CREATE TABLE dbo.NoteTagRefs(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NoteId INT NOT NULL,
    TagId INT NOT NULL,
    CONSTRAINT FK_NoteTagRefs_Notes FOREIGN KEY(NoteId) REFERENCES dbo.Notes(Id) ON DELETE CASCADE,
    CONSTRAINT FK_NoteTagRefs_Tags FOREIGN KEY(TagId) REFERENCES dbo.Tags(Id) ON DELETE CASCADE
);
GO

CREATE UNIQUE NONCLUSTERED INDEX idx_unique_noteid_tagid ON dbo.NoteTagRefs (NoteId, TagId);
GO

/****** Seed Data ******/

-- Default author
SET IDENTITY_INSERT dbo.Authors ON;
INSERT INTO dbo.Authors (Id, AccountName, Role, IsActivated, Description)
VALUES (1, 'admin', 0, 1, 'Default administrator');
SET IDENTITY_INSERT dbo.Authors OFF;
GO

-- Default note catalog
SET IDENTITY_INSERT dbo.NoteCatalogs ON;
INSERT INTO dbo.NoteCatalogs (Id, Name, [Schema], Format, IsDefault, Description)
VALUES (1, 'DefaultCatalog', '<schema />', 'plain_text', 1, 'Default note catalog');
SET IDENTITY_INSERT dbo.NoteCatalogs OFF;
GO

-- Automobile note catalog (for GasLog JSON storage)
INSERT INTO dbo.NoteCatalogs (Name, [Schema], Format, IsDefault, Description)
VALUES ('GasLog', '<schema />', 'json', 0, 'Gas log entries stored as JSON');
GO

INSERT INTO dbo.NoteCatalogs (Name, [Schema], Format, IsDefault, Description)
VALUES ('Automobile', '<schema />', 'json', 0, 'Automobile information stored as JSON');
GO

INSERT INTO dbo.NoteCatalogs (Name, [Schema], Format, IsDefault, Description)
VALUES ('GasDiscount', '<schema />', 'json', 0, 'Gas discount programs stored as JSON');
GO

-- Default tag
INSERT INTO dbo.Tags (Name, IsActivated, Description)
VALUES ('General', 1, 'General purpose tag');
GO

PRINT 'Database schema and seed data created successfully.';
GO
