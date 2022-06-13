using Hmm.Automobile.DomainEntity;

namespace Hmm.Automobile
{
    public static class AutomobileConstant
    {
        public const string AutoMobileInfoCatalogName = "Hmm.AutomobileMan.AutomobileInfo";

        public const string GasDiscountCatalogName = "Hmm.AutomobileMan.Discount";

        public const string GasLogCatalogName = "Hmm.AutomobileMan.GasLog";

        /// <summary>
        /// Base subject is used for root element of <see cref="AutomobileInfo"/> note XML content 
        /// </summary>
        public const string AutoMobileRecordSubject = "Automobile";

        /// <summary>
        /// Base subject is used for root element of <see cref="GasDiscount"/> note XML content 
        /// </summary>
        public const string GasDiscountRecordSubject = "GasDiscount";

        /// <summary>
        /// Base subject is used for root element of <see cref="GasLog"/> note XML content 
        /// </summary>
        public const string GasLogRecordSubject = "GasLog";
    }
}