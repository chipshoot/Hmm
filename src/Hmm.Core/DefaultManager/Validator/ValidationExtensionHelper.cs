//using FluentValidation;
//using Hmm.Utility.Dal.DataEntity;
//using Hmm.Utility.Dal.Query;

//namespace Hmm.Core.DefaultManager.Validator
//{
//    public static class ValidationExtensionHelper
//    {
//        public async static IRuleBuilderOptions<T, int> EntityMustExists<T>(this IRuleBuilder<T, int> ruleBuilder, IEntityLookup lookupRepository) where T : Entity
//        {
//            return ruleBuilder.Must(id =>
//            {
//                var entity = await lookupRepository.GetEntityAsync<T>(id);
//                return entity != null;
//            });
//        }
//    }
//}