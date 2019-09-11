using System;
using System.Collections.Generic;
using Migrator.Interfaces;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Repositories.Job
{
    public interface IJobRepository : IRepository<Data.Job, Guid>
    {
        IEnumerable<Data.Job> GetAllLive();
    }
}
