/* Seed data for Hmm database - aligned with init-db.sql schema */

/* Seed Authors */
INSERT INTO dbo.Authors ([AccountName], [Role], [IsActivated], [Description])
VALUES (N'fchy', 1, 1, N'Default test author');

INSERT INTO dbo.Authors ([AccountName], [Role], [IsActivated], [Description])
VALUES (N'fzt', 1, 1, N'Secondary test author');

/* Seed NoteCatalogs */
INSERT INTO dbo.NoteCatalogs ([Name], [Schema], [Format], [IsDefault], [Description])
VALUES (N'DefaultCatalog',
    N'<?xml version="1.0" encoding="UTF-16"?><xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:vc="http://www.w3.org/2007/XMLSchema-versioning" xmlns:hmm="http://schema.hmm.com/2020" targetNamespace="http://schema.hmm.com/2020" elementFormDefault="qualified" attributeFormDefault="unqualified" vc:minVersion="1.1"><xs:element name="Note"><xs:annotation><xs:documentation>The root of all note managed by HMM</xs:documentation></xs:annotation><xs:complexType><xs:sequence><xs:element name="Content" type="xs:string"/></xs:sequence></xs:complexType></xs:element></xs:schema>',
    'plain_text', 1, N'Default catalog for general notes');

INSERT INTO dbo.NoteCatalogs ([Name], [Schema], [Format], [IsDefault], [Description])
VALUES (N'Hmm.AutomobileMan.AutomobileInfo',
    N'<xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:vc="http://www.w3.org/2007/XMLSchema-versioning" xmlns:hmm="http://schema.hmm.com/2020" targetNamespace="http://schema.hmm.com/2020" elementFormDefault="qualified" attributeFormDefault="unqualified" vc:minVersion="1.1"><xs:element name="Note"><xs:annotation><xs:documentation>The root of all note managed by HMM</xs:documentation></xs:annotation><xs:complexType><xs:sequence><xs:element name="Content"><xs:complexType><xs:sequence><xs:element name="Automobile"><xs:complexType><xs:sequence><xs:element name="Builder" type="xs:string"/><xs:element name="Brand" type="xs:string"/><xs:element name="Year" type="xs:string"/><xs:element name="Color" type="xs:string"/><xs:element name="Plate" type="xs:string"/><xs:element name="PIN" type="xs:string"/></xs:sequence></xs:complexType></xs:element></xs:sequence></xs:complexType></xs:element></xs:sequence></xs:complexType></xs:element></xs:schema>',
    'xml', 0, N'Automobile information catalog');

INSERT INTO dbo.NoteCatalogs ([Name], [Schema], [Format], [IsDefault], [Description])
VALUES (N'Hmm.AutomobileMan.Discount',
    N'<?xml version="1.0" encoding="UTF-16"?><xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:vc="http://www.w3.org/2007/XMLSchema-versioning" xmlns:rns="http://schema.hmm.com/2020" targetNamespace="http://schema.hmm.com/2020" elementFormDefault="qualified" attributeFormDefault="unqualified" vc:minVersion="1.1"><xs:element name="Note"><xs:annotation><xs:documentation>The root of all note managed by HMM</xs:documentation></xs:annotation><xs:complexType><xs:sequence><xs:element name="Content"><xs:complexType><xs:sequence><xs:element name="GasDiscount"><xs:annotation><xs:documentation>The discount information for gas log</xs:documentation></xs:annotation><xs:complexType><xs:sequence><xs:element name="Program" type="xs:string"/><xs:element name="Amount" type="rns:MonetaryType"/><xs:element name="DiscountType" type="xs:int"/><xs:element name="isActive" type="xs:boolean"/><xs:element name="Comment" type="xs:string"/></xs:sequence></xs:complexType></xs:element></xs:sequence></xs:complexType></xs:element></xs:sequence></xs:complexType></xs:element><xs:complexType name="MonetaryType"><xs:sequence><xs:element name="Money"><xs:complexType><xs:sequence><xs:element name="Value" type="xs:decimal"/><xs:element name="Code" type="xs:string"/></xs:sequence></xs:complexType></xs:element></xs:sequence></xs:complexType></xs:schema>',
    'xml', 0, N'Gas discount catalog');

