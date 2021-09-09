/****** Object:  Table [dbo].[Authors]    Script Date: 03/05/2018 16:05:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Authors](
	[ID] [UNIQUEIDENTIFIER] DEFAULT NEWID() NOT NULL,
	[AccountName] [nvarchar](256) NOT NULL,
    [Role] [int] NOT NULL,
	[IsActivated] [bit] NOT NULL,
	[Description] [nvarchar](1000) NULL,
 CONSTRAINT [PK_Authors] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Index [IDX_UniqueAccountName]    Script Date: 11/5/2018 3:15:56 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IDX_UniqueAccountName] ON [dbo].[Authors]
(
	[AccountName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

/****** Object:  Table [dbo].[Tags]    Script Date: 03/05/2018 16:05:01 ******/
CREATE TABLE [dbo].[Tags](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[IsActivated] [bit] NOT NULL,
	[Description] [nvarchar](1000) NULL,
 CONSTRAINT [PK_Tags] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[NoteRenders]    Script Date: 03/05/2018 16:05:01 ******/
CREATE TABLE [dbo].[NoteRenders](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](400) NOT NULL,
	[Namespace] [nvarchar](1000) NOT NULL,
	[IsDefault] BIT NOT NULL,
	[Description] [nvarchar](1000) NULL,
 CONSTRAINT [PK_NoteRenders] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[NoteRenders] ADD  CONSTRAINT [DF_NoteRenders_IsDefault]  DEFAULT ((0)) FOR [IsDefault]
GO

/****** Object:  Index [IDX_UniqueRenderName]    Script Date: 11/6/2018 1:53:16 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IDX_UniqueRenderName] ON [dbo].[NoteRenders]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

/****** Object:  Table [dbo].[Subsystems]    Script Date: 10/06/2020 16:05:01 ******/
CREATE TABLE [dbo].[Subsystems](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[IsDefault] BIT NOT NULL,
	[Description] [nvarchar](1000) NULL,
 CONSTRAINT [PK_Subsystems] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[NoteCatalogs]    Script Date: 03/05/2018 16:05:01 ******/
CREATE TABLE [dbo].[NoteCatalogs](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Schema] [xml] NOT NULL,
	[RenderID] [int] NOT NULL,
	[SubsystemID] INT NULL,
	[IsDefault] BIT NOT NULL,
	[Description] [nvarchar](1000) NULL,
 CONSTRAINT [PK_NoteCatalogs] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[NoteCatalogs]  WITH CHECK ADD  CONSTRAINT [FK_NoteCatalogs_NoteRenders] FOREIGN KEY([RenderID])
REFERENCES [dbo].[NoteRenders] ([ID])
GO

ALTER TABLE [dbo].[NoteCatalogs]  WITH CHECK ADD  CONSTRAINT [FK_NoteCatalogs_Subsystems] FOREIGN KEY([SubsystemID])
REFERENCES [dbo].[Subsystems] ([ID])
GO

ALTER TABLE [dbo].[NoteCatalogs] ADD  CONSTRAINT [DF_NoteCatalogs_IsDefault]  DEFAULT ((0)) FOR [IsDefault]
GO

/****** Object:  Index [IDX_UniqueCatalogName]    Script Date: 11/6/2018 1:55:07 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [IDX_UniqueCatalogName] ON [dbo].[NoteCatalogs]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

/****** Object:  Table [dbo].[ContactInfoCatalogs]    Script Date: 03/05/2018 16:05:01 ******/
CREATE TABLE [dbo].[ContactInfoCatalogs](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Description] [nvarchar](1000) NULL,
 CONSTRAINT [PK_ContactInfoCatalogs] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[Notes]    Script Date: 03/05/2018 16:05:01 ******/
CREATE TABLE [dbo].[Notes](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Subject] [nvarchar](1000) NOT NULL,
	[Content] [xml] NOT NULL,
	[CatalogId] [int] NOT NULL,
	[AuthorID] [UNIQUEIDENTIFIER] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[LastModifiedDate] [datetime] NOT NULL,
	[Description] [nvarchar](1000) NULL,
	[Ts] [timestamp] NOT NULL,
 CONSTRAINT [PK_Notes] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

/****** Object:  Table [dbo].[Emails]    Script Date: 03/05/2018 16:05:01 ******/
CREATE TABLE [dbo].[Emails](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Email] [nvarchar](1000) NOT NULL,
	[Owner] [UNIQUEIDENTIFIER] NOT NULL,
	[IsPrimary] [bit] NOT NULL,
	[Catalog] [int] NOT NULL,
	[IsActivated] [bit] NOT NULL,
	[Description] [nvarchar](1000) NULL,
 CONSTRAINT [PK_Emails] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[Phones]    Script Date: 03/05/2018 16:05:01 ******/
CREATE TABLE [dbo].[Phones](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[AreaCode] [int] NULL,
	[Phone] [nvarchar](1000) NOT NULL,
	[Extension] [int] NULL,
	[Owner] [UNIQUEIDENTIFIER] NOT NULL,
	[Country] [nvarchar](3) NULL,
	[Catalog] [int] NOT NULL,
	[IsActivated] [bit] NOT NULL,
	[Description] [nvarchar](1000) NULL,
 CONSTRAINT [PK_Phones] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[NoteTagRefs]    Script Date: 03/05/2018 16:05:01 ******/
CREATE TABLE [dbo].[NoteTagRefs](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Note] [int] NOT NULL,
	[Tag] [int] NOT NULL,
 CONSTRAINT [PK_NoteTagRefs] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  ForeignKey [FK_Emails_ContactInfoCatalogs]    Script Date: 03/05/2018 16:05:01 ******/
ALTER TABLE [dbo].[Emails]  WITH CHECK ADD  CONSTRAINT [FK_Emails_ContactInfoCatalogs] FOREIGN KEY([Catalog])
REFERENCES [dbo].[ContactInfoCatalogs] ([ID])
GO
ALTER TABLE [dbo].[Emails] CHECK CONSTRAINT [FK_Emails_ContactInfoCatalogs]
GO
/****** Object:  ForeignKey [FK_Emails_Authors]    Script Date: 03/05/2018 16:05:01 ******/
ALTER TABLE [dbo].[Emails]  WITH CHECK ADD  CONSTRAINT [FK_Emails_Authors] FOREIGN KEY([Owner])
REFERENCES [dbo].[Authors] ([ID])
GO
ALTER TABLE [dbo].[Emails] CHECK CONSTRAINT [FK_Emails_Authors]
GO
/****** Object:  ForeignKey [FK_Notes_NoteCatalogs]    Script Date: 03/05/2018 16:05:01 ******/
ALTER TABLE [dbo].[Notes]  WITH CHECK ADD  CONSTRAINT [FK_Notes_NoteCatalogs] FOREIGN KEY([CatalogId])
REFERENCES [dbo].[NoteCatalogs] ([ID])
GO
ALTER TABLE [dbo].[Notes] CHECK CONSTRAINT [FK_Notes_NoteCatalogs]
GO
/****** Object:  ForeignKey [FK_Notes_Authors]    Script Date: 03/05/2018 16:05:01 ******/
ALTER TABLE [dbo].[Notes]  WITH CHECK ADD  CONSTRAINT [FK_Notes_Authors] FOREIGN KEY([AuthorID])
REFERENCES [dbo].[Authors] ([ID])
GO
ALTER TABLE [dbo].[Notes] CHECK CONSTRAINT [FK_Notes_Authors]
GO
/****** Object:  ForeignKey [FK_NoteTagRefs_Notes]    Script Date: 03/05/2018 16:05:01 ******/
ALTER TABLE [dbo].[NoteTagRefs]  WITH CHECK ADD  CONSTRAINT [FK_NoteTagRefs_Notes] FOREIGN KEY([Note])
REFERENCES [dbo].[Notes] ([ID])
GO
ALTER TABLE [dbo].[NoteTagRefs] CHECK CONSTRAINT [FK_NoteTagRefs_Notes]
GO
/****** Object:  ForeignKey [FK_NoteTagRefs_Tags]    Script Date: 03/05/2018 16:05:01 ******/
ALTER TABLE [dbo].[NoteTagRefs]  WITH CHECK ADD  CONSTRAINT [FK_NoteTagRefs_Tags] FOREIGN KEY([Tag])
REFERENCES [dbo].[Tags] ([ID])
GO
ALTER TABLE [dbo].[NoteTagRefs] CHECK CONSTRAINT [FK_NoteTagRefs_Tags]
GO
/****** Object:  ForeignKey [FK_Phones_ContactInfoCatalogs]    Script Date: 03/05/2018 16:05:01 ******/
ALTER TABLE [dbo].[Phones]  WITH CHECK ADD  CONSTRAINT [FK_Phones_ContactInfoCatalogs] FOREIGN KEY([Catalog])
REFERENCES [dbo].[ContactInfoCatalogs] ([ID])
GO
ALTER TABLE [dbo].[Phones] CHECK CONSTRAINT [FK_Phones_ContactInfoCatalogs]
GO
/****** Object:  ForeignKey [FK_Phones_Authors]    Script Date: 03/05/2018 16:05:01 ******/
ALTER TABLE [dbo].[Phones]  WITH CHECK ADD  CONSTRAINT [FK_Phones_Authors] FOREIGN KEY([Owner])
REFERENCES [dbo].[Authors] ([ID])
GO
ALTER TABLE [dbo].[Phones] CHECK CONSTRAINT [FK_Phones_Authors]
GO
