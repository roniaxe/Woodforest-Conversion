using MVPSI.JAMS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using WoodForestConversion.API.Conversion.ConversionBase;
using WoodForestConversion.API.Conversion.DTOs;
using WoodForestConversion.Data;
using Formatting = Newtonsoft.Json.Formatting;
using Job = MVPSI.JAMS.Job;

namespace WoodForestConversion.API.Conversion.Jobs
{
    public static class JobConversionHelper
    {
        #region Static Properties

        private static Dictionary<Guid, Data.Job> _archonJobDictionary;

        public static Dictionary<Guid, Data.Job> ArchonJobDictionary
        {
            get
            {
                if (_archonJobDictionary == null)
                {
                    using (var db = new ARCHONEntities())
                    {
                        _archonJobDictionary = db.Jobs
                            .Where(j => j.IsLive && !j.IsDeleted)
                            .ToDictionary(j => j.JobUID);
                    }
                }

                return _archonJobDictionary;
            }
        }

        private static Dictionary<Guid, JobCategoryDto> _jobFolderName;

        public static Dictionary<Guid, JobCategoryDto> JobFolderName
        {
            get
            {
                if (_jobFolderName == null)
                {
                    using (var db = new ARCHONEntities())
                    {
                        _jobFolderName =
                                        (from job in db.Jobs
                                         join category in db.Categories
                                            on job.Category equals category.CategoryUID into ps
                                         from category in ps.DefaultIfEmpty()
                                         select new { job.JobUID, category.CategoryName })
                                        .ToDictionary(j => j.JobUID, j => new JobCategoryDto(j.JobUID, j.CategoryName));
                    }
                }

                return _jobFolderName;
            }
        }

        private static Dictionary<string, string> _keywordsDictionary;

        public static Dictionary<string, string> KeywordsDictionary
        {
            get
            {
                if (_keywordsDictionary == null)
                {
                    using (var db = new ARCHONEntities())
                    {
                        _keywordsDictionary = db.Keywords
                            .ToDictionary(keyword => $"{keyword.CategoryUID}-{keyword.Keyword1}", keyword => keyword.KeyValue, StringComparer.InvariantCultureIgnoreCase);
                    }
                }

                return _keywordsDictionary;
            }
        }

        #endregion
        public static bool GenerateExceptions(Job job, Dictionary<string, List<Job>> convertedJobs, Guid jobUID)
        {
            bool jobProcessed;

            switch (job.JobName)
            {
                case "ATM Create CAF and PBF":
                    jobProcessed = true;

                    // Split that job into 2
                    job.Elements.Clear();
                    Job weekendJob = job.Clone() as Job;
                    weekendJob.JobName = "ATM Create CAF and PBF - Weekend";

                    // First Job
                    ScheduleTrigger scheduleTrigger = new ScheduleTrigger("Weekdays", new TimeOfDay("2:30 AM"));
                    JobDependency jobDependency = new JobDependency($@"\{ConversionBaseHelper.JamsArchonRootFolder}\ACH File Import");
                    job.Elements.Add(scheduleTrigger);
                    job.Elements.Add(jobDependency);

                    // Second job
                    ScheduleTrigger scheduleTriggerWeekend = new ScheduleTrigger("Saturday, Sunday", new TimeOfDay("12:00 AM"));
                    JobDependency jobDependencyWeekend = new JobDependency($@"\{ConversionBaseHelper.JamsArchonRootFolder}\Bank - All\Enable PBF Creation - Update Bank Table");
                    JobDependency jobDependencyWeekend2 = new JobDependency($@"\{ConversionBaseHelper.JamsArchonRootFolder}\Bank - All\All Critical Processing Complete");
                    weekendJob.Elements.Add(scheduleTriggerWeekend);
                    weekendJob.Elements.Add(jobDependencyWeekend);
                    weekendJob.Elements.Add(jobDependencyWeekend2);

                    if (convertedJobs.TryGetValue(JobFolderName[jobUID]?.CategoryName ?? "", out var jobForFolder))
                    {
                        jobForFolder.Add(job);
                        jobForFolder.Add(weekendJob);
                    }
                    else
                    {
                        convertedJobs.Add(JobFolderName[jobUID]?.CategoryName ?? "", new List<Job> { job, weekendJob });
                    }
                    break;
                default:
                    jobProcessed = false;
                    break;
            }

            return jobProcessed;
        }

        public static string FixJobName(string jobName)
        {
            while (!PathName.IsValid(jobName))
            {
                var idx = jobName.IndexOfAny(Path.GetInvalidPathChars());
                if (idx < 0)
                {
                    idx = jobName.IndexOfAny(Path.GetInvalidFileNameChars());
                }
                jobName = jobName.Replace(jobName[idx], ' ');
            }

            return jobName;
        }

        public static void ObjectToJson(string fileName, object obj)
        {
            // serialize JSON directly to a file
            using (StreamWriter file = File.CreateText(fileName))
            {
                JsonSerializer serializer = new JsonSerializer
                {
                    Formatting = Formatting.Indented
                };
                serializer.Serialize(file, obj);
            }
        }

