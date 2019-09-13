using System.Collections.Generic;
using LightInject;
using Migrator.Interfaces;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Conversion.Abstract
{
    public abstract class AbstractConverter<TSource, TTarget> : IConverter where TTarget : new()
    {
        #region Properties
        protected readonly ServiceContainer Container;
        protected IEnumerable<TSource> Source { get; set; }
        protected List<TTarget> Target { get; } = new List<TTarget>();
        #endregion

        #region Constructor + Destructor
        protected AbstractConverter(ServiceContainer container)
        {
            Container = container;
        }

        ~AbstractConverter() => Container.Dispose(); 
        #endregion

        #region Methods
        public abstract void Convert();

        protected TTarget GetInstance()
        {
            return new TTarget();
        } 
        #endregion
    }
}
