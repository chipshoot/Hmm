//// Ignore Spelling: Repo

//using Hmm.Utility.Dal.Query;
//using Hmm.Utility.Dal.Repository;
//using Hmm.Utility.Misc;
//using Hmm.Utility.Validation;
//using System;
//using System.Linq.Expressions;
//using System.Threading.Tasks;

//namespace Hmm.Core.DefaultManager
//{
//    public class AuthorManager : IAuthorManager
//    {
//        private readonly IRepository<AuthorDao> _authorRepo;
//        private readonly IHmmValidator<AuthorDao> _validator;

//        public AuthorManager(IRepository<AuthorDao> authorRepo, IHmmValidator<AuthorDao> validator)
//        {
//            Guard.Against<ArgumentNullException>(authorRepo == null, nameof(authorRepo));
//            Guard.Against<ArgumentNullException>(validator == null, nameof(validator));
//            _authorRepo = authorRepo;
//            _validator = validator;
//        }

//        public AuthorDao Create(AuthorDao authorInfo)
//        {
//            // reset user id to apply unique account name validation
//            authorInfo.Id = 0;
//            if (!_validator.IsValidEntity(authorInfo, ProcessResult))
//            {
//                return null;
//            }

//            try
//            {
//                var addedUsr = _authorRepo.Add(authorInfo);
//                return addedUsr;
//            }
//            catch (Exception ex)
//            {
//                ProcessResult.WrapException(ex);
//                return null;
//            }
//        }

//        public async Task<AuthorDao> CreateAsync(AuthorDao authorInfo)
//        {
//            // reset user id to apply unique account name validation
//            authorInfo.Id = 0;
//            if (!_validator.IsValidEntity(authorInfo, ProcessResult))
//            {
//                return null;
//            }

//            try
//            {
//                var addedUsr = await _authorRepo.AddAsync(authorInfo);
//                return addedUsr;
//            }
//            catch (Exception ex)
//            {
//                ProcessResult.WrapException(ex);
//                return null;
//            }
//        }

//        public bool AuthorExists(string id)
//        {
//            Guard.Against<ArgumentNullException>(string.IsNullOrEmpty(id), nameof(id));

//            if (!int.TryParse(id, out var userId))
//            {
//                throw new ArgumentOutOfRangeException(nameof(id));
//            }

//            return _authorRepo.GetEntity(userId) != null;
//        }

//        public AuthorDao GetAuthorById(int id)
//        {
//            var author = _authorRepo.GetEntity(id);
//            if (author == null)
//            {
//                ProcessResult.PropagandaResult(_authorRepo.ProcessMessage);
//            }

//            return author;
//        }

//        public async Task<AuthorDao> GetAuthorByIdAsync(int id)
//        {
//            var author = await _authorRepo.GetEntityAsync(id);
//            if (author == null)
//            {
//                ProcessResult.PropagandaResult(_authorRepo.ProcessMessage);
//            }

//            return author;
//        }

//        public async Task<bool> AuthorExistsAsync(string id)
//        {
//            Guard.Against<ArgumentNullException>(string.IsNullOrEmpty(id), nameof(id));

//            if (!int.TryParse(id, out var userId))
//            {
//                throw new ArgumentOutOfRangeException(nameof(id));
//            }

//            var author = await _authorRepo.GetEntityAsync(userId);
//            return author != null;
//        }

//        public AuthorDao Update(AuthorDao authorInfo)
//        {
//            try
//            {
//                if (!_validator.IsValidEntity(authorInfo, ProcessResult))
//                {
//                    return null;
//                }

//                if (!AuthorExists(authorInfo.Id.ToString()))
//                {
//                    ProcessResult.AddErrorMessage($"Cannot update author: {authorInfo.AccountName}, because system cannot find it in data source");
//                    return null;
//                }
//                var updatedUser = _authorRepo.Update(authorInfo);
//                if (updatedUser == null)
//                {
//                    ProcessResult.PropagandaResult(_authorRepo.ProcessMessage);
//                }

//                return updatedUser;
//            }
//            catch (Exception ex)
//            {
//                ProcessResult.WrapException(ex);
//                return null;
//            }
//        }

//        public async Task<AuthorDao> UpdateAsync(AuthorDao authorInfo)
//        {
//            try
//            {
//                if (!_validator.IsValidEntity(authorInfo, ProcessResult))
//                {
//                    return null;
//                }

//                var authorExists = await AuthorExistsAsync(authorInfo.Id.ToString());
//                if (!authorExists)
//                {
//                    ProcessResult.AddErrorMessage($"Cannot update author: {authorInfo.AccountName}, because system cannot find it in data source");
//                    return null;
//                }
//                var updatedUser = await _authorRepo.UpdateAsync(authorInfo);
//                if (updatedUser == null)
//                {
//                    ProcessResult.PropagandaResult(_authorRepo.ProcessMessage);
//                }

//                return updatedUser;
//            }
//            catch (Exception ex)
//            {
//                ProcessResult.WrapException(ex);
//                return null;
//            }
//        }

//        public PageList<AuthorDao> GetEntities(Expression<Func<AuthorDao, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
//        {
//            try
//            {
//                var authors = _authorRepo.GetEntities(query, resourceCollectionParameters);

//                return authors;
//            }
//            catch (Exception ex)
//            {
//                ProcessResult.WrapException(ex);
//                return null;
//            }
//        }

//        public async Task<PageList<AuthorDao>> GetEntitiesAsync(Expression<Func<AuthorDao, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
//        {
//            try
//            {
//                var authors = await _authorRepo.GetEntitiesAsync(query, resourceCollectionParameters);
//                return authors;
//            }
//            catch (Exception ex)
//            {
//                ProcessResult.WrapException(ex);
//                return null;
//            }
//        }

//        public void DeActivate(int id)
//        {
//            var user = _authorRepo.GetEntity(id);
//            if (user == null)
//            {
//                ProcessResult.Success = false;
//                ProcessResult.AddErrorMessage($"Cannot find user with id : {id}", true);
//            }
//            else if (user.IsActivated)
//            {
//                try
//                {
//                    user.IsActivated = false;
//                    _authorRepo.Update(user);
//                }
//                catch (Exception ex)
//                {
//                    ProcessResult.WrapException(ex);
//                }
//            }
//        }

//        public async Task DeActivateAsync(int id)
//        {
//            var user = await _authorRepo.GetEntityAsync(id);
//            if (user == null)
//            {
//                ProcessResult.Success = false;
//                ProcessResult.AddErrorMessage($"Cannot find user with id : {id}", true);
//            }
//            else if (user.IsActivated)
//            {
//                try
//                {
//                    user.IsActivated = false;
//                    await _authorRepo.UpdateAsync(user);
//                }
//                catch (Exception ex)
//                {
//                    ProcessResult.WrapException(ex);
//                }
//            }
//        }

//        public ProcessingResult ProcessResult { get; } = new();
//    }
//}