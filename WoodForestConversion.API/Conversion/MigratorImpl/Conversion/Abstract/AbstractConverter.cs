using LightInject;
using Migrator.Interfaces;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Conversion.Abstract
{
    public abstract class AbstractConverter : IConverter
    {
        protected readonly ServiceContainer Container;

        protected AbstractConverter(ServiceContainer container)
        {
            Container = container;
        }

        public abstract void Convert();
    }
}
