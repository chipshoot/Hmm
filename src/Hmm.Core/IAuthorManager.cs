//using Hmm.Utility.Dal.Query;
//using Hmm.Utility.Misc;
//using System;
//using System.Linq.Expressions;
//using System.Threading.Tasks;

//namespace Hmm.Core
//{
//    public interface IAuthorManager
//    {
//        PageList<AuthorDao> GetEntities(Expression<Func<AuthorDao, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null);

//        Task<PageList<AuthorDao>> GetEntitiesAsync(Expression<Func<AuthorDao, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null);

//        bool AuthorExists(string id);

//        Task<bool> AuthorExistsAsync(string id);

//        AuthorDao GetAuthorById(int id);

//        Task<AuthorDao> GetAuthorByIdAsync(int id);

//        AuthorDao Create(AuthorDao authorInfo);

//        Task<AuthorDao> CreateAsync(AuthorDao authorInfo);

//        AuthorDao Update(AuthorDao authorInfo);

//        Task<AuthorDao> UpdateAsync(AuthorDao authorInfo);

//        /// <summary>
//        /// Set the flag to deactivate author to make it invisible for system.
//        /// author may be associated with note, so we not want to delete everything
//        /// </summary>
//        /// <param name="id">The id of author whose activate flag will be set</param>
//        void DeActivate(int id);

//        Task DeActivateAsync(int id);

//        ProcessingResult ProcessResult { get; }
//    }
//}