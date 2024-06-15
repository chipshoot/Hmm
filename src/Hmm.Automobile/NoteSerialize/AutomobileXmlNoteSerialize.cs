using Hmm.Automobile.DomainEntity;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Validation;
using Microsoft.Extensions.Logging;
using System;
using System.Xml.Linq;

namespace Hmm.Automobile.NoteSerialize
{
    public class AutomobileXmlNoteSerialize : EntityXmlNoteSerializeBase<AutomobileInfo>
    {
        private readonly IApplication _application;
        private readonly IEntityLookup _lookup;

        public AutomobileXmlNoteSerialize(IApplication application, ILogger<AutomobileInfo> logger, IEntityLookup lookup) : base(logger)
        {
            Guard.Against<ArgumentNullException>(application == null, nameof(application));
            Guard.Against<ArgumentNullException>(lookup == null, nameof(lookup));
            _application = application;
            _lookup = lookup;
        }

        public override AutomobileInfo GetEntity(HmmNote note)
        {
            var (automobileRoot, ns) = GetEntityRoot(note, AutomobileConstant.AutoMobileRecordSubject);
            if (automobileRoot == null)
            {
                return null;
            }
            _ = int.TryParse(automobileRoot.Element(ns + "MeterReading")?.Value, out var meterReading);
            var automobile = new AutomobileInfo
            {
                Id = note.Id,
                MeterReading = meterReading,
                Brand = automobileRoot.Element(ns + "Brand")?.Value,
                Maker = automobileRoot.Element(ns + "Maker")?.Value,
                Year = automobileRoot.Element(ns + "Year")?.Value,
                Color = automobileRoot.Element(ns + "Color")?.Value,
                Pin = automobileRoot.Element(ns + "Pin")?.Value,
                Plate = automobileRoot.Element(ns + "Plate")?.Value,
                AuthorId = note.Author.Id
            };

            return automobile;
        }

        public override string GetNoteSerializationText(AutomobileInfo entity)
        {
            if (entity == null)
            {
                return string.Empty;
            }

            var xml = new XElement(AutomobileConstant.AutoMobileRecordSubject,
                new XElement("Maker", entity.Maker),
                new XElement("Brand", entity.Brand),
                new XElement("Year", entity.Year),
                new XElement("Color", entity.Color),
                new XElement("Pin", entity.Pin),
                new XElement("Plate", entity.Plate),
                new XElement("MeterReading", entity.MeterReading)
            );

            return GetNoteContent(xml).ToString(SaveOptions.DisableFormatting);
        }

        protected override NoteCatalog GetCatalog()
        {
            return _application.GetCatalog(NoteCatalogType.Automobile, _lookup);
        }
    }
}