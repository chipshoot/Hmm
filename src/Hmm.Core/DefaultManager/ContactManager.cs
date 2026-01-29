using AutoMapper;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.Map;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core.DefaultManager;

public class ContactManager : IContactManager
{
    private readonly IRepository<ContactDao> _contactDaoRepository;
    private readonly IMapper _mapper;
    private readonly IHmmValidator<Contact> _validator;
    private readonly IEntityLookup _lookup;

    public ContactManager(IRepository<ContactDao> contactRepository, IMapper mapper, IEntityLookup lookup, IHmmValidator<Contact> validator)
    {
        ArgumentNullException.ThrowIfNull(contactRepository);
        ArgumentNullException.ThrowIfNull(mapper);
        ArgumentNullException.ThrowIfNull(lookup);
        ArgumentNullException.ThrowIfNull(validator);
        _contactDaoRepository = contactRepository;
        _mapper = mapper;
        _validator = validator;
        _lookup = lookup;
    }

    public async Task<ProcessingResult<PageList<Contact>>> GetContactsAsync(Expression<Func<Contact, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
    {
        try
        {
            // Use cached expression helper to combine query with IsActivated filter
            var daoQuery = ExpressionHelper.CombineWithIsActivated<Contact, ContactDao>(query);

            var contactDaosResult = await _contactDaoRepository.GetEntitiesAsync(daoQuery, resourceCollectionParameters);
            if (!contactDaosResult.Success)
            {
                return ProcessingResult<PageList<Contact>>.Fail(contactDaosResult.ErrorMessage, contactDaosResult.ErrorType);
            }

            var contacts = _mapper.Map<PageList<Contact>>(contactDaosResult.Value);
            return ProcessingResult<PageList<Contact>>.Ok(contacts);
        }
        catch (Exception ex)
        {
            return ProcessingResult<PageList<Contact>>.FromException(ex);
        }
    }

    public async Task<ProcessingResult<Contact>> GetContactByIdAsync(int id)
    {
        var contactDaoResult = await _lookup.GetEntityAsync<ContactDao>(id);

        if (!contactDaoResult.Success)
        {
            return ProcessingResult<Contact>.Fail(contactDaoResult.ErrorMessage, contactDaoResult.ErrorType);
        }

        var contactDao = contactDaoResult.Value;
        if (!contactDao.IsActivated)
        {
            return ProcessingResult<Contact>.Deleted($"Contact with ID {id} has been deactivated");
        }

        return _mapper.MapWithNullCheck<ContactDao, Contact>(contactDao);
    }

    public async Task<bool> IsContactExistsAsync(int id)
    {
        try
        {
            var contactDaoResult = await _lookup.GetEntityAsync<ContactDao>(id);
            return contactDaoResult.Success;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ProcessingResult<Contact>> CreateAsync(Contact contactInfo)
    {
        try
        {
            var validationResult = await _validator.ValidateEntityAsync(contactInfo);
            if (!validationResult.Success)
            {
                return ProcessingResult<Contact>.Invalid(validationResult.GetWholeMessage());
            }

            var contactDaoResult = _mapper.MapWithNullCheck<Contact, ContactDao>(contactInfo);
            if (!contactDaoResult.Success)
            {
                return ProcessingResult<Contact>.Fail(contactDaoResult.ErrorMessage);
            }

            var addedContactDaoResult = await _contactDaoRepository.AddAsync(contactDaoResult.Value);
            if (!addedContactDaoResult.Success)
            {
                return ProcessingResult<Contact>.Fail(addedContactDaoResult.ErrorMessage, addedContactDaoResult.ErrorType);
            }

            var createdContact = _mapper.Map<Contact>(addedContactDaoResult.Value);
            return ProcessingResult<Contact>.Ok(createdContact);
        }
        catch (Exception ex)
        {
            return ProcessingResult<Contact>.FromException(ex);
        }
    }

    public async Task<ProcessingResult<Contact>> UpdateAsync(Contact contactInfo)
    {
        try
        {
            var validationResult = await _validator.ValidateEntityAsync(contactInfo);
            if (!validationResult.Success)
            {
                return ProcessingResult<Contact>.Invalid(validationResult.GetWholeMessage());
            }

            var contactDaoResult = _mapper.MapWithNullCheck<Contact, ContactDao>(contactInfo);
            if (!contactDaoResult.Success)
            {
                return ProcessingResult<Contact>.Fail(contactDaoResult.ErrorMessage);
            }

            var updatedContactDaoResult = await _contactDaoRepository.UpdateAsync(contactDaoResult.Value);
            if (!updatedContactDaoResult.Success)
            {
                return ProcessingResult<Contact>.Fail(updatedContactDaoResult.ErrorMessage, updatedContactDaoResult.ErrorType);
            }

            return _mapper.MapWithNullCheck<ContactDao, Contact>(updatedContactDaoResult.Value);
        }
        catch (Exception ex)
        {
            return ProcessingResult<Contact>.FromException(ex);
        }
    }

    public Task<ProcessingResult<Unit>> DeActivateAsync(int id)
    {
        return DeactivationHelper.DeactivateAsync(
            _contactDaoRepository,
            id,
            "contact");
    }
}