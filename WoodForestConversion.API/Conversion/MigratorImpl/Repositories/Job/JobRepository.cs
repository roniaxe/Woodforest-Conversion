using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Repositories.Job
{
    public class JobRepository : EntityFrameworkGenericRepository<Data.Job, Guid>, IJobRepository
    {
        public IEnumerable<Data.Job> GetAllLive()
        {
            return GetAll().Where(job => job.IsLive && !job.IsDeleted);
        }
    }
}
