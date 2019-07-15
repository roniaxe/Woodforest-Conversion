using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Repositories.ConditionSet
{
    class ConditionSetRepository : EntityFrameworkGenericRepository<Data.ConditionSet, Guid>, IConditionSetRepository
    {
        public ConditionSetRepository(DbContext context) : base(context)
        {
        }

        public IEnumerable<Data.ConditionSet> GetAllLive()
        {
            return GetAll().Where(set => set.IsLive);
        }
    }
}
