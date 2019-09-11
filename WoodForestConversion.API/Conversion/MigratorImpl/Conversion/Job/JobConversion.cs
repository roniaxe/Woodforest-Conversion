using Archon.Tasks;
using LightInject;
using MVPSI.JAMS;
using MVPSI.JAMSSequence;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Serilog;
using WoodForestConversion.API.Conversion.ConversionBase;
using WoodForestConversion.API.Conversion.DTOs;
using WoodForestConversion.API.Conversion.Enums;
using WoodForestConversion.API.Conversion.JobsHelpers;
using WoodForestConversion.API.Conversion.MigratorImpl.Conversion.Abstract;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.ExecutionModule;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.Job;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.JobService;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.JobStep;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.ServiceModule;
using WoodForestConversion.Data;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Conversion.Job
{
    public class JobConversion : AbstractConverter
    {
        private static readonly Random Rnd = new Random();
        private readonly Dictionary<string, MVPSI.JAMS.Agent> _connectionStoreDictionary = new Dictionary<string, MVPSI.JAMS.Agent>(StringComparer.InvariantCultureIgnoreCase);
        private readonly ConcurrentDictionary<Guid, JobFreqDto> _jobConditions = new ConcurrentDictionary<Guid, JobFreqDto>();

        private Dictionary<Guid, IGrouping<Guid, ServiceModuleDto>> ServiceModuleDictionary { get; set; }

        public JobConversion(ServiceContainer container) : base(container)
        {
        }

        public override void Convert()
        {
            // ServiceModuleDictionary = CreateServiceModuleDto();
            PopulateJobConditionsDictionary();

            var convertedJobs = new Dictionary<string, List<MVPSI.JAMS.Job>>();

            foreach (var job in Container.GetInstance<IJobRepository>().GetAllLive())
                try
                {
                    Log.Logger.Information($"Converting {job.JobName}");

                    var jamsJob = new MVPSI.JAMS.Job();

                    ConvertJobDetails(job, jamsJob);
                    ConvertJobConditions(job, jamsJob);
                    AddJobSteps(job, jamsJob);

                    if (JobConversionHelper.GenerateExceptions(jamsJob, convertedJobs, job.JobUID))
                        continue;

                    var jobCategoryName = JobConversionHelper.JobFolderName[job.JobUID]?.CategoryName;

                    if (convertedJobs.TryGetValue(jobCategoryName ?? "", out var jobForFolder))
                        jobForFolder.Add(jamsJob);
                    else
                        convertedJobs.Add(jobCategoryName ?? "", new List<MVPSI.JAMS.Job> { jamsJob });
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, ex.Message);
                    throw;
                }

            Container.Dispose();
            Directory.CreateDirectory($@"{ConversionBaseHelper.XmlOutputLocation}\ConnectionStores\");
            JAMSXmlSerializer.WriteXml(_connectionStoreDictionary.Values,
                $@"{ConversionBaseHelper.XmlOutputLocation}\ConnectionStores\ConnectionStores.xml");

            foreach (var convertedJob in convertedJobs)
            {
                JobConversionHelper.CreateDummyJobExportXml(convertedJob);

                JAMSXmlSerializer.WriteXml(convertedJob.Value,
                    $@"{ConversionBaseHelper.XmlOutputLocation}\Jobs\{convertedJob.Key}\2_run_second-{convertedJob.Key}.xml");
            }
        }

        private Dictionary<Guid, IGrouping<Guid, ServiceModuleDto>> CreateServiceModuleDto()
        {
            return (
                    from serviceModule in Container.GetInstance<IServiceModuleRepository>().GetAll()

                    join executionModule in Container.GetInstance<IExecutionModuleRepository>().GetAll()
                        on serviceModule.ModuleUID equals executionModule.ModuleUID

                    join jobService in Container.GetInstance<IJobServiceRepository>().GetAll()
                        on serviceModule.ServiceUID equals jobService.ServiceUID

                    where jobService.Available && jobService.Capacity > 0
                    select new ServiceModuleDto
                    {
                        ModuleName = executionModule.ModuleName,
                        ModuleUid = executionModule.ModuleUID,
                        ServiceUID = jobService.ServiceUID,
                        ServiceName = jobService.ServiceName
                    })
                .GroupBy(arg => arg.ModuleUid)
                .ToDictionary(dtos => dtos.Key);
        }

        private void PopulateJobConditionsDictionary()
        {
            // Get the partitioner.
            var partitioner = Partitioner.Create(JobConversionHelper.ArchonJobDictionary);

            // creation strategies.
            var partitions = partitioner.GetPartitions(Environment.ProcessorCount * 3);

            // Create a task for each partition.
            var tasks = partitions.Select(p => Task.Run(() =>
                {
                    // Create the context.
                    using (var ctx = new ARCHONEntities())
                    // Remember, the IEnumerator<T> implementation
                    // might implement IDisposable.
                    // While there are items in p.
                    {
                        while (p.MoveNext())
                        {
                            // Get the current item.
                            var current = p.Current;

                            var jobFreq = new JobFreqDto(current.Value, ctx);

                            _jobConditions.TryAdd(current.Key, jobFreq);
                        }
                    }
                })).
                // ToArray is needed (or something to materialize the list) to
                // avoid deferred execution.
                ToArray();

            Task.WaitAll(tasks);
        }

        private void ConvertJobDetails(Data.Job sourceJob, MVPSI.JAMS.Job targetJob)
        {
            sourceJob.JobName = JobConversionHelper.FixJobName(sourceJob.JobName);

            targetJob.JobName = sourceJob.JobName;
            targetJob.Description = sourceJob.Note;
            targetJob.MethodName = "Sequence";

            // Set the job to be Disabled
            targetJob.Properties.SetValue("Enabled", false);
        }

        private void ConvertJobConditions(Data.Job job, MVPSI.JAMS.Job targetJob)
        {
            var conditions = _jobConditions[job.JobUID];

            conditions.PopulateScheduleTriggers(conditions.ConditionsTree, job.Style);
            conditions.PopulateFileDependencies();
            conditions.PopulateJobDependencies(targetJob.JobName, job.Style);

            if (!conditions.Elements.Any())
                switch (conditions.ExecutionFrequency)
                {
                    //
                    // Manual jobs - Do nothing for now
                    //
                    case ExecutionFrequency.Once:
                        break;
                    //
                    // Always executing, with intervals (Everyday + Resubmit)
                    // 
                    case ExecutionFrequency.AlwaysExecuting:
                        targetJob.Elements.Add(new ScheduleTrigger("Daily", new TimeOfDay(new DateTime())));
                        targetJob.Elements.Add(new Resubmit(new DeltaTime(conditions.Interval * 60),
                            new TimeOfDay(DateTime.Today - TimeSpan.FromMinutes(conditions.Interval))));
                        break;
                }
            else
                foreach (var jobConditionsElement in conditions.Elements)
                    targetJob.Elements.Add(jobConditionsElement);
        }

        private void AddJobSteps(Data.Job sourceJob, MVPSI.JAMS.Job targetJob)
        {
            var sequenceTask = new SequenceTask();
            sequenceTask.Properties.SetValue("ParentTaskID", Guid.Empty);

            var archonSteps = Container.GetInstance<IJobStepRepository>().GetAllLive()
                .Where(js => js.JobUID == sourceJob.JobUID)
                .OrderBy(js => js.DisplayID)
                .Select(js => new ArchonStepDto
                {
                    ArchonStepName = js.StepName,
                    ArchonConfiguration = js.ConfigurationFile,
                    ParentTaskID = sequenceTask.ElementUid,
                    DisplayTitle = js.StepName,
                    ExecutionModule = Container.GetInstance<IExecutionModuleRepository>().GetAll()
                        .FirstOrDefault(em => em.ModuleUID == js.ModuleUID)
                }).ToList();

            if (!archonSteps.Any()) return;

            targetJob.SourceElements.Add(sequenceTask);

            //SelectAgent(archonSteps, targetJob);

            foreach (var archonStep in archonSteps)
            {
                if (!archonStep.ExecutionModule.ModuleObject.Equals("Archon.Modules.CommandEvent") &&
                    !archonStep.ExecutionModule.ModuleObject.Equals("Archon.Modules.SqlProcessEvent"))
                {
                    var task = new Element
                    {
                        ElementTypeName = "ArchonStep"
                    };
                    task.Properties.SetValue("ParentTaskID", archonStep.ParentTaskID);
                    task.Properties.SetValue("DisplayTitle", archonStep.DisplayTitle);
                    task.Properties.SetValue("ArchonStepName", archonStep.ArchonStepName);
                    task.Properties.SetValue("ArchonModuleName", archonStep.ExecutionModule.ModuleName);
                    task.Properties.SetValue("ArchonConfiguration", archonStep.ArchonConfiguration);
                    task.Properties.SetValue("ArchonModuleAssembly", ModuleAssemblyConverter.FromString(archonStep.ExecutionModule.ModuleAssembly));
                    task.Properties.SetValue("ArchonModuleObject", ModuleObjectConverter.FromString(archonStep.ExecutionModule.ModuleObject));
                    task.Properties.SetValue("ArchonCategoryName", CategoryConverter.FromGUID(sourceJob.Category));
                    targetJob.SourceElements.Add(task);
                }
                else
                {
                    string parsedPath = null;

                    try
                    {
                        parsedPath = JobConversionHelper.ParsePath(archonStep.ArchonConfiguration, sourceJob.Category);
                        var content = File.ReadAllText($@"C:\Users\RoniAxelrad\Desktop\Woodforest\XMLs\{parsedPath}");
                        var xmlDocument = JobConversionHelper.ToXml(content);

                        if (archonStep.ExecutionModule.ModuleObject.Equals("Archon.Modules.CommandEvent"))
                        {
                            var task = new Element
                            {
                                ElementTypeName = "PowerShellScriptTask"
                            };
                            task.Properties.SetValue("ParentTaskID", archonStep.ParentTaskID);
                            task.Properties.SetValue("DisplayTitle", archonStep.DisplayTitle);

                            var command = JobConversionHelper.ParseToCommand(xmlDocument);
                            var fixedCommand = JobConversionHelper.TranslateKeywords(command, sourceJob.Category.Value);
                            if (fixedCommand.Contains("YesterdayDate"))
                            {
                                var yesterdayParam = new Param("YesterdayDate")
                                {
                                    DataType = DataType.Date,
                                    Length = 8,
                                };
                                yesterdayParam.Properties.SetValue("DefaultValue", "YESTERDAY");
                                targetJob.Parameters.Add(yesterdayParam);
                            }

                            task.Properties.SetValue("PSScript", fixedCommand);
                            targetJob.SourceElements.Add(task);
                        }

                        if (archonStep.ExecutionModule.ModuleObject.Equals("Archon.Modules.SqlProcessEvent"))
                        {
                            var elemList = xmlDocument.GetElementsByTagName("event");

                            foreach (XmlElement sqlActivity in elemList)
                            {
                                var task = new Element
                                {
                                    ElementTypeName = "SqlQueryTask"
                                };
                                task.Properties.SetValue("ParentTaskID", archonStep.ParentTaskID);
                                task.Properties.SetValue("DisplayTitle", archonStep.DisplayTitle);

                                var server = JobConversionHelper.TranslateKeywords(sqlActivity.Attributes["server"].Value, sourceJob.Category.Value);
                                var database = JobConversionHelper.TranslateKeywords(sqlActivity.Attributes["database"].Value, sourceJob.Category.Value);

                                if (!_connectionStoreDictionary.TryGetValue(server, out var connectionStore))
                                {
                                    connectionStore = JobConversionHelper.CreateConnectionStore(sqlActivity, sourceJob.Category.Value);
                                    _connectionStoreDictionary.Add(connectionStore.AgentName, connectionStore);
                                }

                                var sqlCommand = JobConversionHelper.TranslateKeywords(
                                    GetInnerTexts(sqlActivity.GetElementsByTagName("executesql")),
                                    sourceJob.Category.Value
                                    );
                                task.Properties.SetValue("SqlQueryText", sqlCommand);
                                task.Properties.SetValue("DatabaseName", database);
                                task.Properties.SetValue("SqlAgent", new AgentReference(connectionStore));

                                targetJob.SourceElements.Add(task);
                            }
                        }

                        string GetInnerTexts(XmlNodeList nodeList)
                        {
                            var sb = new StringBuilder();
                            foreach (XmlNode node in nodeList) sb.AppendLine($"{node.InnerText.Trim()};");

                            return sb.ToString();
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        Log.Logger.Error(
                            $"Config File Is Missing! {parsedPath} --> StepName: {archonStep.ArchonStepName}, Module: {archonStep.ExecutionModule.ModuleObject}");
                        return;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        Log.Logger.Error(
                            $"Config Folder Is Missing! {parsedPath} --> StepName: {archonStep.ArchonStepName}, Module: {archonStep.ExecutionModule.ModuleObject}");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex, ex.Message);
                        return;
                    }
                }
            }
        }

        private void SelectAgent(IEnumerable<ArchonStepDto> archonSteps, MVPSI.JAMS.Job targetJob)
        {
            var jobModules = new HashSet<Guid>(archonSteps.Select(step => step.ExecutionModule.ModuleUID));
            if (!jobModules.Any()) return;

            try
            {
                IEnumerable<string> mergedList = null;
                foreach (var jobModule in jobModules)
                {
                    if (ServiceModuleDictionary.TryGetValue(jobModule, out var serviceDto))
                    {
                        var serviceNames = serviceDto.Select(dto => dto.ServiceName);

                        mergedList = mergedList == null ?
                            serviceNames :
                            mergedList.Intersect(serviceNames);
                    }
                    else
                    {
                        Log.Logger.Warning($"   No service found to run module {jobModule}");
                    }
                }

                if (mergedList == null)
                {
                    Log.Logger.Warning("   Job has no service to run on. No agent will be assigned.");
                    return;
                }

                var r = Rnd.Next(mergedList.Count());

                targetJob.AgentName = mergedList.ElementAt(r);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, ex.Message);
                throw;
            }
        }
    }
}