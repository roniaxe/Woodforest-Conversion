using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Repositories.JobStep
{
    public class JobStepRepository : EntityFrameworkGenericRepository<Data.JobStep, Guid>, IJobStepRepository
    {
        public JobStepRepository(DbContext context) : base(context)
        {
        }

        public IEnumerable<Data.JobStep> GetAllLive()
        {
            return GetAll().Where(step => step.IsLive && !step.IsDeleted);
        }
    }
}
