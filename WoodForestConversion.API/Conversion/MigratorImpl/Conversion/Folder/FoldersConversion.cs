using LightInject;
using MVPSI.JAMS;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using WoodForestConversion.API.Conversion.ConversionBase;
using WoodForestConversion.API.Conversion.MigratorImpl.Conversion.Abstract;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.Category;
using WoodForestConversion.Data;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Conversion.Folder
{
    public class FoldersConversion : AbstractConverter
    {
        public FoldersConversion(TextWriter log, ServiceContainer container) : base(log, container)
        {
            Container.Register<DbContext, ARCHONEntities>((factory, context) => new ARCHONEntities());
        }
        public override void Convert()
        {
            var categories = Container.TryGetInstance<ICategoryRepository>().GetAll();
            var convertedFolders = new List<MVPSI.JAMS.Folder>();

            foreach (var category in categories)
            {
                MVPSI.JAMS.Folder folder = new MVPSI.JAMS.Folder
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
