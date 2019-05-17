using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WoodForestConversion.Data;

namespace WoodForestConversion.API.Conversion.DTOs
{
    public class ArchonStepDto
    {
        public string ArchonStepName { get; set; }
        public string ArchonConfiguration { get; set; }
        public Guid ParentTaskID { get; set; }
        public string DisplayTitle { get; set; }
        public ExecutionModule ExecutionModule { get; set; }
    }
}
