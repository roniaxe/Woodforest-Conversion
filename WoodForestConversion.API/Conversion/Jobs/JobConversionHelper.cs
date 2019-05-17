using System;
using MVPSI.JAMS;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

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
            "CLNUSERPROD Import",
            "CLNNAIC Import",
            "CBACCTYPES Import",
            "sqlops_SQLMaint_Rebuild_Index_bankers",
            "CommVault_BANKERSTOOLBOXDB",
            "CLNPRIME Import",
            "Upload Overdraft Letters to FOS",
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
        public static bool GenerateExceptions(Data.Job job, Dictionary<string, List<Job>> convertedJobs)
        {
            bool jobProcessed;

            switch (job.JobName)
            {
                case "ATM Create CAF and PBF":
                    jobProcessed = true;
                    var jamsJob = new Job();
                    // Split that job into 2
                    Job weekendJob = jamsJob.Clone() as Job;
                    weekendJob.JobName = "ATM Create CAF and PBF - Weekend";

                    // First Job
                    ScheduleTrigger scheduleTrigger = new ScheduleTrigger("Weekdays", new TimeOfDay("2:30 AM"));
                    JobDependency jobDependency = new JobDependency(@"\ACH File Import");
                    jamsJob.Elements.Add(scheduleTrigger);
                    jamsJob.Elements.Add(jobDependency);

                    // Second job
                    ScheduleTrigger scheduleTriggerWeekend = new ScheduleTrigger("Saturday, Sunday", new TimeOfDay("12:00 AM"));
                    JobDependency jobDependencyWeekend = new JobDependency(@"\Enable PBF Creation - Update Bank Table");
                    JobDependency jobDependencyWeekend2 = new JobDependency(@"\All Critical Processing Complete");
                    weekendJob.Elements.Add(scheduleTriggerWeekend);
                    weekendJob.Elements.Add(jobDependencyWeekend);
                    weekendJob.Elements.Add(jobDependencyWeekend2);

                    if (convertedJobs.TryGetValue(JobConversion.JobFolderName[job.JobUID], out var jobForFolder))
                    {
                        jobForFolder.Add(jamsJob);
                        jobForFolder.Add(weekendJob);
                    }
                    else
                    {
                        convertedJobs.Add(JobConversion.JobFolderName[job.JobUID], new List<Job> { jamsJob, weekendJob });
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
            return ConditionsExceptions.Contains(jobName) ||
                   HasDependencyThatIsNotLive.Contains(jobName);
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

        public static void AddIfUnique(this ElementCollection elementCollection, Element element)
        {
            switch (element)
            {
                case RunawayEvent _:
                    foreach (var e in elementCollection)
                    {
                        if (e is RunawayEvent)
                        {
                            return;
                        }
                    }

                    elementCollection.Add(element);
                    break;
                case Resubmit _:
                    foreach (var e in elementCollection)
                    {
                        if (e is Resubmit)
                        {
                            break;
                        }
                    }

                    elementCollection.Add(element);
                    break;
                case JobDependency jobDependency:
                    foreach (var e in elementCollection)
                    {
                        if (!(e is JobDependency jDep)) continue;
                        if (jDep.DependOnJob.JobName == jobDependency.DependOnJob.JobName) break;
                    }

                    elementCollection.Add(element);
                    break;
                case FileDependency fileDependency:
                    foreach (var e in elementCollection)
                    {
                        if (!(e is FileDependency fDep)) continue;
                        if (fDep.FileName == fileDependency.FileName) break;
                    }

                    elementCollection.Add(element);
                    break;
            }
        }
    }
}