INSERT INTO dbo.NoteCatalogs ([Name], [Schema], [Format], [IsDefault], [Description])
VALUES (N'Hmm.AutomobileMan.GasLog',
    N'<?xml version="1.0" encoding="UTF-16"?><xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:vc="http://www.w3.org/2007/XMLSchema-versioning" xmlns:rns="http://schema.hmm.com/2020" targetNamespace="http://schema.hmm.com/2020" elementFormDefault="qualified" attributeFormDefault="unqualified" vc:minVersion="1.1"><xs:element name="Note"><xs:annotation><xs:documentation>The root of all note managed by HMM</xs:documentation></xs:annotation><xs:complexType><xs:sequence><xs:element name="Content"><xs:complexType><xs:sequence><xs:element name="GasLog"><xs:annotation><xs:documentation>Gas log entry</xs:documentation></xs:annotation><xs:complexType><xs:sequence><xs:element name="Date" type="xs:dateTime"/><xs:element name="Distance" type="rns:DimensionType"/><xs:element name="CurrentMeterReading" type="rns:DimensionType"/><xs:element name="Gas"><xs:complexType><xs:complexContent><xs:extension base="rns:VolumeType"/></xs:complexContent></xs:complexType></xs:element><xs:element name="Price" type="rns:MonetaryType"/><xs:element name="GasStation" type="xs:string"/><xs:element name="Discounts"><xs:complexType><xs:sequence><xs:element name="Discount" type="rns:DiscountType" minOccurs="0" maxOccurs="unbounded"/></xs:sequence></xs:complexType></xs:element><xs:element name="Automobile" type="xs:string"/></xs:sequence></xs:complexType></xs:element></xs:sequence></xs:complexType></xs:element></xs:sequence></xs:complexType></xs:element><xs:complexType name="DimensionType"><xs:sequence><xs:element name="Dimension"><xs:complexType><xs:sequence><xs:element name="Value" type="xs:double"/><xs:element name="Unit" type="xs:string"/></xs:sequence></xs:complexType></xs:element></xs:sequence></xs:complexType><xs:complexType name="VolumeType"><xs:sequence><xs:element name="Volume"><xs:complexType><xs:sequence><xs:element name="Value"/><xs:element name="Unit"/></xs:sequence></xs:complexType></xs:element></xs:sequence></xs:complexType><xs:complexType name="MonetaryType"><xs:sequence><xs:element name="Money"><xs:complexType><xs:sequence><xs:element name="Value" type="xs:decimal"/><xs:element name="Code" type="xs:string"/></xs:sequence></xs:complexType></xs:element></xs:sequence></xs:complexType><xs:complexType name="DiscountType"><xs:sequence><xs:element name="Amount" type="rns:MonetaryType"/><xs:element name="Program" type="xs:string"/></xs:sequence></xs:complexType></xs:schema>',
    'xml', 0, N'Gas log catalog');

/* Seed sample Notes - AuthorId 1 = fchy, CatalogId references: 1=Default, 2=AutomobileInfo, 3=Discount, 4=GasLog */
INSERT INTO dbo.Notes ([Subject], [Content], [CatalogId], [AuthorId], [CreateDate], [LastModifiedDate], [Description])
VALUES (N'Automobile',
    N'<?xml version="1.0" encoding="utf-8"?><Note xmlns="http://schema.hmm.com/2020"><Content><Automobile><Maker>Subaru</Maker><Brand>Outback</Brand><Year>2020</Year><Color>Blue</Color><Plate>BCTT208</Plate><Pin>PIN1</Pin><MeterReading>125402</MeterReading></Automobile></Content></Note>',
    2, 1, '2020-10-10', '2020-10-10', N'Second car');

