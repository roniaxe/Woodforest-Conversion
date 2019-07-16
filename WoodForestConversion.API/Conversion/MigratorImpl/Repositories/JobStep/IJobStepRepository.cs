using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Migrator.Interfaces;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Repositories.JobStep
{
    public interface IJobStepRepository : IRepository<Data.JobStep, Guid>
    {
        IEnumerable<Data.JobStep> GetAllLive();
    }
}
