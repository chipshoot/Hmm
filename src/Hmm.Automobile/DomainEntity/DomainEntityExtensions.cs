using Hmm.Core;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Validation;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.Automobile.DomainEntity
{
    public static class DomainEntityExtensions
    {
        public static string GetSubject(this AutomobileBase entity)
        {
            var subject = entity switch
            {
                AutomobileInfo => AutomobileConstant.AutoMobileRecordSubject,
                GasDiscount => AutomobileConstant.GasDiscountRecordSubject,
                GasLog => AutomobileConstant.GasLogRecordSubject,
                _ => string.Empty
            };

            return subject;
        }

        public static int GetCatalogId(this AutomobileBase entity, IEntityLookup lookup)
        {
            if (lookup == null) throw new ArgumentNullException(nameof(lookup));
            var catalogId = 0;
            switch (entity)
            {
                case AutomobileInfo:
                    var autoCat = lookup.GetEntities<NoteCatalog>()
                        .FirstOrDefault(cat => cat.Name == AutomobileConstant.AutoMobileInfoCatalogName);
                    if (autoCat != null)
                    {
                        catalogId = autoCat.Id;
                    }
                    break;

                case GasDiscount:
                    var discountCat = lookup.GetEntities<NoteCatalog>()
                        .FirstOrDefault(cat => cat.Name == AutomobileConstant.GasDiscountCatalogName);
                    if (discountCat != null)
                    {
                        catalogId = discountCat.Id;
                    }
                    break;

                case GasLog:
                    var logCat = lookup.GetEntities<NoteCatalog>()
                        .FirstOrDefault(cat => cat.Name == AutomobileConstant.GasLogCatalogName);
                    if (logCat != null)
                    {
                        catalogId = logCat.Id;
                    }
                    break;

                default:
                    catalogId = 0;
                    break;
            }

            return catalogId;
        }

        public static async Task<int> GetCatalogIdAsync(this AutomobileBase entity, IEntityLookup lookup)
        {
            if (lookup == null) throw new ArgumentNullException(nameof(lookup));
            var catalogId = 0;
            switch (entity)
            {
                case AutomobileInfo:
                    var autoCats = await lookup.GetEntitiesAsync<NoteCatalog>(cat =>
                        cat.Name == AutomobileConstant.AutoMobileInfoCatalogName);
                    var autoCat = autoCats.FirstOrDefault();
                    if (autoCat != null)
                    {
                        catalogId = autoCat.Id;
                    }
                    break;

                case GasDiscount:
                    var discountCats = await lookup.GetEntitiesAsync<NoteCatalog>(cat => cat.Name == AutomobileConstant.GasDiscountCatalogName);
                    var discountCat = discountCats.FirstOrDefault();
                    if (discountCat != null)
                    {
                        catalogId = discountCat.Id;
                    }
                    break;

                case GasLog:
                    var logCats = await lookup.GetEntitiesAsync<NoteCatalog>(cat => cat.Name == AutomobileConstant.GasLogCatalogName);
                    var logCat = logCats.FirstOrDefault();
                    if (logCat != null)
                    {
                        catalogId = logCat.Id;
                    }
                    break;

                default:
                    catalogId = 0;
                    break;
            }

            return catalogId;
        }

        public static HmmNote GetNote(this AutomobileInfo automobile, INoteSerializer<AutomobileInfo> serializer, Author author)
        {
            Guard.Against<ArgumentNullException>(author == null, nameof(author));
            if (automobile == null)
            {
                return null;
            }

            var note = serializer.GetNote(automobile);
            note.Author = author;
            return note;
        }

        public static HmmNote GetNote(this GasDiscount discount, INoteSerializer<GasDiscount> serializer, Author author)
        {
            Guard.Against<ArgumentNullException>(author == null, nameof(author));
            if (discount == null)
            {
                return null;
            }

            var note = serializer.GetNote(discount);
            note.Author = author;
            return note;
        }

        public static HmmNote GetNote(this GasLog log, INoteSerializer<GasLog> serializer, Author author)
        {
            Guard.Against<ArgumentNullException>(author == null, nameof(author));
            if (log == null)
            {
                return null;
            }

            var note = serializer.GetNote(log);
            note.Subject = log.GetGasLogNoteSubject();
            note.Author = author;
            return note;
        }

        /// <summary>
        /// The method is used to get gas log subject. Hmm is using <see cref="AutomobileConstant"/> to get base subject for
        /// note content, however we also need a way to quickly find out the gas log of each automobile, so we add automobile
        /// id to gas log subject. e.g. GasLog,AutomobileId:1, this way Hmm can search database subject by note catalog and
        /// subject to retrieve all gas log of the automobile
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        public static string GetGasLogNoteSubject(this GasLog log) => $"{log.GetSubject()},AutomobileId:{log.Car.Id}";
    }
}