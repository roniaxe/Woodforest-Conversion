using System.Configuration;

namespace WoodForestConversion.API.Conversion.ConversionBase
{
    public class ConversionBaseHelper
    {
        public static readonly string JamsArchonRootFolder = ConfigurationManager.AppSettings["jams_archon_root_foldername"];
        public static readonly string XmlOutputLocation = ConfigurationManager.AppSettings["xml_output_location"];
    }
}
