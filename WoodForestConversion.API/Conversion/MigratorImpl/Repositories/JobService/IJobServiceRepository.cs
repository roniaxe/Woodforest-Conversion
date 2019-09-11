using System;
using Migrator.Interfaces;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Repositories.JobService
{
    public interface IJobServiceRepository : IRepository<Data.JobService, Guid>
    {
    }
}
