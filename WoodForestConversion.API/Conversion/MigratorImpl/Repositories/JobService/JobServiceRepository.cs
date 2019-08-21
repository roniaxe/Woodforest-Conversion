using System;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Repositories.JobService
{
    public class JobServiceRepository : EntityFrameworkGenericRepository<Data.JobService, Guid>, IJobServiceRepository
    {
    }
}
