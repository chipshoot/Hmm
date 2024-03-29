﻿using Hmm.Automobile.DomainEntity;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.MeasureUnit;
using Hmm.Utility.Validation;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Hmm.Automobile.NoteSerializer
{
    public class GasLogXmlNoteSerializer : EntityXmlNoteSerializerBase<GasLog>
    {
        private const string TimeStampFormatString = "yyyyMMddHHmmssffff";
        private const string XmlTimeStampFormatString = "yyyy-MM-ddTHH:mm:ssZ";
        private readonly IApplication _app;
        private readonly IAutoEntityManager<AutomobileInfo> _autoManager;
        private readonly IAutoEntityManager<GasDiscount> _discountManager;
        private readonly IEntityLookup _lookupRepo;

        public GasLogXmlNoteSerializer(
            IApplication app,
            ILogger<GasLog> logger,
            IAutoEntityManager<AutomobileInfo> autoManager,
            IAutoEntityManager<GasDiscount> discountManager,
            IEntityLookup lookupRepo)
            : base(logger)
        {
            Guard.Against<ArgumentNullException>(app == null, nameof(app));
            Guard.Against<ArgumentNullException>(autoManager == null, nameof(autoManager));
            Guard.Against<ArgumentNullException>(discountManager == null, nameof(discountManager));
            Guard.Against<ArgumentNullException>(lookupRepo == null, nameof(lookupRepo));

            _app = app;
            _autoManager = autoManager;
            _discountManager = discountManager;
            _lookupRepo = lookupRepo;
        }

        public override GasLog GetEntity(HmmNote note)
        {
            try
            {
                var (gasLogRoot, ns) = GetEntityRoot(note, AutomobileConstant.GasLogRecordSubject);
                if (gasLogRoot == null)
                {
                    return null;
                }
                _ = int.TryParse(gasLogRoot.Element(ns + "Automobile")?.Value, out var carId);
                var car = _autoManager.GetEntityById(carId);

                var gasLog = new GasLog
                {
                    Date = GetDate(gasLogRoot.Element(ns + "Date")),
                    Id = note.Id,
                    Car = car,
                    Station = gasLogRoot.Element(ns + "GasStation")?.Value,
                    Distance = gasLogRoot.Element(ns + "Distance")?.GetDimension() ?? 0.GetKilometer(),
                    CurrentMeterReading = gasLogRoot.Element(ns + "CurrentMeterReading")?.GetDimension() ?? 0.GetKilometer(),
                    Gas = gasLogRoot.Element(ns + "Gas")?.GetVolume() ?? 0d.GetDeciliter(),
                    Price = gasLogRoot.Element(ns + "Price")?.GetMoney() ?? 0m.GetCad(),
                    CreateDate = GetDate(gasLogRoot.Element(ns + "CreateDate")),
                    Comment = gasLogRoot.Element(ns + "Comment")?.Value,
                    AuthorId = note.Author.Id
                };

                var discounts = GetDiscountInfos(gasLogRoot.Element(ns + "Discounts"), ns);
                if (discounts.Any())
                {
                    gasLog.Discounts = discounts;
                }

                return gasLog;
            }
            catch (Exception e)
            {
                ProcessResult.WrapException(e);
                throw;
            }
        }

        public override string GetNoteSerializationText(GasLog entity)
        {
            if (entity == null)
            {
                return string.Empty;
            }

            var xml = new XElement(AutomobileConstant.GasLogRecordSubject,
                new XElement("Date", entity.Date.ToString("o")),
                new XElement("Distance", entity.Distance.SerializeToXml(ContentNamespace)),
                new XElement("CurrentMeterReading", entity.CurrentMeterReading.SerializeToXml(ContentNamespace)),
                new XElement("Gas", entity.Gas.SerializeToXml(ContentNamespace)),
                new XElement("Price", entity.Price.SerializeToXml(ContentNamespace)),
                new XElement("GasStation", entity.Station),
                new XElement("Discounts", ""),
                new XElement("Automobile", entity.Car.Id),
                new XElement("Comment", entity.Comment ?? ""),
                new XElement("CreateDate", entity.CreateDate.ToString("o")));

            if (entity.Discounts.Any())
            {
                foreach (var disc in entity.Discounts)
                {
                    if (disc.Amount == null || disc.Program == null)
                    {
                        ProcessResult.AddErrorMessage("Cannot found valid discount information, amount or discount program is missing");
                        continue;
                    }

                    var discElement = new XElement("Discount",
                        new XElement("Amount", disc.Amount?.SerializeToXml(ContentNamespace)),
                        new XElement("Program", disc.Program?.Id));
                    xml.Element("Discounts")?.Add(discElement);
                }
            }

            return GetNoteContent(xml).ToString(SaveOptions.DisableFormatting);
        }

        private List<GasDiscountInfo> GetDiscountInfos(XContainer discountRoot, XNamespace ns)
        {
            var infos = new List<GasDiscountInfo>();
            if (discountRoot == null)
            {
                return infos;
            }

            foreach (var element in discountRoot.Elements())
            {
                var amountNode = element.Element(ns + "Amount");
                if (amountNode == null)
                {
                    ProcessResult.AddErrorMessage("Cannot found money information from discount string");
                    continue;
                }
                var money = amountNode.GetMoney();
                var discountIdStr = element.Element(ns + "Program")?.Value;
                if (!int.TryParse(discountIdStr, out var discountId))
                {
                    ProcessResult.AddErrorMessage($"Cannot found valid discount id from string {discountIdStr}");
                    continue;
                }

                // Setup temporary discount object with right id, we cannot get discount entity right now because DB reader
                // is still opening for GasLog
                var discount = _discountManager.GetEntityById(discountId);
                if (discount == null)
                {
                    ProcessResult.AddErrorMessage($"Cannot found discount id : {discountId} from data source");
                    continue;
                }

                infos.Add(new GasDiscountInfo
                {
                    Amount = money,
                    Program = discount
                });
            }

            return infos;
        }

        private static DateTime GetDate(XElement dateNode)
        {
            var dateString = dateNode?.Value;
            DateTime logDate;
            try
            {
                logDate = DateTime.ParseExact(string.IsNullOrEmpty(dateString) ? DateTime.MinValue.ToString("o") : dateString, "o", null);
                return logDate;
            }
            catch (FormatException)
            {
                try
                {
                    logDate = DateTime.ParseExact(
                        string.IsNullOrEmpty(dateString) ? DateTime.MinValue.ToString(TimeStampFormatString) : dateString,
                        TimeStampFormatString, null);
                    return logDate;
                }
                catch (FormatException)
                {
                    logDate = DateTime.ParseExact(
                        string.IsNullOrEmpty(dateString)
                            ? DateTime.MinValue.ToString(XmlTimeStampFormatString)
                            : dateString, XmlTimeStampFormatString, null);
                    return logDate;
                }
            }
        }

        protected override NoteCatalog GetCatalog()
        {
            return _app.GetCatalog(NoteCatalogType.GasLog, _lookupRepo);
        }
    }
}