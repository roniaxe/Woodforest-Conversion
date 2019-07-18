using LightInject;
using Migrator.Interfaces;
using MVPSI.JAMS;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using WoodForestConversion.API.Conversion.ConversionBase;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.Category;
using WoodForestConversion.Data;

namespace WoodForestConversion.API.Conversion.Folders
{
    public class FoldersConversion : IConverter
    {
        private readonly TextWriter _logger;
        private ServiceContainer _container;
        public FoldersConversion(TextWriter logWriter)
        {
            _logger = logWriter;
            CreateContainer();
        }

        private void CreateContainer()
        {
            _container = new ServiceContainer();
            _container.Register<DbContext, ARCHONEntities>();
            _container.Register<ICategoryRepository, CategoryRepository>();
        }
        public void Convert()
        {
            var categories = _container.TryGetInstance<ICategoryRepository>().GetAll();
            var convertedFolders = new List<Folder>();

            foreach (var category in categories)
            {
                Folder folder = new Folder
                {
                    FolderName = category.CategoryName
                };
                convertedFolders.Add(folder);
            }

            Directory.CreateDirectory($@"{ConversionBaseHelper.XmlOutputLocation}\Folders\");
            JAMSXmlSerializer.WriteXml(convertedFolders, $@"{ConversionBaseHelper.XmlOutputLocation}\Folders\Folders.xml");
        }
    }
}
