using System;
using System.Data.Entity;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Repositories.ExecutionModule
{
    class ExecutionModuleRepository : EntityFrameworkGenericRepository<Data.ExecutionModule, Guid>, IExecutionModuleRepository
    {
        public ExecutionModuleRepository(DbContext context) : base(context)
        {
        }
    }
}
