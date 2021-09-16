using Hmm.Automobile.DomainEntity;
using Hmm.Core.DomainEntity;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;

namespace Hmm.Automobile.NoteSerializer
{
    public class AutomobileXmlNoteSerializer : EntityXmlNoteSerializerBase<AutomobileInfo>
    {
        public AutomobileXmlNoteSerializer(XNamespace noteRootNamespace, NoteCatalog catalog, ILogger logger) : base(noteRootNamespace, catalog, logger)
        {
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
    }
}