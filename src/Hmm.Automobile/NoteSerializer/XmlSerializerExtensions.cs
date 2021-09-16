using Hmm.Utility.Currency;
using Hmm.Utility.MeasureUnit;
using Hmm.Utility.Validation;
using System;
using System.Xml.Linq;

namespace Hmm.Automobile.NoteSerializer
{
    public static class XmlSerializerExtensions
    {
        public static Money GetMoney(this XElement xml)
        {
            Guard.Against<ArgumentNullException>(xml == null, nameof(xml));

            // ReSharper disable once PossibleNullReferenceException
            var ns = xml.GetDefaultNamespace();
            var root = xml.Element(ns + "Money");
            if (root == null)
            {
                throw new ArgumentException("The XML element does not contains Money element");
            }

            var dv = root.Element(ns + "Value");
            if (!double.TryParse(dv?.Value, out var value))
            {
                throw new ArgumentException("The Money XML element does not contains valid value element");
            }

            var code = root.Element(ns + "Code");
            if (string.IsNullOrEmpty(code?.Value))
            {
                throw new ArgumentException("The Money XML element does not contains code element");
            }

            if (!Enum.TryParse(code.Value, true, out CurrencyCodeType codeType))
            {
                throw new ArgumentException("The Money XML element does not contains valid code element");
            }

            var money = new Money(value, codeType);
            return money;
        }

        public static XElement SerializeToXml(this Money amount, XNamespace ns)
        {
            if (ns != null)
            {
                return new XElement(ns + "Money",
                    new XElement(ns + "Value", amount.InternalAmount),
                    new XElement(ns + "Code", amount.CurrencyCode));
            }

            return new XElement("Money",
                new XElement("Value", amount.InternalAmount),
                new XElement("Code", amount.CurrencyCode));
        }

        public static Dimension GetDimension(this XElement xmlContent)
        {
            var ns = xmlContent.GetDefaultNamespace();
            var root = xmlContent.Element(ns + "Dimension");
            if (root == null)
            {
                throw new ArgumentException("The XML element does not contains Dimension element");
            }

            var dv = GetXElement("Value", root, ns);
            if (!double.TryParse(dv?.Value, out var value))
            {
                throw new ArgumentException("The XML element does not contains valid value element");
            }

            var unit = GetXElement("Unit", root, ns);
            if (string.IsNullOrEmpty(unit?.Value))
            {
                throw new ArgumentException("The XML element does not contains unit element");
            }

            if (!Enum.TryParse(unit.Value, true, out DimensionUnit unitType))
            {
                throw new ArgumentException("The XML element does not contains unit element");
            }

            var dim = new Dimension(value, unitType);

            return dim;
        }

        public static XElement SerializeToXml(this Dimension distance, XNamespace ns)
        {
            if (ns != null)
            {
                return new XElement(ns + "Dimension",
                    new XElement(ns + "Value", distance.Value),
                    new XElement(ns + "Unit", distance.Unit));
            }

            return new XElement("Dimension",
                new XElement("Value", distance.Value),
                new XElement("Unit", distance.Unit));
        }

        public static XElement SerializeToXml(this Volume volume, XNamespace ns)
        {
            return new XElement("Volume",
                new XElement("Value", volume.Value),
                new XElement("Unit", volume.Unit));
        }

        public static Volume GetVolume(this XElement xmlContent)
        {
            var ns = xmlContent.GetDefaultNamespace();
            var root = xmlContent.Element(ns + "Volume");
            if (root == null)
            {
                throw new ArgumentException("The XML element does not contains Volume element");
            }

            var dv = GetXElement("Value", root, ns);
            if (!double.TryParse(dv?.Value, out var value))
            {
                throw new ArgumentException("The XML element does not contains valid value element");
            }

            var unit = GetXElement("Unit", root, ns);
            if (string.IsNullOrEmpty(unit?.Value))
            {
                throw new ArgumentException("The XML element does not contains unit element");
            }

            if (!Enum.TryParse(unit.Value, true, out VolumeUnit unitType))
            {
                throw new ArgumentException("The XML element does not contains unit element");
            }

            var vol = new Volume(value, unitType);
            return vol;
        }

        private static XElement GetXElement(string eName, XContainer content, XNamespace ns)
        {
            return ns != null ? content?.Element(ns + eName) : content?.Element(eName);
        }
    }
}