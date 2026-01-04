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

    public ContactManager(IRepository<ContactDao> contactRepository, IMapper mapper, IEntityLookup lookup)
    {
        ArgumentNullException.ThrowIfNull(contactRepository);
        ArgumentNullException.ThrowIfNull(mapper);
        ArgumentNullException.ThrowIfNull(lookup);
        _contactDaoRepository = contactRepository;
        _mapper = mapper;
        _validator = new ContactValidator();
        _lookup = lookup;
    }

    public async Task<ProcessingResult<PageList<Contact>>> GetContactsAsync(Expression<Func<Contact, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
    {
        try
        {
            Expression<Func<ContactDao, bool>> isActivatedExpression = t => t.IsActivated;
            Expression<Func<ContactDao, bool>> daoQuery = null;
            if (query != null)
            {
                var mappedQuery = ExpressionMapper<Contact, ContactDao>.MapExpression(query);

                // Combine the mapped query with the IsActivated expression
                var parameter = Expression.Parameter(typeof(ContactDao), "c");
                var body = Expression.AndAlso(
                    Expression.Invoke(mappedQuery, parameter),
                    Expression.Invoke(isActivatedExpression, parameter)
                );
                daoQuery = Expression.Lambda<Func<ContactDao, bool>>(body, parameter);
            }
            else
            {
                daoQuery = isActivatedExpression;
            }

            var contactDaos = await _contactDaoRepository.GetEntitiesAsync(daoQuery, resourceCollectionParameters);
            var contacts = _mapper.Map<PageList<Contact>>(contactDaos);
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

        var contact = _mapper.Map<Contact>(contactDao);
        if (contact == null)
        {
            return ProcessingResult<Contact>.Fail("Cannot convert ContactDao to Contact");
        }

        return ProcessingResult<Contact>.Ok(contact);
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

            var contactDao = _mapper.Map<ContactDao>(contactInfo);
            if (contactDao == null)
            {
                return ProcessingResult<Contact>.Fail("Cannot convert Contact to ContactDao");
            }

            var addedContactDaoResult = await _contactDaoRepository.AddAsync(contactDao);
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

            var contactDao = _mapper.Map<ContactDao>(contactInfo);
            if (contactDao == null)
            {
                return ProcessingResult<Contact>.Fail("Cannot convert Contact to ContactDao");
            }

            var savedContactResult = await _lookup.GetEntityAsync<ContactDao>(contactInfo.Id);
            if (!savedContactResult.Success)
            {
                return ProcessingResult<Contact>.NotFound($"Cannot update contact: {contactInfo.Id}, because system cannot find it in data source");
            }

            var updatedContactDaoResult = await _contactDaoRepository.UpdateAsync(contactDao);
            if (!updatedContactDaoResult.Success)
            {
                return ProcessingResult<Contact>.Fail(updatedContactDaoResult.ErrorMessage, updatedContactDaoResult.ErrorType);
            }

            var updatedContact = _mapper.Map<Contact>(updatedContactDaoResult.Value);
            if (updatedContact == null)
            {
                return ProcessingResult<Contact>.Fail("Cannot convert ContactDao to Contact");
            }

            return ProcessingResult<Contact>.Ok(updatedContact);
        }
        catch (Exception ex)
        {
            return ProcessingResult<Contact>.FromException(ex);
        }
    }

    public async Task<ProcessingResult<Unit>> DeActivateAsync(int id)
    {
        try
        {
            var contactResult = await _contactDaoRepository.GetEntityAsync(id);
            if (!contactResult.Success)
            {
                return ProcessingResult<Unit>.NotFound($"Cannot find contact with id: {id}");
            }

            var contact = contactResult.Value;
            if (!contact.IsActivated)
            {
                return ProcessingResult<Unit>.Ok(Unit.Value, $"Contact with id {id} is already deactivated");
            }

            contact.IsActivated = false;
            var updatedResult = await _contactDaoRepository.UpdateAsync(contact);

            if (!updatedResult.Success)
            {
                return ProcessingResult<Unit>.Fail("Failed to deactivate contact");
            }

            return ProcessingResult<Unit>.Ok(Unit.Value, $"Contact with id {id} has been deactivated");
        }
        catch (Exception ex)
        {
            return ProcessingResult<Unit>.FromException(ex);
        }
    }
}