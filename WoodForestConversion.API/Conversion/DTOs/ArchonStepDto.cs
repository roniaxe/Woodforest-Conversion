using System;
using WoodForestConversion.Data;

namespace WoodForestConversion.API.Conversion.DTOs
{
    public class ArchonStepDto
    {
        public string ArchonStepName { get; internal set; }
        public string ArchonConfiguration { get; internal set; }
        public Guid ParentTaskID { get; internal set; }
        public string DisplayTitle { get; internal set; }
        public ExecutionModule ExecutionModule { get; internal set; }
        public string FixedConfiguration { get; internal set; }
    }
}