        public static void CreateDummyJobExportXml(KeyValuePair<string, List<Job>> jobCollection)
        {
            var xmlSettings = new XmlWriterSettings
            {
                CloseOutput = true,
                Encoding = Encoding.UTF8,
                Indent = true
            };

            Directory.CreateDirectory($@"{ConversionBaseHelper.XmlOutputLocation}\Jobs\{jobCollection.Key}");
            XmlWriter xmlWriter = XmlWriter.Create($@"{ConversionBaseHelper.XmlOutputLocation}\Jobs\{jobCollection.Key}\1_run_first-{jobCollection.Key}.xml", xmlSettings);
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("JAMSObjects");

            foreach (var cj in jobCollection.Value)
            {
                xmlWriter.WriteStartElement("job");
                xmlWriter.WriteAttributeString("name", cj.JobName);
                xmlWriter.WriteAttributeString("method", cj.MethodName);
                xmlWriter.WriteFullEndElement();
            }
            xmlWriter.WriteFullEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }

        public static bool HandleATMCreateJob(string jobName, ElementCollection elements)
        {
            var excludeList = new List<string>
            {
                "BI_Warehousing_TeamPerformanceTotals",
                "CAF Reject Hist",
                "Chip Card Status Update",
                "PostCard Full Extract",
                "Postilion - HotCard File Creation",
                "Postilion Office - Copy Card and Account Data",
                "sqlops_ActivityDB_Backup_Dbs_ATM_Woodforest"
            };

            if (!excludeList.Contains(jobName)) return false;

            elements.Add(jobName.Equals(@"sqlops_ActivityDB_Backup_Dbs_ATM_Woodforest")
                ? new JobDependency($@"\{ConversionBaseHelper.JamsArchonRootFolder}\ATM Create CAF and PBF - Weekend")
                : new JobDependency($@"\{ConversionBaseHelper.JamsArchonRootFolder}\ATM Create CAF and PBF"));

            return true;

        }

        public static string TranslateKeywords(string originalText, Guid categoryGuid)
        {
            return Regex.Replace(originalText, @"\#([^\#]+)\#", match =>
            {
                var keyword = match.Groups[1].Value;
                if (keyword.StartsWith("DATE(", StringComparison.InvariantCultureIgnoreCase))
                {
                    return "{JAMS.Now(\"yyyyMMdd\")}";
                }
                if (keyword.StartsWith("YESTERDAY(", StringComparison.InvariantCultureIgnoreCase))
                {
                    return "{YesterdayDate(\"yyyyMMdd\")}";
                }
                try
                {
                    return KeywordsDictionary[$"{categoryGuid}-{keyword}"];
                }
                catch (Exception)
                {
                    return KeywordsDictionary[$"{Guid.Empty}-{keyword}"];
                }
            }, RegexOptions.IgnoreCase);
        }

        public static XmlDocument ToXml(string content)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(content);
            return xmlDoc;
        }
        public static string ParseToCommand(XmlDocument xmlDocument)
        {
            string finalCommand = null;
            XmlNodeList elemList = xmlDocument.GetElementsByTagName("command");
            for (int i = 0; i < elemList.Count; i++)
            {
                XmlAttribute exec = elemList[i].Attributes?["exec"];
                XmlAttribute args = elemList[i].Attributes?["args"];
                if (exec != null)
                {
                    finalCommand = $"{exec.Value} {(args != null ? args.Value : "")}";
                }
                else
                {
                    Console.WriteLine("Error retrieving exec content");
                }
            }

            return finalCommand;
        }

        public static string ParsePath(string path, Guid? category)
        {
            int skip = 6;

            if (path.StartsWith(@"\\woodforest.net"))
            {
                path = Regex.Replace(path, @"\\\\woodforest.net\\Jobs\\Archon2\\Configuration\\", "", RegexOptions.IgnoreCase);
                path = Regex.Replace(path, @"\\\\woodforest.net\\Jobs\\Archon2\\Apps\\", "", RegexOptions.IgnoreCase);
                return string.Join(@"\", path);
            }

            var origPathArray = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var fixedPath = TranslateKeywords(origPathArray.First(), category.Value);
            string fullPath = Path.GetFullPath(fixedPath).TrimEnd(Path.DirectorySeparatorChar);
            var fullPathArray = fullPath.Split(Path.DirectorySeparatorChar);
            string projectName = $@"{string.Join(@"\", fullPathArray.Skip(skip))}\{string.Join(@"\", origPathArray.Skip(1))}";
            return projectName;
        }

        public static Agent CreateConnectionStore(XmlDocument xmlDocument, Guid sourceJobCategory)
        {
            XmlNodeList elemList = xmlDocument.GetElementsByTagName("event");
            var server = TranslateKeywords(elemList[0].Attributes?["server"].Value, sourceJobCategory);
            var database = TranslateKeywords(elemList[0].Attributes?["database"].Value, sourceJobCategory);
            var timeout = elemList[0].Attributes?["timeout"]?.Value ?? "600";
            Agent connectionStore = new Agent
            {
                AgentName = $"{server}",//_{database}",//_{timeout}",
                Description = $"{database}",// {database}",// Database with timeout {timeout}",
                AgentTypeName = "SqlServer",
                PlatformTypeName = "Neutral",
                JobLimit = 999999
            };
            connectionStore.Properties.SetValue("SqlConnectionString",
                $"Data Source={server};Integrated Security=True;Connect Timeout={timeout};Application Name=JAMS Job");
            return connectionStore;
        }
    }
}
