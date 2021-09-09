using Hmm.Core.DomainEntity;
using Hmm.Utility.Validation;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Hmm.Core.NoteSerializer
{
    public class DefaultXmlNoteSerializer<T> : NoteSerializerBase<T>
    {
        private XmlSchemaSet _schemas;

        public DefaultXmlNoteSerializer(XNamespace noteRootNamespace, NoteCatalog catalog, ILogger logger) : base(logger)
        {
            Guard.Against<ArgumentNullException>(noteRootNamespace == null, nameof(noteRootNamespace));
            Guard.Against<ArgumentNullException>(catalog == null, nameof(catalog));

            ContentNamespace = noteRootNamespace;
            Catalog = catalog;
        }

        private XNamespace ContentNamespace { get; }
        
        private XmlSchemaSet Schemas => _schemas ??= GetSchema();

        protected readonly NoteCatalog Catalog;

        public override T GetEntity(HmmNote note)
        {
            if (note == null)
            {
                ProcessResult.AddWaningMessage("Null note found when try to serializing entity to note", true);
            }

            return default;
        }

        public override HmmNote GetNote(in T entity)
        {
            if (entity == null)
            {
                ProcessResult.AddWaningMessage("Null entity found when try to serializing entity to note", true);
                return null;
            }

            // if entity is HmmNote or its child
            if (entity is HmmNote hmmNote)
            {
                var note = new HmmNote
                {
                    Subject = hmmNote.Subject,
                    Content = GetNoteSerializationText(entity),
                    CreateDate = hmmNote.CreateDate,
                    Description = hmmNote.Description,
                    Author = hmmNote.Author,
                    Catalog = hmmNote.Catalog
                };
                return note;
            }

            return null;
        }

        public virtual string GetNoteSerializationText(T entity)
        {
            if (entity == null)
            {
                ProcessResult.AddWaningMessage("Null entity found when try to note serializing text for entity", true);
                return string.Empty;
            }

            if (!(entity is HmmNote note))
            {
                return string.Empty;
            }

            var xmlContent = GetNoteContent(note.Content);
            var content = xmlContent.ToString(SaveOptions.DisableFormatting);
            return content;
        }

        protected virtual XDocument GetNoteContent(string noteContent)
        {
            var isXmlContent = false;
            var xml = GetRootXmlDoc();

            // ReSharper disable PossibleNullReferenceException
            if (string.IsNullOrEmpty(noteContent))
            {
                xml.Root.Element("Content").Value = string.Empty;
                return ApplyNameSpace(xml);
            }

            // validate the content and return if the content is already valid XML note content
            try
            {
                var contentXml = XDocument.Parse(noteContent);

                // the content can be parsed by XDocument, it's valid XML string
                isXmlContent = true;
                var errors = false;
                if (Schemas != null)
                {
                    contentXml.Validate(_schemas, (o, e) => { errors = true; });
                }

                // content is already valid note XML content, return without any change
                if (!errors)
                {
                    return contentXml;
                }
            }
            catch (Exception ex)
            {
                ProcessResult.AddWaningMessage(ex.Message);
            }

            Debug.Assert(!string.IsNullOrEmpty(noteContent));

            if (isXmlContent)
            {
                try
                {
                    var innerElement = XElement.Parse(noteContent);
                    xml.Root?.Element("Content")?.Add(innerElement);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, ex.Message);
                    xml.Root.Element("Content").Value = noteContent;
                }
            }
            else
            {
                xml.Root.Element("Content").Value = noteContent;
                // ReSharper restore PossibleNullReferenceException
            }

            return ApplyNameSpace(xml);
        }

        protected virtual XDocument GetNoteContent(XElement contentXml)
        {
            Debug.Assert(contentXml != null);
            Debug.Assert(ContentNamespace != null);

            var xml = GetRootXmlDoc();

            try
            {
                xml.Root?.Element("Content")?.Add(contentXml);
            }
            catch (Exception ex)
            {
                // ReSharper disable PossibleNullReferenceException
                ProcessResult.AddErrorMessage(ex.Message);
                Logger.LogError(ex, ex.Message);
                xml.Root.Element("Content").Value = contentXml.ToString();
            }

            return ApplyNameSpace(xml);
        }

        private static XDocument GetRootXmlDoc()
        {
            var xml = new XDocument(
                new XDeclaration("1.0", "utf-16", "yes"),
                new XElement("Note", new XElement("Content", "")));
            return xml;
        }

        private XDocument ApplyNameSpace(XDocument xml)
        {
            Debug.Assert(ContentNamespace != null);
            Debug.Assert(xml != null);

            foreach (var el in xml.Descendants())
            {
                el.Name = ContentNamespace + el.Name.LocalName;
            }

            return xml;
        }

        private XmlSchemaSet GetSchema()
        {
            Debug.Assert(Catalog != null);
            if (Catalog == null || string.IsNullOrEmpty(Catalog.Schema))
            {
                return null;
            }

            try
            {
                var schemaSet = new XmlSchemaSet();
                schemaSet.Add(ContentNamespace.ToString(), XmlReader.Create(new StringReader(Catalog.Schema)));
                return schemaSet;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }
    }
}