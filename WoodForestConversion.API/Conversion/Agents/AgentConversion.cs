using LightInject;
using Migrator.Interfaces;
using MVPSI.JAMS;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using WoodForestConversion.API.Conversion.ConversionBase;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.JobService;
using WoodForestConversion.Data;

namespace WoodForestConversion.API.Conversion.Agents
{
    public class AgentConversion : IConverter
    {
        private readonly TextWriter _log;
        private ServiceContainer _container;
        public AgentConversion(TextWriter log)
        {
            _log = log;
            CreateContainer();
        }

        private void CreateContainer()
        {
            _container = new ServiceContainer();
            _container.Register<DbContext, ARCHONEntities>();
            _container.Register<IJobServiceRepository, JobServiceRepository>();
        }

        public void Convert()
        {
            var sourceAgents = _container.GetInstance<IJobServiceRepository>().GetAll();
            List<Agent> convertedAgents = new List<Agent>();

            foreach (var jobService in sourceAgents)
            {
                Agent convertedAgent = new Agent();
                ConvertAgentDetails(jobService, convertedAgent);
                convertedAgents.Add(convertedAgent);
            }

            Directory.CreateDirectory($@"{ConversionBaseHelper.XmlOutputLocation}\Agents\");
            JAMSXmlSerializer.WriteXml(convertedAgents, $@"{ConversionBaseHelper.XmlOutputLocation}\Agents\Agents.xml");

        }

        public void ConvertAgentDetails(JobService sourceAgent, Agent targetAgent)
        {
            targetAgent.AgentName = sourceAgent.ServiceName;
            targetAgent.Online = sourceAgent.Available;
            targetAgent.AgentTypeName = "Outgoing";
            targetAgent.JobLimit = sourceAgent.Capacity;
            targetAgent.AgentPlatform = AgentPlatform.Windows;
            targetAgent.PlatformTypeName = "Windows";
        }
    }
}
