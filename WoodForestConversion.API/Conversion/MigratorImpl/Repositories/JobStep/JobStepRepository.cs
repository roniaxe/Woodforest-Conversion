using System;
using System.Collections.Generic;
using System.Linq;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Repositories.JobStep
{
    public class JobStepRepository : EntityFrameworkGenericRepository<Data.JobStep, Guid>, IJobStepRepository
    {
        public IEnumerable<Data.JobStep> GetAllLive()
        {
            return GetAll().Where(step => step.IsLive && !step.IsDeleted);
        }
    }
}
