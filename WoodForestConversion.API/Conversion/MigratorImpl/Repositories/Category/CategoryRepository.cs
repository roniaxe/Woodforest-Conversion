using System;
using System.Data.Entity;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Repositories.Category
{
    public class CategoryRepository : EntityFrameworkGenericRepository<Data.Category, Guid>, ICategoryRepository
    {
    }
}
