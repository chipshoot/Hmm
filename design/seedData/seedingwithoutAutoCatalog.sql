-- clear date
DELETE FROM dbo.Notes
DBCC CHECKIDENT ('[Notes]', RESEED, 0)
GO
DELETE FROM dbo.NoteCatalogs
DBCC CHECKIDENT ('[NoteCatalogs]', RESEED, 0)
GO
DELETE FROM dbo.NoteRenders
DBCC CHECKIDENT ('[NoteRenders]', RESEED, 0)
GO
DELETE FROM dbo.Subsystems
DBCC CHECKIDENT ('[Subsystems]', RESEED, 0)
GO
DELETE FROM dbo.Authors
GO

-- insert data
INSERT INTO dbo.Authors ([ID], [AccountName], [Role], [IsActivated], [Description]) VALUES (CAST('6E8FDCED-8857-46B9-BAD8-6DF2540FD07E' AS uniqueidentifier), N'fchy', 1, 1, NULL)
INSERT INTO dbo.Authors ([ID], [AccountName], [Role], [IsActivated], [Description]) VALUES (CAST('1501CAB6-CA3F-470F-AE5E-1A0B970D1707' AS uniqueidentifier), N'fzt',  1, 1, NULL)
INSERT INTO dbo.Authors ([ID], [AccountName], [Role], [IsActivated], [Description]) VALUES (CAST('3A62B298-6479-4B54-8806-D2BB2B4B3A64' AS uniqueidentifier), N'03D9D3DE-0C3C-4775-BEC3-6B698B696837',  1, 1, 'Automobile default author')
GO

SET IDENTITY_INSERT dbo.NoteRenders ON
INSERT INTO dbo.NoteRenders ([ID], [Name], [Namespace], [IsDefault], [Description]) VALUES (1, N'DefaultRender', N'default namespace', 1, N'Default note render')
SET IDENTITY_INSERT dbo.NoteRenders OFF
GO

SET IDENTITY_INSERT dbo.Subsystems ON
INSERT INTO dbo.Subsystems ([ID], [Name], [DefaultAuthorID], [IsDefault], [Description]) VALUES (1, N'HmmNote', '6E8FDCED-8857-46B9-BAD8-6DF2540FD07E', 1, N'Default system for Hmm note managing')
--INSERT INTO dbo.Subsystems ([ID], [Name], [DefaultAuthorID], [IsDefault], [Description]) VALUES (2, N'Automobile', '6E8FDCED-8857-46B9-BAD8-6DF2540FD07E', 0, N'Manage basic information or my car')
--INSERT INTO dbo.Subsystems ([ID], [Name], [DefaultAuthorID], [IsDefault], [Description]) VALUES (3, N'Daily Tracking', '6E8FDCED-8857-46B9-BAD8-6DF2540FD07E', 0, N'Manage and tracing daily action')
SET IDENTITY_INSERT dbo.Subsystems OFF
GO

