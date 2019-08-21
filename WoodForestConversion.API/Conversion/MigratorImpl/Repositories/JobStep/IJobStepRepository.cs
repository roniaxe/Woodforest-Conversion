using System;
using System.Collections.Generic;
using Migrator.Interfaces;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Repositories.JobStep
{
    public interface IJobStepRepository : IRepository<Data.JobStep, Guid>
    {
        IEnumerable<Data.JobStep> GetAllLive();
    }
}