INSERT INTO dbo.Notes ([Subject], [Content], [CatalogId], [AuthorId], [CreateDate], [LastModifiedDate], [Description])
VALUES (N'GasDiscount',
    N'<?xml version="1.0" encoding="utf-8"?><Note xmlns="http://schema.hmm.com/2020"><Content><GasDiscount><Program>Patrol Canada Membership</Program><Amount><Money><Value>0.2</Value><Code>CAD</Code></Money></Amount><DiscountType>1</DiscountType><IsActive>true</IsActive><Comment>Save 0.2 dollar per liter</Comment></GasDiscount></Content></Note>',
    3, 1, '2020-01-01', '2020-01-01', N'Patrol Canada Membership');

INSERT INTO dbo.Notes ([Subject], [Content], [CatalogId], [AuthorId], [CreateDate], [LastModifiedDate], [Description])
VALUES (N'GasLog',
    N'<?xml version="1.0" encoding="utf-8"?><Note xmlns="http://schema.hmm.com/2020"><Content><GasLog><Date>2019-01-01T01:01:01-05:00</Date><Distance><Dimension><Value>400</Value><Unit>Kilometre</Unit></Dimension></Distance><CurrentMeterReading><Dimension><Value>12400</Value><Unit>Kilometre</Unit></Dimension></CurrentMeterReading><Gas><Volume><Value>50</Value><Unit>Liter</Unit></Volume></Gas><Price><Money><Value>0.954</Value><Code>cad</Code></Money></Price><GasStation>PatrolCanada</GasStation><Discounts><Discount><Amount><Money><Value>0.9</Value><Code>cad</Code></Money></Amount><Program>2</Program></Discount></Discounts><Automobile>1</Automobile></GasLog></Content></Note>',
    4, 1, '2019-01-01', '2019-01-01', N'Testing gas log 1');

INSERT INTO dbo.Notes ([Subject], [Content], [CatalogId], [AuthorId], [CreateDate], [LastModifiedDate], [Description])
VALUES (N'GasLog',
    N'<?xml version="1.0" encoding="utf-8"?><Note xmlns="http://schema.hmm.com/2020"><Content><GasLog><Date>2019-01-15T01:01:01-05:00</Date><Distance><Dimension><Value>390</Value><Unit>Kilometre</Unit></Dimension></Distance><CurrentMeterReading><Dimension><Value>12790</Value><Unit>Kilometre</Unit></Dimension></CurrentMeterReading><Gas><Volume><Value>40</Value><Unit>Liter</Unit></Volume></Gas><Price><Money><Value>54</Value><Code>cad</Code></Money></Price><GasStation>Costco</GasStation><Discounts></Discounts><Automobile>1</Automobile></GasLog></Content></Note>',
    4, 1, '2019-01-15', '2019-01-15', N'Testing gas log 2');

INSERT INTO dbo.Notes ([Subject], [Content], [CatalogId], [AuthorId], [CreateDate], [LastModifiedDate], [Description])
VALUES (N'GasLog',
    N'<?xml version="1.0" encoding="utf-8"?><Note xmlns="http://schema.hmm.com/2020"><Content><GasLog><Date>2019-01-15T01:01:01-05:00</Date><Distance><Dimension><Value>390</Value><Unit>Kilometre</Unit></Dimension></Distance><CurrentMeterReading><Dimension><Value>13180</Value><Unit>Kilometre</Unit></Dimension></CurrentMeterReading><Gas><Volume><Value>40</Value><Unit>Liter</Unit></Volume></Gas><Price><Money><Value>54</Value><Code>cad</Code></Money></Price><GasStation>Costco</GasStation><Discounts></Discounts><Automobile>1</Automobile></GasLog></Content></Note>',
    4, 2, '2019-01-15', '2019-01-15', N'Testing gas log 3 for fzt');

PRINT 'Seed data inserted successfully.';
GO
