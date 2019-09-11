using System;

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
