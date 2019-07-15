using System;
using System.Data.Entity;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Repositories.Category
{
    public class CategoryRepository : EntityFrameworkGenericRepository<Data.Category, Guid>, ICategoryRepository
    {
        public CategoryRepository(DbContext context) : base(context)
        {
        }
    }
}
