using System.Collections.Generic;
using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Core.DomainEntity;

public class Contact : Entity
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public IEnumerable<Email> Emails { get; set; }
    public IEnumerable<Phone> Phones { get; set; }
    public IEnumerable<AddressInfo> Addresses { get; set; }
    public bool IsActivated { get; set; }
}