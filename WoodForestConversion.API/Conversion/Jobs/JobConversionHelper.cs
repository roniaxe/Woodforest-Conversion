using System;
using MVPSI.JAMS;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using System.Xml;
using WoodForestConversion.API.Conversion.ConversionBase;
using Formatting = Newtonsoft.Json.Formatting;
using Job = MVPSI.JAMS.Job;

namespace WoodForestConversion.API.Conversion.Jobs
{
    public static class JobConversionHelper
    {
        private static readonly List<string> ConditionsExceptions = new List<string>
        {
            "ATM Create CAF and PBF",
            "Enable PBF Creation - Update Bank Table",
            "Excessive NSF OD Fee - Daily Email",
            "MCOM Decompress *",
            "Send Shift Change Email",
            "sqlops_Fix_Card_Transaction_Search - Scheduled to run on sunday and not on sunday",
            "CommVault_ORIONDB",
            "PDF Statement Upload to Depot",
            "CommVault_AMBITDB",
            "Add ACH completion time to Processing Timesheet",
            "CommVault_PRMDB",
            "CommVault_EvergreenDB",
            "CommVault_MGDB",
            "CommVault_DataWarehouseDB",
            "Bill Pay Import",
            "Review Immediate Availability",
            "CommVault_BIMODELDB",
            "CommVault_CRMDB",
            "sqlops_PRMDB_RebuildIndex",
            "AMBIT Data Export",
            "sqlops_MGDB_RebuildIndex"
        };

        private static readonly List<string> HasDependencyThatIsNotLive = new List<string>
        {
            "sqlops_SQLKEYDB_RebuildIndex",
            "Process ATM reports from Saturday",
            "Remove BAM Rebuild Index from Live",
            //"CLNUSERPROD Import",
            //"CLNNAIC Import",
            //"CommVault_BANKERSTOOLBOXDB",
            //"CLNPRIME Import",
            //"Upload Overdraft Letters to FOS",
            "sqlops_DPMDB_RebuildIndex",
            "CommVault_SQLKEYDB",
            "EMP_REL Import",
            "CLNWHENT Import",
            "CLNWHEN Import",
            "CommVault_DPMDB",
            "CommVault_SSASDB",
            "CLNBASTB Import",
            "CLCOLLTY Import",
            "SFTP Branch Capture CSV file to FOS",
            "sqlops_EVERGREEN_ServerProperties_Update",
            "CLNBASIS Import",
            "ACH Disputes database sync",
            "CLNGRADE Import",
            "CLCOLLOC Import",
            "Confirm email received from Equifax.",
            "CLNCONCEN Import"
        };
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

                    if (convertedJobs.TryGetValue(JobConversion.JobFolderName[jobUID]?.CategoryName ?? "", out var jobForFolder))
                    {
                        jobForFolder.Add(job);
                        jobForFolder.Add(weekendJob);
                    }
                    else
                    {
                        convertedJobs.Add(JobConversion.JobFolderName[jobUID]?.CategoryName ?? "", new List<Job> { job, weekendJob });
                    }
                    break;
                default:
                    jobProcessed = false;
                    break;
            }

            return jobProcessed;
        }

        public static bool CheckNonConvertible(string jobName)
        {
            return HasDependencyThatIsNotLive.Contains(jobName);
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
            var excludeList = new List<string> {
                "BI_Warehousing_TeamPerformanceTotals",
                "CAF Reject Hist",
                "Chip Card Status Update",
                "PostCard Full Extract",
                "Postilion - HotCard File Creation",
                "Postilion Office - Copy Card and Account Data",
                "sqlops_ActivityDB_Backup_Dbs_ATM_Woodforest"
            };

            if (excludeList.Contains(jobName))
            {
                elements.Add(jobName.Equals(@"sqlops_ActivityDB_Backup_Dbs_ATM_Woodforest")
                    ? new JobDependency($@"\{ConversionBaseHelper.JamsArchonRootFolder}\ATM Create CAF and PBF - Weekend")
                    : new JobDependency($@"\{ConversionBaseHelper.JamsArchonRootFolder}\ATM Create CAF and PBF"));
                return true;
            }

            return false;
        }
    }
}
