using Archon.Tasks;
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
using Migrator.Interfaces;
using WoodForestConversion.API.Conversion.ConversionBase;
using WoodForestConversion.API.Conversion.DTOs;
using WoodForestConversion.API.Conversion.Enums;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.ExecutionModule;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.Job;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.JobService;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.ServiceModule;
using WoodForestConversion.Data;
using Job = MVPSI.JAMS.Job;

namespace WoodForestConversion.API.Conversion.Jobs
{
    public class JobConversion : IConverter
    {
        public IJobRepository JobRepository { get; }
        public IServiceModuleRepository ServiceModuleRepository { get; }
        public IExecutionModuleRepository ExecutionModuleRepository { get; }
        public IJobServiceRepository JobServiceRepository { get; }
        private readonly TextWriter _log;
        private readonly ConcurrentDictionary<Data.Job, JobFreqDto> _concurrentDictionary = new ConcurrentDictionary<Data.Job, JobFreqDto>();
        private readonly Dictionary<string, Agent> _connectionStoreDictionary = new Dictionary<string, Agent>(StringComparer.InvariantCultureIgnoreCase);
        public Dictionary<Guid, IGrouping<Guid, ServiceModuleDto>> ServiceModuleDictionary { get; set; }
        public ARCHONEntities ArchonEntities { get; set; }
        private static readonly Random Rnd = new Random();

