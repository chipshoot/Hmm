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
    private readonly ValidatorBase<Contact> _validator;
    private readonly IEntityLookup _lookup;

    public ContactManager(IRepository<ContactDao> contactRepository, IMapper mapper, IEntityLookup lookup)
    {
        Guard.Against<ArgumentNullException>(contactRepository == null, nameof(contactRepository));
        Guard.Against<ArgumentNullException>(mapper == null, nameof(mapper));
        Guard.Against<ArgumentNullException>(lookup == null, nameof(lookup));
        _contactDaoRepository = contactRepository;
        _mapper = mapper;
        _validator = new ContactValidator();
        _lookup = lookup;
    }

    public async Task<PageList<Contact>> GetContactsAsync(Expression<Func<Contact, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
    {
        try
        {
            ProcessResult.Rest();
            Expression<Func<ContactDao, bool>> isActivatedExpression = t => t.IsActivated;
            Expression<Func<ContactDao, bool>> daoQuery = null;
            if (query != null)
            {
                var mappedQuery = ExpressionMapper<Contact, ContactDao>.MapExpression(query);

                // Combine the mapped query with the IsActivated expression
                var parameter = Expression.Parameter(typeof(ContactDao), "c");
                var body = Expression.AndAlso(
                    Expression.Invoke(mappedQuery, parameter),
                    Expression.Invoke(isActivatedExpression, parameter));
                daoQuery = Expression.Lambda<Func<ContactDao, bool>>(body, parameter);
            }
            else
            {
                daoQuery = isActivatedExpression;
            }

            var contactDaos = await _contactDaoRepository.GetEntitiesAsync(daoQuery, resourceCollectionParameters);

            var contacts = _mapper.Map<List<Contact>>(contactDaos);

            var contactPage = resourceCollectionParameters == null
                ? new PageList<Contact>(contacts, 1, 0, contacts.Count)
                : new PageList<Contact>(contacts, 1, resourceCollectionParameters.PageNumber,
                    resourceCollectionParameters.PageSize);
            return contactPage;
        }
        catch (Exception ex)
        {
            ProcessResult.WrapException(ex);
            return null;
        }
    }

    public async Task<Contact> GetContactByIdAsync(int id)
    {
        try
        {
            ProcessResult.Rest();
            var contactDao = await _lookup.GetEntityAsync<ContactDao>(id);

            switch (contactDao)
            {
                case null:
                    return null;

                case { IsActivated: false }:
                    return null;

                default:
                    {
                        var contact = _mapper.Map<Contact>(contactDao);
                        return contact;
                    }
            }
        }
        catch (Exception ex)
        {
            ProcessResult.WrapException(ex);
            return null;
        }
    }

    public async Task<bool> IsContactExistsAsync(int id)
    {
        try
        {
            var contactDao = await _lookup.GetEntityAsync<ContactDao>(id);
            return contactDao != null;
        }
        catch (Exception ex)
        {
            ProcessResult.WrapException(ex);
            return false;
        }
    }

    public async Task<Contact> CreateAsync(Contact contactInfo)
    {
        try
        {
            ProcessResult.Rest();
            var isValid = await _validator.IsValidEntityAsync(contactInfo, ProcessResult);
            if (!isValid)
            {
                return null;
            }

            var contactDao = _mapper.Map<ContactDao>(contactInfo);
            if (contactDao == null)
            {
                ProcessResult.AddErrorMessage("Cannot convert Contact to ContactDao");
                return null;
            }

            var addedContact = await _contactDaoRepository.AddAsync(contactDao);
            if (addedContact == null)
            {
                ProcessResult.PropagandaResult(_contactDaoRepository.ProcessMessage);
                return null;
            }

            contactInfo.Id = addedContact.Id;
            return contactInfo;
        }
        catch (Exception ex)
        {
            ProcessResult.WrapException(ex);
            return null;
        }
    }

    public async Task<Contact> UpdateAsync(Contact contactInfo)
    {
        if (contactInfo == null)
        {
            return null;
        }

        ProcessResult.Rest();
        if (!await _validator.IsValidEntityAsync(contactInfo, ProcessResult))
        {
            return null;
        }

        var savedContact = await _lookup.GetEntityAsync<ContactDao>(contactInfo.Id);
        if (savedContact == null)
        {
            ProcessResult.AddErrorMessage($"Cannot find contact: {contactInfo.Id} for updating");
            return null;
        }

        // update Contact record
        var contactDao = _mapper.Map<ContactDao>(contactInfo);
        if (contactDao == null)
        {
            ProcessResult.AddErrorMessage("Cannot convert Contact to ContactDao");
            return null;
        }

        var updatedContactDao = await _contactDaoRepository.UpdateAsync(contactDao);
        if (updatedContactDao == null)
        {
            ProcessResult.PropagandaResult(_contactDaoRepository.ProcessMessage);
        }

        var updatedContact = _mapper.Map<Contact>(updatedContactDao);
        return updatedContact;
    }

    public async Task DeActivateAsync(int id)
    {
        ProcessResult.Rest();
        var contact = await _contactDaoRepository.GetEntityAsync(id);
        if (contact == null)
        {
            ProcessResult.Success = false;
            ProcessResult.AddErrorMessage($"Cannot find user with id : {id}", true);
        }
        else if (contact.IsActivated)
        {
            try
            {
                contact.IsActivated = false;
                await _contactDaoRepository.UpdateAsync(contact);
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
            }
        }
    }

    public ProcessingResult ProcessResult { get; } = new();
}