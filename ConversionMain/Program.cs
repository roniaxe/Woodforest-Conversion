using System;
using System.IO;
using WoodForestConversion.API.Conversion.Agents;
using WoodForestConversion.API.Conversion.Folders;
using WoodForestConversion.API.Conversion.Jobs;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.Category;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.ExecutionModule;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.Job;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.JobService;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.JobStep;
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
                using (var context = new ARCHONEntities())
                {
                    var jobRepo = new JobRepository(context);
                    var serviceModuleRepo = new ServiceModuleRepository(context);
                    var executionModuleRepo = new ExecutionModuleRepository(context);
                    var jobServiceRepo = new JobServiceRepository(context);
                    var categoryRepository = new CategoryRepository(context);
                    var jobStepRepository = new JobStepRepository(context);
                    var jobConverter = new JobConversion(
                        logWriter,
                        serviceModuleRepo,
                        executionModuleRepo,
                        jobServiceRepo,
                        jobStepRepository);
                    var agentConverter = new AgentConversion(logWriter, jobServiceRepo);
                    var folderConverter = new FoldersConversion(logWriter, categoryRepository);
                    //var queueConverter = new QueueConversion();

                    //var queues = queueConverter.Convert();
                    //jobConverter.BatchQueues = queues;
                    agentConverter.Convert();
                    folderConverter.Convert();
                    jobConverter.Convert();
                }
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
    }
}
