using System;
using System.Data.Entity;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Repositories.ExecutionModule
{
    public class ExecutionModuleRepository : EntityFrameworkGenericRepository<Data.ExecutionModule, Guid>, IExecutionModuleRepository
    {
    }
}
