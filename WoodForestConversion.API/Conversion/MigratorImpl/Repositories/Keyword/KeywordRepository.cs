﻿using System;
using System.Data.Entity;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Repositories.Keyword
{
    public class KeywordRepository : EntityFrameworkGenericRepository<Data.Keyword, Guid>, IKeywordRepository
    {
    }
}
