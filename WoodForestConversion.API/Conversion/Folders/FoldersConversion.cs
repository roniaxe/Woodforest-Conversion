using Migrator.Interfaces;
using MVPSI.JAMS;
using System.Collections.Generic;
using System.IO;
using WoodForestConversion.API.Conversion.ConversionBase;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.Category;

namespace WoodForestConversion.API.Conversion.Folders
{
    public class FoldersConversion : IConverter
    {
        public ICategoryRepository CategoryRepository { get; }
        private readonly TextWriter _logger;
        public FoldersConversion(TextWriter logWriter, ICategoryRepository categoryRepository)
        {
            CategoryRepository = categoryRepository;
            _logger = logWriter;
        }

        public void Convert()
        {
            var categories = CategoryRepository.GetAll();
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
