namespace Hmm.Core.Map.DomainEntity;

public class Contact : EntityBase
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public IEnumerable<Email> Emails { get; set; }
    public IEnumerable<Phone> Phones { get; set; }
    public IEnumerable<AddressInfo> Addresses { get; set; }
    public bool IsActivated { get; set; }
}