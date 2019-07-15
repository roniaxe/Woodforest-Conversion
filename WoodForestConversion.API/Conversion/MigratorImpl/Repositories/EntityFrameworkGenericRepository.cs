﻿using Migrator.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Repositories
{
    public class EntityFrameworkGenericRepository<TSource, TPrimaryKeyTpe> : IRepository<TSource, TPrimaryKeyTpe> where TSource : class
    {
        public DbContext DbContext { get; }
        public EntityFrameworkGenericRepository(DbContext context)
        {
            DbContext = context;
        }

        public IEnumerable<TSource> GetAll()
        {
            return DbContext.Set<TSource>();
        }

        public IEnumerable<TSource> Find(Expression<Func<TSource, bool>> predicate)
        {
            return DbContext.Set<TSource>().Where(predicate);
        }

        public TSource GetById(TPrimaryKeyTpe id)
        {
            return DbContext.Set<TSource>().Find(id);
        }
    }
}
