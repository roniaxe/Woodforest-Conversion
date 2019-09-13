using System;
using LightInject;
using MVPSI.JAMS;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using Serilog;
using WoodForestConversion.API.Conversion.ConversionBase;
using WoodForestConversion.API.Conversion.MigratorImpl.Conversion.Abstract;
using WoodForestConversion.API.Conversion.MigratorImpl.Conversion.Core;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.Category;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.JobService;
using WoodForestConversion.Data;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Conversion.Folder
{
    public class FoldersConversion : AbstractConverter<Data.Category, MVPSI.JAMS.Folder>
    {
        public FoldersConversion(ServiceContainer container) : base(container)
        {
            Source = Container.GetInstance<ICategoryRepository>().GetAll();
        }
        public override void Convert()
        {
            try
            {
                foreach (var category in Source)
                {
                    var newFolder = GetInstance();

                    newFolder.FolderName = category.CategoryName;
                    newFolder.Properties.SetValue("Enabled", true);

                    Target.Add(newFolder);
                }

                SerializerHelper.Serialize(Target);
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, exception.Message);
                throw;
            }
        }
    }
}
