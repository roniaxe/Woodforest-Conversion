using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Archon.Tasks;

namespace WoodForestConversion.API.Conversion.Enums
{
    internal static class CategoryConverter
    {
        internal static CategoryName FromGUID(Guid? categoryGuid)
        {
            switch (categoryGuid)
            {
                case var cGuid when cGuid == new Guid("9D7D40A4-0D29-4D3A-904E-203FB1BDF5CF"):
                    return CategoryName.Sandbox;
                case var cGuid when cGuid == new Guid("A91797A7-CCC6-4CFA-A7F5-4483FD2EA795"):
                    return CategoryName.Bank_All;
                case var cGuid when cGuid == new Guid("88BEE0E8-222C-4F08-8E50-4FF6111F2FA3"):
                    return CategoryName.Operations_Tasks;
                case var cGuid when cGuid == new Guid("320146A1-17B8-4D02-BB72-6EC175D0DDF4"):
                    return CategoryName.Database_Maintenance;
                case var cGuid when cGuid == new Guid("C2852699-F90C-487F-B96B-73944BC70FC9"):
                    return CategoryName.Bank_Legacy;
                case var cGuid when cGuid == new Guid("8CC32299-451D-4522-B772-7F22CF32C5D9"):
                    return CategoryName.Bank_WLI;
                case var cGuid when cGuid == new Guid("1B893315-2463-4198-945C-80FDBB431037"):
                    return CategoryName.Bank_FSB;
                case var cGuid when cGuid == new Guid("DE361193-0191-4CDC-9BED-9A9BFB16C668"):
                    return CategoryName.CommVault_Backup;
                case var cGuid when cGuid == new Guid("C0D14FAB-309A-4988-863A-C990F2FB0324"):
                    return CategoryName.Bank_WNB;
                case var cGuid when cGuid == new Guid("66AC054C-67A9-413B-A0EB-EB9418554E06"):
                    return CategoryName.Misc_Tasks;
                default:
                    return CategoryName.Unknown;
            }
        }
    }
}