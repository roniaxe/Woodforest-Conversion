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
                            .ToDictionary(keyword => $"{keyword.CategoryUID}-{keyword.Keyword1.ToUpperInvariant()}", keyword => keyword.KeyValue);
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
            Regex yourRegex = new Regex(@"\#([^\#]+)\#");
            return yourRegex.Replace(originalText, match =>
            {
                try
                {
                    return KeywordsDictionary[$"{categoryGuid}-{match.Groups[1].Value.ToUpperInvariant()}"];
                }
                catch (Exception)
                {
                    return KeywordsDictionary[$"{Guid.Empty}-{match.Groups[1].Value.ToUpperInvariant()}"];
                }
            });
        }

        public static string ParseToCommand(string fixedContent)
        {
            string finalCommand = null;
            XmlDocument document = new XmlDocument();
            document.LoadXml(fixedContent);
            XmlNodeList elemList = document.GetElementsByTagName("command");
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
            path = path.ToUpperInvariant().Replace(@"\\woodforest.net\Jobs\Archon2\Configuration\".ToUpperInvariant(), "");
            var origPathArray = path.Split(Path.DirectorySeparatorChar);
            var fixedPath = TranslateKeywords(origPathArray.First(), category.Value);
            string fullPath = Path.GetFullPath(fixedPath).TrimEnd(Path.DirectorySeparatorChar);
            var fullPathArray = fullPath.Split(Path.DirectorySeparatorChar);
            string projectName = $@"{fullPathArray.Last()}\{string.Join(@"\", origPathArray.Skip(1))}";
            return projectName;
        }
    }
}
