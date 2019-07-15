using System;
using System.Data.Entity;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Repositories.ServiceModule
{
    public class ServiceModuleRepository : EntityFrameworkGenericRepository<Data.ServiceModule, Guid>, IServiceModuleRepository
    {
        public ServiceModuleRepository(DbContext context) : base(context)
        {
        }
    }
}
