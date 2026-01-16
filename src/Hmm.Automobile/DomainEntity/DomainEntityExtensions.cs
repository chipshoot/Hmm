using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.Automobile.DomainEntity
{
    public static class DomainEntityExtensions
    {
        public static string GetSubject(this AutomobileBase entity)
        {
            return entity switch
            {
                AutomobileInfo => AutomobileConstant.AutoMobileRecordSubject,
                GasDiscount => AutomobileConstant.GasDiscountRecordSubject,
                GasLog => AutomobileConstant.GasLogRecordSubject,
                _ => string.Empty
            };
        }

        public static async Task<int> GetCatalogIdAsync(this AutomobileBase entity, IEntityLookup lookup)
        {
            ArgumentNullException.ThrowIfNull(lookup);

            var catalogName = entity switch
            {
                AutomobileInfo => AutomobileConstant.AutoMobileInfoCatalogName,
                GasDiscount => AutomobileConstant.GasDiscountCatalogName,
                GasLog => AutomobileConstant.GasLogCatalogName,
                _ => null
            };

            if (string.IsNullOrEmpty(catalogName))
            {
                return 0;
            }

            var catalogsResult = await lookup.GetEntitiesAsync<NoteCatalog>(cat => cat.Name == catalogName);
            if (!catalogsResult.Success || catalogsResult.Value == null)
            {
                return 0;
            }

            var catalog = catalogsResult.Value.FirstOrDefault();
            return catalog?.Id ?? 0;
        }
    }
}
