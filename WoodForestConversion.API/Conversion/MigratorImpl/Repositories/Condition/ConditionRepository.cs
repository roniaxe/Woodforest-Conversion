﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Repositories.Condition
{
    public class ConditionRepository : EntityFrameworkGenericRepository<Data.Condition, Guid>, IConditionRepository
    {
        public IEnumerable<Data.Condition> GetAllLive()
        {
            return GetAll().Where(condition => condition.IsLive);
        }
    }
}
