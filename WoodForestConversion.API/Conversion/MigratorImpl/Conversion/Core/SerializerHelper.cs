using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVPSI.JAMS;
using WoodForestConversion.API.Conversion.ConversionBase;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Conversion.Core
{
    internal static class SerializerHelper
    {
        internal static void Serialize<T>(IEnumerable<T> list, string name = null)
        {
            if (string.IsNullOrWhiteSpace(name)) name = typeof(T).Name;
            Directory.CreateDirectory($@"{ConversionBaseHelper.XmlOutputLocation}\{name}\");
            JAMSXmlSerializer.WriteXml(list, $@"{ConversionBaseHelper.XmlOutputLocation}\{name}\{name}.xml");
        }
    }
}
