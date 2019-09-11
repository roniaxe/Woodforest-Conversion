using Migrator.Interfaces;
using System;
using System.Collections.Generic;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Repositories.ConditionSet
{
    public interface IConditionSetRepository : IRepository<Data.ConditionSet, Guid>
    {
        IEnumerable<Data.ConditionSet> GetAllLive();
    }
}
