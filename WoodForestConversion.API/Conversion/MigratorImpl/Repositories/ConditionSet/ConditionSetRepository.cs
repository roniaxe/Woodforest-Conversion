using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Repositories.ConditionSet
{
    public class ConditionSetRepository : EntityFrameworkGenericRepository<Data.ConditionSet, Guid>, IConditionSetRepository
    {
        public IEnumerable<Data.ConditionSet> GetAllLive()
        {
            return GetAll().Where(set => set.IsLive);
        }
    }
}
