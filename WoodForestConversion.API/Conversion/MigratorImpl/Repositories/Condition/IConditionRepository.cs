using Migrator.Interfaces;
using System;
using System.Collections.Generic;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Repositories.Condition
{
    public interface IConditionRepository : IRepository<Data.Condition, Guid>
    {
        IEnumerable<Data.Condition> GetAllLive();
    }
}
