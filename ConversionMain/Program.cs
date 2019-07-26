using System;
using System.Data.Entity;
using System.IO;
using LightInject;
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
using WoodForestConversion.Data;

namespace WoodForest.Conversion
{
    public class Program
    {
        public static void Main(string[] args)
        {
            TextWriter logWriter = null;
            try
            {
                logWriter = File.CreateText("log.txt");
                var container = CreateAndRegisterContainer();

                var jobConverter = new JobConversion(logWriter, container);
                var agentConverter = new AgentConversion(logWriter, container);
                var folderConverter = new FoldersConversion(logWriter, container);

                agentConverter.Convert();
                folderConverter.Convert();
                jobConverter.Convert();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadKey();
            }
            finally
            {
                logWriter?.Dispose();
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
