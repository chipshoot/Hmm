using FluentValidation;
using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Dal.Query;

namespace Hmm.Core.DefaultManager.Validator
{
    public static class ValidationExtensionHelper
    {
        public static IRuleBuilderOptions<T, int> EntityMustExists<T>(this IRuleBuilder<T, int> ruleBuilder, IEntityLookup lookupRepo) where T : Entity
        {
            return ruleBuilder.Must(id =>
            {
                var entity = lookupRepo.GetEntity<T>(id);
                return entity != null;
            });
        }
    }
}