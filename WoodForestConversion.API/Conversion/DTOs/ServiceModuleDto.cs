using System;

namespace WoodForestConversion.API.Conversion.DTOs
{
    public class ServiceModuleDto
    {
        public Guid ModuleUid { get; set; }
        public string ModuleName { get; set; }
        public Guid ServiceUID { get; set; }
        public string ServiceName { get; set; }
    }
}