        public JobConversion(
            TextWriter log,
            IJobRepository jobRepository,
            IServiceModuleRepository serviceModuleRepository,
            IExecutionModuleRepository executionModuleRepository,
            IJobServiceRepository jobServiceRepository)
        {
            JobRepository = jobRepository;
            ServiceModuleRepository = serviceModuleRepository;
            ExecutionModuleRepository = executionModuleRepository;
            JobServiceRepository = jobServiceRepository;
            _log = log;
            ArchonEntities = new ARCHONEntities();
        }
        public void Convert()
        {
            ServiceModuleDictionary = CreateServiceModuleDto();
            PopulateJobConditionsDictionary();

            Dictionary<string, List<Job>> convertedJobs = new Dictionary<string, List<Job>>();

            foreach (var jobConditions in _concurrentDictionary)
            {
                try
                {
                    Job jamsJob = new Job();
                    _log.WriteLine($"Converting {jobConditions.Key.JobName}");
                    ConvertJobDetails(jobConditions.Key, jamsJob);
                    ConvertJobConditions(jobConditions.Value, jamsJob);
                    AddJobSteps(jobConditions.Key, jamsJob);

                    if (JobConversionHelper.GenerateExceptions(jamsJob, convertedJobs, jobConditions.Key.JobUID)) continue;

                    var jobCategoryName = JobConversionHelper.JobFolderName[jobConditions.Key.JobUID]?.CategoryName;
                    if (convertedJobs.TryGetValue(jobCategoryName ?? "", out var jobForFolder))
                    {
                        jobForFolder.Add(jamsJob);
                    }
                    else
                    {
                        convertedJobs.Add(jobCategoryName ?? "", new List<Job> { jamsJob });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }

            Directory.CreateDirectory($@"{ConversionBaseHelper.XmlOutputLocation}\ConnectionStores\");
            JAMSXmlSerializer.WriteXml(_connectionStoreDictionary.Values, $@"{ConversionBaseHelper.XmlOutputLocation}\ConnectionStores\ConnectionStores.xml");

            foreach (var convertedJob in convertedJobs)
            {
                JobConversionHelper.CreateDummyJobExportXml(convertedJob);

                JAMSXmlSerializer.WriteXml(convertedJob.Value, $@"{ConversionBaseHelper.XmlOutputLocation}\Jobs\{convertedJob.Key}\2_run_second-{convertedJob.Key}.xml");
            }
        }

        private Dictionary<Guid, IGrouping<Guid, ServiceModuleDto>> CreateServiceModuleDto()
        {
            return (
                from serviceModule in ServiceModuleRepository.GetAll()
                join executionModule in ExecutionModuleRepository.GetAll() on serviceModule.ModuleUID equals executionModule.ModuleUID
                join jobService in JobServiceRepository.GetAll() on serviceModule.ServiceUID equals jobService.ServiceUID
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
            OrderablePartitioner<KeyValuePair<Guid, Data.Job>> partitioner = Partitioner.Create(JobConversionHelper.ArchonJobDictionary);

            // creation strategies.
            IList<IEnumerator<KeyValuePair<Guid, Data.Job>>> partitions = partitioner.GetPartitions(Environment.ProcessorCount / 2);

            // Create a task for each partition.
            Task[] tasks = partitions.Select(p => Task.Run(() =>
            {
                // Create the context.
                using (var ctx = new ARCHONEntities())
                // Remember, the IEnumerator<T> implementation
                // might implement IDisposable.
                using (p)
                    // While there are items in p.
                    while (p.MoveNext())
                    {
                        // Get the current item.
                        var current = p.Current;
                        JobFreqDto jobFreq = new JobFreqDto(current.Value, ctx);

                        _concurrentDictionary.AddOrUpdate(current.Value, jobFreq, (job1, dto) => dto);
                    }
            })).
                // ToArray is needed (or something to materialize the list) to
                // avoid deferred execution.
                ToArray();

            Task.WaitAll(tasks);
        }

        private void ConvertJobDetails(Data.Job sourceJob, Job targetJob)
        {
            sourceJob.JobName = JobConversionHelper.FixJobName(sourceJob.JobName);

            targetJob.JobName = sourceJob.JobName;
            targetJob.Description = sourceJob.Note;
            targetJob.MethodName = "Sequence";
        }

        private void ConvertJobConditions(JobFreqDto conditions, Job targetJob)
        {
            conditions.PopulateScheduleTriggers(conditions.ConditionsTree);
            conditions.PopulateFileDependencies();
            conditions.PopulateJobDependencies(targetJob.JobName);

            if (!conditions.Elements.Any())
            {
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
            }
            else
            {
                foreach (var jobConditionsElement in conditions.Elements)
                {
                    targetJob.Elements.Add(jobConditionsElement);
                }
            }
        }

        private void AddJobSteps(Data.Job sourceJob, Job targetJob)
        {
            SequenceTask sequenceTask = new SequenceTask();
            sequenceTask.Properties.SetValue("ParentTaskID", Guid.Empty);

            var archonSteps = ArchonEntities.JobSteps
                .Where(js => js.JobUID == sourceJob.JobUID && !js.IsDeleted && js.IsLive)
                .OrderBy(js => js.DisplayID).Select(js => new ArchonStepDto
                {
                    ArchonStepName = js.StepName,
                    ArchonConfiguration = js.ConfigurationFile,
                    ParentTaskID = sequenceTask.ElementUid,
                    DisplayTitle = js.StepName,
                    ExecutionModule = ArchonEntities.ExecutionModules
                        .FirstOrDefault(em => em.ModuleUID == js.ModuleUID)
                });
            if (!archonSteps.Any()) return;

            targetJob.SourceElements.Add(sequenceTask);

            SelectAgent(archonSteps, targetJob);

            foreach (var archonStep in archonSteps)
            {
                var seqTask = new Element();
                seqTask.Properties.SetValue("ParentTaskID", archonStep.ParentTaskID);

                if (!archonStep.ExecutionModule.ModuleObject.Equals("Archon.Modules.CommandEvent") &&
                    !archonStep.ExecutionModule.ModuleObject.Equals("Archon.Modules.SqlProcessEvent"))
                {
                    seqTask.ElementTypeName = "ArchonStep";
                    seqTask.Properties.SetValue("ArchonStepName", archonStep.ArchonStepName);
                    seqTask.Properties.SetValue("ArchonModuleName", archonStep.ExecutionModule.ModuleName);
                    seqTask.Properties.SetValue("ArchonConfiguration", archonStep.ArchonConfiguration);
                    seqTask.Properties.SetValue("ArchonModuleAssembly", ModuleAssemblyConverter.FromString(archonStep.ExecutionModule.ModuleAssembly));
                    seqTask.Properties.SetValue("ArchonModuleObject", ModuleObjectConverter.FromString(archonStep.ExecutionModule.ModuleObject));
                    seqTask.Properties.SetValue("DisplayTitle", archonStep.DisplayTitle);
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
                            seqTask.ElementTypeName = "PowerShellScriptTask";
                            string command = JobConversionHelper.ParseToCommand(xmlDocument);
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
                            seqTask.Properties.SetValue("PSScript", fixedCommand);
                        }

                        if (archonStep.ExecutionModule.ModuleObject.Equals("Archon.Modules.SqlProcessEvent"))
                        {
                            seqTask.ElementTypeName = "SqlQueryTask";
                            XmlNodeList elemList = xmlDocument.GetElementsByTagName("event");
                            var server = JobConversionHelper.TranslateKeywords(elemList[0].Attributes?["server"].Value, sourceJob.Category.Value);
                            if (!_connectionStoreDictionary.TryGetValue(server, out var connectionStore))
                            {
                                connectionStore = JobConversionHelper.CreateConnectionStore(xmlDocument, sourceJob.Category.Value);
                                _connectionStoreDictionary.Add(connectionStore.AgentName, connectionStore);
                            }

                            string sqlCommand = GetInnerTexts(xmlDocument.GetElementsByTagName("executesql"));
                            seqTask.Properties.SetValue("SqlQueryText", sqlCommand);
                            seqTask.Properties.SetValue("DatabaseName", connectionStore.Description);
                            seqTask.Properties.SetValue("SqlAgent", new AgentReference(connectionStore));
                        }

                        string GetInnerTexts(XmlNodeList nodeList)
                        {
                            StringBuilder sb = new StringBuilder();
                            foreach (XmlNode node in nodeList)
                            {
                                sb.AppendLine($"{node.InnerText.Trim()};");
                            }

                            return sb.ToString();
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        _log.WriteLine($"Config File Is Missing! {parsedPath} --> StepName: {archonStep.ArchonStepName}, Module: {archonStep.ExecutionModule.ModuleObject}");
                        return;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        _log.WriteLine($"Config Folder Is Missing! {parsedPath} --> StepName: {archonStep.ArchonStepName}, Module: {archonStep.ExecutionModule.ModuleObject}");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        return;
                    }
                }

                targetJob.SourceElements.Add(seqTask);
            }
        }

        private void SelectAgent(IQueryable<ArchonStepDto> archonSteps, Job targetJob)
        {
            HashSet<Guid> jobModules = new HashSet<Guid>(archonSteps.Select(step => step.ExecutionModule.ModuleUID));
            if (!jobModules.Any()) return;

            try
            {
                IEnumerable<string> mergedList = null;
                foreach (var jobModule in jobModules)
                {
                    IEnumerable<string> serviceList;

                    if (ServiceModuleDictionary.ContainsKey(jobModule))
                    {
                        serviceList = ServiceModuleDictionary[jobModule].Select(dto => dto.ServiceName);
                    }
                    else
                    {
                        _log.WriteLine($"   No service found to run module {jobModule} - {ArchonEntities.ExecutionModules.FirstOrDefault(module => module.ModuleUID == jobModule)?.ModuleName}");
                        continue;
                    }

                    if (mergedList == null)
                    {
                        mergedList = serviceList;
                    }
                    else
                    {
                        mergedList = mergedList.Join(serviceList,
                            s => s, s => s, (s, s1) => s);
                    }
                }

                if (mergedList == null)
                {
                    _log.WriteLine("   Job has no service to run on. No agent will be assigned.");
                    return;
                }

                int r = Rnd.Next(mergedList.Count());

                targetJob.AgentName = mergedList.ElementAt(r);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}
