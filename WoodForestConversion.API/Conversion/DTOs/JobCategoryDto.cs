using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoodForestConversion.API.Conversion.DTOs
{
    public class JobCategoryDto
    {
        public JobCategoryDto(Guid jobUid, string categoryName)
        {
            JobUid = jobUid;
            CategoryName = categoryName;
        }

        public Guid JobUid { get; set; }
        public string CategoryName { get; set; }
    }
}
