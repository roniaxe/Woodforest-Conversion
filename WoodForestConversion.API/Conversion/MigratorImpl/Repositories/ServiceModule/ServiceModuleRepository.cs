using System;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Repositories.ServiceModule
{
    public class ServiceModuleRepository : EntityFrameworkGenericRepository<Data.ServiceModule, Guid>, IServiceModuleRepository
    {
    }
}
