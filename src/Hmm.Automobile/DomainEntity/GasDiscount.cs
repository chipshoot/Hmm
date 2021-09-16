using Hmm.Utility.Currency;

namespace Hmm.Automobile.DomainEntity
{
    public class GasDiscount : AutomobileBase
    {
        public string Program { get; set; }

        public Money Amount { get; set; }

        public GasDiscountType DiscountType { get; set; }

        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the comment, the way of calculating
        /// discount can be added to comment, e.g. 0.2c/liter
        /// </summary>
        /// <value>
        /// The comment.
        /// </value>
        public string Comment { get; set; }
    }
}