using Hmm.Automobile.DomainEntity;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Validation;
using Microsoft.Extensions.Logging;
using System;
using System.Xml.Linq;

namespace Hmm.Automobile.NoteSerializer
{
    public class GasDiscountXmlNoteSerializer : EntityXmlNoteSerializerBase<GasDiscount>
    {
        private readonly IApplication _app;
        private readonly IEntityLookup _lookupRepo;

        public GasDiscountXmlNoteSerializer(IApplication app, ILogger<GasDiscount> logger, IEntityLookup lookupRepo) : base(logger)
        {
            Guard.Against<ArgumentNullException>(app == null, nameof(app));
            Guard.Against<ArgumentNullException>(lookupRepo == null, nameof(lookupRepo));
            _app = app;
            _lookupRepo = lookupRepo;
        }

        public override GasDiscount GetEntity(HmmNote note)
        {
            var (discountRoot, ns) = GetEntityRoot(note, AutomobileConstant.GasDiscountRecordSubject);
            if (discountRoot == null)
            {
                return null;
            }
            _ = bool.TryParse(discountRoot.Element(ns + "IsActive")?.Value, out var isActive);
            _ = Enum.TryParse<GasDiscountType>(discountRoot.Element(ns + "DiscountType")?.Value, out var discType);
            var discount = new GasDiscount
            {
                Id = note.Id,
                Program = discountRoot.Element(ns + "Program")?.Value,
                Amount = discountRoot.Element(ns + "Amount")?.GetMoney(),
                DiscountType = discType,
                IsActive = isActive,
                Comment = discountRoot.Element(ns + "Comment")?.Value,
                AuthorId = note.Author.Id
            };

            return discount;
        }

        public override string GetNoteSerializationText(GasDiscount entity)
        {
            if (entity == null)
            {
                return string.Empty;
            }

            var xml = new XElement(AutomobileConstant.GasDiscountRecordSubject,
                new XElement("Program", entity.Program),
                new XElement("Amount", entity.Amount.SerializeToXml(ContentNamespace)),
                new XElement("DiscountType", entity.DiscountType),
                new XElement("IsActive", entity.IsActive),
                new XElement("Comment", entity.Comment)
                );

            return GetNoteContent(xml).ToString(SaveOptions.DisableFormatting);
        }

        protected override NoteCatalog GetCatalog()
        {
            return _app.GetCatalog(NoteCatalogType.GasDiscount, _lookupRepo);
        }
    }
}