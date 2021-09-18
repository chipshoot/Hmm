using Hmm.Automobile.DomainEntity;
using Hmm.Core.DomainEntity;
using Hmm.Core.NoteSerializer;
using Microsoft.Extensions.Logging;
using System;
using System.Xml.Linq;

namespace Hmm.Automobile.NoteSerializer
{
    public class EntityXmlNoteSerializerBase<T> : DefaultXmlNoteSerializer<T> where T : AutomobileBase
    {
        protected EntityXmlNoteSerializerBase(XNamespace noteRootNamespace, NoteCatalog catalog, ILogger logger) : base(noteRootNamespace, catalog, logger)
        {
        }

        public override HmmNote GetNote(in T entity)
        {
            if (entity == null)
            {
                ProcessResult.AddWaningMessage($"Null entity found when try to serializing entity to note", true);
                return null;
            }

            var subject = entity.GetSubject();
            var note = new HmmNote
            {
                Id = entity.Id,
                Subject = subject,
                Content = GetNoteSerializationText(entity),
                Catalog = Catalog
            };

            return note;
        }

        protected (XElement, XNamespace) GetEntityRoot(HmmNote note, string subject)
        {
            if (note == null || string.IsNullOrEmpty(subject))
            {
                return (null, null);
            }

            try
            {
                var noteStr = note.Content;
                var noteXml = XDocument.Parse(noteStr);
                var ns = noteXml.Root?.GetDefaultNamespace();

                // validate against schema
                ValidateContent(noteXml);
                var entityRoot = noteXml.Root?.Element(ns + "Content")?.Element(ns + subject);
                switch (entityRoot)
                {
                    case null:
                        ProcessResult.AddErrorMessage("Null entity found when try to serializing entity to note", true);
                        return (null, null);

                    default:
                        return (entityRoot, ns);
                }
            }
            catch (Exception e)
            {
                ProcessResult.WrapException(e);
                return (null, null);
            }
        }
    }
}