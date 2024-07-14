namespace Hmm.Core.DomainEntity
{

    public class Email
    {
        public string Address { get; set; }
        public EmailType Type { get; set; }
        public bool IsPrimary { get; set; }
    }
     
    public enum EmailType
    {
        Personal,
        Work,
        Other
    }
}
