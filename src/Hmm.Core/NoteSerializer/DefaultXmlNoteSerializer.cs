using Hmm.Core.DomainEntity;
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

        public DefaultXmlNoteSerializer(ILogger logger) : base(logger)
        {
            ContentNamespace = CoreConstants.DefaultNoteNamespace;
        }

        protected XNamespace ContentNamespace { get; }

        private XmlSchemaSet Schemas => _schemas ??= GetSchema();

        protected NoteCatalog Catalog { get; init; }

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

            switch (entity)
            {
                // if entity is HmmNote or its child
                case HmmNote hmmNote:
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
                default:
                    return null;
            }
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
                ValidateContent(contentXml);

                // content is already valid note XML content, return without any change
                if (!(ProcessResult.HasWarning || ProcessResult.HasInfo))
                {
                    return contentXml;
                }
            }
            catch (Exception ex)
            {
                // Cannot parse string as XML, apply plain text as content
                ProcessResult.AddErrorMessage(ex.Message, false, false);
                xml.Root.Element("Content").Value = noteContent;
                return ApplyNameSpace(xml);
            }

            Debug.Assert(!string.IsNullOrEmpty(noteContent));

            try
            {
                var innerElement = XElement.Parse(noteContent);
                xml.Root?.Element("Content")?.Add(innerElement);
            }
            catch (Exception ex)
            {
                // Cannot parse string as XML, apply plain text as content
                ProcessResult.WrapException(ex);
                xml.Root.Element("Content").Value = noteContent;
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
                ProcessResult.WrapException(ex);
                xml.Root.Element("Content").Value = contentXml.ToString();
            }

            return ApplyNameSpace(xml);
        }

        protected void ValidateContent(XDocument xml)
        {
            if (xml == null)
            {
                return;
            }

            // the content can be parsed by XDocument, it's valid XML string
            if (Schemas != null)
            {
                xml.Validate(_schemas, (obj, e) =>
                {
                    switch (e.Severity)
                    {
                        case XmlSeverityType.Warning:
                            ProcessResult.AddInfoMessage(e.Message);
                            break;

                        case XmlSeverityType.Error:
                            ProcessResult.AddWaningMessage(e.Message);
                            break;

                        default:
                            ProcessResult.AddInfoMessage(e.Message);
                            break;
                    }
                });
            }
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