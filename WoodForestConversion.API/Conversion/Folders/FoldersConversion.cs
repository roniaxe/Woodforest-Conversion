using System.Collections.Generic;
using System.IO;
using Migrator.Interfaces;
using MVPSI.JAMS;
using WoodForestConversion.API.Conversion.ConversionBase;
using WoodForestConversion.Data;

namespace WoodForestConversion.API.Conversion.Folders
{
    public class FoldersConversion : IConverter
    {
        private readonly TextWriter _logger;
        public FoldersConversion(TextWriter logWriter)
        {
            _logger = logWriter;
        }

        public void Convert()
        {
            using (var archonEntities = new ARCHONEntities())
            {
                var categories = archonEntities.Categories;
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
}
