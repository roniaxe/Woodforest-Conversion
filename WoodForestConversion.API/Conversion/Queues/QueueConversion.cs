using MVPSI.JAMS;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using WoodForestConversion.API.Conversion.ConversionBase;
using WoodForestConversion.API.Conversion.DTOs;
using WoodForestConversion.Data;

namespace WoodForestConversion.API.Conversion.Queues
{
    public class QueueConversion
    {
        public Dictionary<Guid, IEnumerable<Guid>> ServiceModuleDictionary { get; set; }
        public ARCHONEntities ArchonEntities { get; set; }

        public QueueConversion()
        {
            ArchonEntities = new ARCHONEntities();

            ServiceModuleDictionary = ArchonEntities.ServiceModules
                .GroupBy(module => module.ModuleUID)
                .ToDictionary(
                    modules => modules.Key,
                    modules => modules.Select(module => module.ServiceUID)
                );

            var q =
                (
                    from serviceModule in ArchonEntities.ServiceModules
                    join executionModule in ArchonEntities.ExecutionModules on serviceModule.ModuleUID equals executionModule.ModuleUID
                    join jobService in ArchonEntities.JobServices on serviceModule.ServiceUID equals jobService.ServiceUID 
                    select new
                    {
                        executionModule.ModuleName,
                        executionModule.ModuleUID,
                        jobService.ServiceUID,
                        jobService.ServiceName
                    }).GroupBy(arg => arg.ModuleUID).ToList();
        }

        public List<BatchQueue> Convert()
        {
            var queueList = new List<BatchQueue>();
            foreach (var serviceModule in ServiceModuleDictionary)
            {
                var queue = new BatchQueue
                {
                    QueueName = ArchonEntities.ExecutionModules.FirstOrDefault(em => em.ModuleUID == serviceModule.Key).ModuleName.Replace('.', '_')
                };

                foreach (var guid in serviceModule.Value)
                {
                    var serviceName = ArchonEntities.JobServices.FirstOrDefault(js => js.ServiceUID == guid)?.ServiceName;
                    if (serviceName != null)
                    {
                        queue.StartedOn.Add(new BatchQueueAgent(serviceName));
                    }
                }
                queueList.Add(queue);
            }
            Directory.CreateDirectory($@"{ConversionBaseHelper.XmlOutputLocation}\Queues");
            JAMSXmlSerializer.WriteXml(queueList, $@"{ConversionBaseHelper.XmlOutputLocation}\Queues\Queues.xml");
            return queueList;
        }
    }
}
