using Hmm.Utility.MeasureUnit;
using System.Xml.Linq;

namespace Hmm.Utility.Misc
{
    public interface IMeasureXmlSerializable<out T>
    {
        XElement Measure2Xml(XNamespace ns);

        T Xml2Measure(XElement xmlContent);
    }
}