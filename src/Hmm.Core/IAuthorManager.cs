using Hmm.Core.DomainEntity;
using Hmm.Utility.Misc;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core
{
    public interface IAuthorManager
    {
        IEnumerable<Author> GetEntities();

        Task<IEnumerable<Author>> GetEntitiesAsync(Expression<Func<Author, bool>> query = null);

        bool AuthorExists(string id);

        Task<bool> AuthorExistsAsync(string id);

        Author GetAuthorById(Guid id);

        Task<Author> GetAuthorByIdAsync(Guid id);

        Author Create(Author authorInfo);

        Task<Author> CreateAsync(Author authorInfo);

        Author Update(Author authorInfo);

        Task<Author> UpdateAsync(Author authorInfo);

        /// <summary>
        /// Set the flag to deactivate author to make it invisible for system.
        /// author may associated with note so we not want to delete everything
        /// </summary>
        /// <param name="id">The id of author whose activate flag will be set</param>
        void DeActivate(Guid id);

        Task DeActivateAsync(Guid id);

        ProcessingResult ProcessResult { get; }
    }
}