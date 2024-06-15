using Hmm.Utility.Currency;
using Hmm.Utility.MeasureUnit;
using System;
using System.Collections.Generic;

namespace Hmm.Automobile.DomainEntity
{
    public class GasLog : AutomobileBase
    {
        public DateTime Date { get; set; }

        public AutomobileInfo Car { get; set; }

        public Dimension Distance { get; set; }

        public Dimension CurrentMeterReading { get; set; }

        public Volume Gas { get; set; }

        public Money Price { get; set; }

        public List<GasDiscountInfo> Discounts { get; set; }

        public string Station { get; set; }

        public DateTime CreateDate { get; set; }

        public string Comment { get; set; }

        /// <summary>
        /// The method is used to get gas log subject. Hmm, is using <see cref="AutomobileConstant"/> to get base subject for
        /// note content, however we also need a way to quickly find out the gas log of each automobile, so we add automobile
        /// id to gas log subject. e.g. GasLog,AutomobileId:1, this way Hmm can search database subject by note catalog and
        /// subject to retrieve all gas log of the automobile
        /// </summary>
        /// <param name="automobileId">The id of automobile which this log belongs to</param>
        /// <returns></returns>
        public static string GetNoteSubject(int automobileId) =>
            $"{AutomobileConstant.GasLogRecordSubject},AutomobileId:{automobileId}";
    }
}