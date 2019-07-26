using System.Data.Entity;
using LightInject;
using Migrator.Interfaces;
using System.IO;
using WoodForestConversion.Data;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Conversion.Abstract
{
    public abstract class AbstractConverter : IConverter
    {
        protected readonly TextWriter Log;
        protected ServiceContainer Container;

        protected AbstractConverter(TextWriter log, ServiceContainer container)
        {
            Container = container;
            Log = log;
        }

        public abstract void Convert();
    }
}
