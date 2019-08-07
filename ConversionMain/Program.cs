using LightInject;
using System;
using System.IO;
using Serilog;
using WoodForestConversion.API.Conversion.MigratorImpl.Conversion.Agent;
using WoodForestConversion.API.Conversion.MigratorImpl.Conversion.Folder;
using WoodForestConversion.API.Conversion.MigratorImpl.Conversion.Job;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.Category;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.Condition;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.ConditionSet;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.ExecutionModule;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.Job;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.JobService;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.JobStep;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.Keyword;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.ServiceModule;

namespace WoodForest.Conversion
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console()
                    .WriteTo.File($"WNBLog_{DateTime.Now}.log", rollingInterval: RollingInterval.Day)
                    .CreateLogger();

                using (var container = CreateAndRegisterContainer())
                {
                    var jobConverter = new JobConversion(Log.Logger, container);
                    var agentConverter = new AgentConversion(Log.Logger, container);
                    var folderConverter = new FoldersConversion(Log.Logger, container);

                    agentConverter.Convert();
                    folderConverter.Convert();
                    jobConverter.Convert();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                Console.ReadKey();
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static ServiceContainer CreateAndRegisterContainer()
        {
            var container = new ServiceContainer();
            container.SetDefaultLifetime<PerContainerLifetime>();
            container.Register<IJobRepository, JobRepository>();
            container.Register<ICategoryRepository, CategoryRepository>();
            container.Register<IConditionRepository, ConditionRepository>();
            container.Register<IConditionSetRepository, ConditionSetRepository>();
            container.Register<IExecutionModuleRepository, ExecutionModuleRepository>();
            container.Register<IJobServiceRepository, JobServiceRepository>();
            container.Register<IJobStepRepository, JobStepRepository>();
            container.Register<IKeywordRepository, KeywordRepository>();
            container.Register<IServiceModuleRepository, ServiceModuleRepository>();
            return container;
        }
    }
}
