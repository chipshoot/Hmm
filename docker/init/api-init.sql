/***** SQL Server version of database script *****/

-- Create database if not exists
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'hmm')
BEGIN
    CREATE DATABASE hmm;
END
GO

USE hmm;
GO

-- Drop existing tables if they exist
IF OBJECT_ID('dbo.NoteTagRefs', 'U') IS NOT NULL DROP TABLE dbo.NoteTagRefs;
IF OBJECT_ID('dbo.Notes', 'U') IS NOT NULL DROP TABLE dbo.Notes;
IF OBJECT_ID('dbo.NoteCatalogs', 'U') IS NOT NULL DROP TABLE dbo.NoteCatalogs;
IF OBJECT_ID('dbo.Tags', 'U') IS NOT NULL DROP TABLE dbo.Tags;
IF OBJECT_ID('dbo.Authors', 'U') IS NOT NULL DROP TABLE dbo.Authors;
IF OBJECT_ID('dbo.Contacts', 'U') IS NOT NULL DROP TABLE dbo.Contacts;
GO

/****** Object:  Table Contacts    Script Date: 2024/06/24 16:05:01 ******/
CREATE TABLE dbo.Contacts(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Contact NVARCHAR(MAX) NULL,
    IsActivated BIT NOT NULL DEFAULT 1,
    Description NVARCHAR(1000) NULL
);
GO

/****** Object:  Table Authors    Script Date: 2024/06/24 16:05:01 ******/
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

/****** Object:  Index IDX_UniqueAccountName    Script Date: 2024/06/24 3:15:56 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX idx_authors_accountname ON dbo.Authors (AccountName ASC);
GO

/****** Object:  Table Tags    Script Date: 03/05/2024 16:05:01 ******/
CREATE TABLE dbo.Tags(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    IsActivated BIT NOT NULL,
    Description NVARCHAR(1000) NULL
);
GO

CREATE UNIQUE NONCLUSTERED INDEX idx_TagName ON dbo.Tags(Name);
GO

/****** Object:  Table NoteCatalogs    Script Date: 06/05/2024 16:05:01 ******/
CREATE TABLE dbo.NoteCatalogs(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    [Schema] XML NOT NULL,
    Format NVARCHAR(50) NOT NULL DEFAULT 'plain_text', -- 'plain_text', 'xml', 'json', 'markdown'
    IsDefault BIT NOT NULL DEFAULT 0,
    Description NVARCHAR(1000) NULL
);
GO

/****** Object:  Index idx_UniqueCatalogName    Script Date: 2024/06/24 1:55:07 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX idx_UniqueCatalogName ON dbo.NoteCatalogs(Name);
GO

/****** Object:  Table Notes Script Date: 2024/06/24 16:05:01 ******/
CREATE TABLE dbo.Notes(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Subject NVARCHAR(1000) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    CatalogId INT NOT NULL,
    AuthorId INT NOT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CreateDate DATETIME2 NOT NULL,
    LastModifiedDate DATETIME2 NOT NULL,
    Description NVARCHAR(1000) NULL,
    Ts ROWVERSION NOT NULL,
    CONSTRAINT FK_Notes_NoteCatalogs FOREIGN KEY(CatalogId) REFERENCES dbo.NoteCatalogs(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Notes_Authors FOREIGN KEY(AuthorId) REFERENCES dbo.Authors(Id) ON DELETE NO ACTION
);
GO

/****** Object:  Table NoteTagRefs    Script Date: 2024/06/24 16:05:01 ******/
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

PRINT 'Database schema created successfully.';
GO