SET IDENTITY_INSERT dbo.NoteCatalogs ON
INSERT INTO dbo.NoteCatalogs ([ID], Name , [Schema] , RenderID , SubsystemID, IsDefault , Description) VALUES  (1, N'DefaultCatalog' , N'<?xml version="1.0" encoding="UTF-16"?><xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:vc="http://www.w3.org/2007/XMLSchema-versioning" xmlns:hmm="http://schema.hmm.com/2020" targetNamespace="http://schema.hmm.com/2020" elementFormDefault="qualified" attributeFormDefault="unqualified" vc:minVersion="1.1"><xs:element name="Note"><xs:annotation><xs:documentation>The root of all note managed by HMM</xs:documentation></xs:annotation><xs:complexType><xs:sequence><xs:element name="Content" type="xs:string"/></xs:sequence></xs:complexType></xs:element></xs:schema>', 1, 1, 1, 'default catalog')
--INSERT INTO dbo.NoteCatalogs ([ID], Name , [Schema] , RenderID , SubsystemID, IsDefault , Description) VALUES  (2, N'Hmm.AutomobileMan.AutomobileInfo' , N'<xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:vc="http://www.w3.org/2007/XMLSchema-versioning" xmlns:hmm="http://schema.hmm.com/2020" targetNamespace="http://schema.hmm.com/2020" elementFormDefault="qualified" attributeFormDefault="unqualified" vc:minVersion="1.1"><xs:element name="Note"><xs:annotation><xs:documentation>The root of all note managed by HMM</xs:documentation></xs:annotation><xs:complexType><xs:sequence><xs:element name="Content" ><xs:complexType><xs:sequence><xs:element name ="Automobile" ><xs:complexType><xs:sequence><xs:element name="Builder" type="xs:string"/><xs:element name="Brand" type="xs:string"/><xs:element name="Year" type="xs:string"/><xs:element name="Color" type="xs:string"/><xs:element name="Plate" type="xs:string"/><xs:element name="PIN" type="xs:string"/></xs:sequence></xs:complexType></xs:element></xs:sequence></xs:complexType></xs:element></xs:sequence></xs:complexType></xs:element></xs:schema>', 1, 2, 0, 'automobile catalog')
--INSERT INTO dbo.NoteCatalogs ([ID], Name , [Schema] , RenderID , SubsystemID, IsDefault , Description) VALUES  (3, N'Hmm.AutomobileMan.Discount' , N'<?xml version="1.0" encoding="UTF-16"?><xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:vc="http://www.w3.org/2007/XMLSchema-versioning" xmlns:rns="http://schema.hmm.com/2020" targetNamespace="http://schema.hmm.com/2020" elementFormDefault="qualified" attributeFormDefault="unqualified" vc:minVersion="1.1"><xs:element name="Note"><xs:annotation><xs:documentation>The root of all note managed by HMM</xs:documentation></xs:annotation><xs:complexType><xs:sequence><xs:element name="Content"><xs:complexType><xs:sequence><xs:element name="GasDiscount"><xs:annotation><xs:documentation>The discount information for gas log</xs:documentation></xs:annotation><xs:complexType><xs:sequence><xs:element name="Program" type="xs:string"/><xs:element name="Amount" type="rns:MonetaryType"/><xs:element name="DiscountType" type="xs:int" /><xs:element name="isActive" type="xs:boolean"/><xs:element name="Comment" type="xs:string"/></xs:sequence></xs:complexType></xs:element></xs:sequence></xs:complexType></xs:element></xs:sequence></xs:complexType></xs:element><xs:complexType name="MonetaryType"><xs:sequence><xs:element name="Money"><xs:complexType><xs:sequence><xs:element name="Value" type="xs:decimal"/><xs:element name="Code" type="xs:string"/></xs:sequence></xs:complexType></xs:element></xs:sequence></xs:complexType></xs:schema>', 1, 2, 0, 'gas discount catalog')
--INSERT INTO dbo.NoteCatalogs ([ID], Name , [Schema] , RenderID , SubsystemID, IsDefault , Description) VALUES  (4, N'Hmm.AutomobileMan.GasLog' , N'<?xml version="1.0" encoding="UTF-16"?><xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:vc="http://www.w3.org/2007/XMLSchema-versioning" xmlns:rns="http://schema.hmm.com/2020" targetNamespace="http://schema.hmm.com/2020" elementFormDefault="qualified" attributeFormDefault="unqualified" vc:minVersion="1.1"><xs:element name="Note"><xs:annotation><xs:documentation>The root of all note managed by HMM</xs:documentation></xs:annotation><xs:complexType><xs:sequence><xs:element name="Content"><xs:complexType><xs:sequence><xs:element name="GasLog"><xs:annotation><xs:documentation>Comment describing your root element</xs:documentation></xs:annotation><xs:complexType><xs:sequence><xs:element name="Date" type="xs:dateTime"/><xs:element name="Distance" type="rns:DimensionType"/><xs:element name="CurrentMeterReading" type="rns:DimensionType"/><xs:element name="Gas"><xs:complexType><xs:complexContent><xs:extension base="rns:VolumeType"/></xs:complexContent></xs:complexType></xs:element><xs:element name="Price" type="rns:MonetaryType"/><xs:element name="GasStation" type="xs:string"/><xs:element name="Discounts"><xs:complexType><xs:sequence><xs:element name="Discount" type="rns:DiscountType" minOccurs="0" maxOccurs="unbounded"/></xs:sequence></xs:complexType></xs:element><xs:element name="Automobile" type="xs:string"/></xs:sequence></xs:complexType></xs:element></xs:sequence></xs:complexType></xs:element></xs:sequence></xs:complexType></xs:element><xs:complexType name="DimensionType"><xs:sequence><xs:element name="Dimension"><xs:complexType><xs:sequence><xs:element name="Value" type="xs:double"/><xs:element name="Unit" type="xs:string"/></xs:sequence></xs:complexType></xs:element></xs:sequence></xs:complexType><xs:complexType name="VolumeType"><xs:sequence><xs:element name="Volume"><xs:complexType><xs:sequence><xs:element name="Value"/><xs:element name="Unit"/></xs:sequence></xs:complexType></xs:element></xs:sequence></xs:complexType><xs:complexType name="MonetaryType"><xs:sequence><xs:element name="Money"><xs:complexType><xs:sequence><xs:element name="Value" type="xs:decimal"/><xs:element name="Code" type="xs:string"/></xs:sequence></xs:complexType></xs:element></xs:sequence></xs:complexType><xs:complexType name="DiscountType"><xs:sequence><xs:element name="Amount" type="rns:MonetaryType"/><xs:element name="Program" type="xs:string"/></xs:sequence></xs:complexType></xs:schema>', 1, 2, 0, 'gas discount catalog')
SET IDENTITY_INSERT dbo.NoteCatalogs OFF
GO

SET IDENTITY_INSERT dbo.Notes ON
--INSERT INTO dbo.Notes ([ID], [Subject] ,[Content] ,[CatalogId] ,[AuthorID] ,[CreateDate] ,[LastModifiedDate] ,[Description]) VALUES (1, 'Automobile' ,'<?xml version="1.0" encoding="utf-8"?><Note xmlns="http://schema.hmm.com/2020"><Content><Automobile><Maker>Subaru</Maker><Brand>Outback</Brand><Year>2020</Year><Color>Blue</Color><Plate>BCTT208</Plate><Pin>PIN1</Pin><MeterReading>125402</MeterReading></Automobile></Content></Note>' ,2 ,'3A62B298-6479-4B54-8806-D2BB2B4B3A64' ,'2020-10-10' ,'2020-10-10' ,'Second car')
--INSERT INTO dbo.Notes ([ID], [Subject] ,[Content] ,[CatalogId] ,[AuthorID] ,[CreateDate] ,[LastModifiedDate] ,[Description]) VALUES (2, 'GasDiscount' ,'<?xml version="1.0" encoding="utf-8"?><Note xmlns="http://schema.hmm.com/2020"><Content><GasDiscount><Program>Patrol Canada Membership</Program><Amount><Money><Value>0.2</Value><Code>CAD</Code></Money></Amount><DiscountType>1</DiscountType><IsActive>true</IsActive><Comment>Save 0.2 dollar per liter</Comment></GasDiscount></Content></Note>' ,3 ,'3A62B298-6479-4B54-8806-D2BB2B4B3A64' ,'2020-01-01' ,'2020-01-01' ,'Patrol Canada Membership')
--INSERT INTO dbo.Notes ([ID], [Subject] ,[Content] ,[CatalogId] ,[AuthorID] ,[CreateDate] ,[LastModifiedDate] ,[Description]) VALUES (3, 'GasLog' ,'<?xml version="1.0" encoding="utf-8"?><Note xmlns="http://schema.hmm.com/2020"><Content><GasLog><Date>2019-01-01T01:01:01-05:00</Date><Distance><Dimension><Value>400</Value><Unit>Kilometre</Unit></Dimension></Distance><CurrentMeterReading><Dimension><Value>12400</Value><Unit>Kilometre</Unit></Dimension></CurrentMeterReading><Gas><Volume><Value>50</Value><Unit>Liter</Unit></Volume></Gas><Price><Money><Value>0.954</Value><Code>cad</Code></Money></Price><GasStation>PatrolCanada</GasStation><Discounts><Discount><Amount><Money><Value>0.9</Value><Code>cad</Code></Money></Amount><Program>2</Program></Discount></Discounts><Automobile>1</Automobile></GasLog></Content></Note>' ,4 ,'6E8FDCED-8857-46B9-BAD8-6DF2540FD07E' ,'2019-01-01' ,'2019-01-01' ,'Testing gas log 1')
--INSERT INTO dbo.Notes ([ID], [Subject] ,[Content] ,[CatalogId] ,[AuthorID] ,[CreateDate] ,[LastModifiedDate] ,[Description]) VALUES (4, 'GasLog' ,'<?xml version="1.0" encoding="utf-8"?><Note xmlns="http://schema.hmm.com/2020"><Content><GasLog><Date>2019-01-15T01:01:01-05:00</Date><Distance><Dimension><Value>390</Value><Unit>Kilometre</Unit></Dimension></Distance><CurrentMeterReading><Dimension><Value>12790</Value><Unit>Kilometre</Unit></Dimension></CurrentMeterReading><Gas><Volume><Value>40</Value><Unit>Liter</Unit></Volume></Gas><Price><Money><Value>54</Value><Code>cad</Code></Money></Price><GasStation>Costco</GasStation><Discounts></Discounts><Automobile>1</Automobile></GasLog></Content></Note>' ,4 ,'6E8FDCED-8857-46B9-BAD8-6DF2540FD07E' ,'2019-01-15' ,'2019-01-15' ,'Testing gas log 2')
--INSERT INTO dbo.Notes ([ID], [Subject] ,[Content] ,[CatalogId] ,[AuthorID] ,[CreateDate] ,[LastModifiedDate] ,[Description]) VALUES (5, 'GasLog' ,'<?xml version="1.0" encoding="utf-8"?><Note xmlns="http://schema.hmm.com/2020"><Content><GasLog><Date>2019-01-15T01:01:01-05:00</Date><Distance><Dimension><Value>390</Value><Unit>Kilometre</Unit></Dimension></Distance><CurrentMeterReading><Dimension><Value>13180</Value><Unit>Kilometre</Unit></Dimension></CurrentMeterReading><Gas><Volume><Value>40</Value><Unit>Liter</Unit></Volume></Gas><Price><Money><Value>54</Value><Code>cad</Code></Money></Price><GasStation>Costco</GasStation><Discounts></Discounts><Automobile>1</Automobile></GasLog></Content></Note>' ,4 ,'1501CAB6-CA3F-470F-AE5E-1A0B970D1707' ,'2019-01-15' ,'2019-01-15' ,'Testing gas log 3 for ftt')
SET IDENTITY_INSERT dbo.Notes OFF
GO
