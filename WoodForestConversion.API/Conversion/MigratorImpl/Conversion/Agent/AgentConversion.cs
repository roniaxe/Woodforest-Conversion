using LightInject;
using MVPSI.JAMS;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using WoodForestConversion.API.Conversion.ConversionBase;
using WoodForestConversion.API.Conversion.MigratorImpl.Conversion.Abstract;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.JobService;
using WoodForestConversion.Data;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Conversion.Agent
{
    public class AgentConversion : AbstractConverter
    {
        public AgentConversion(TextWriter log, ServiceContainer container) : base(log, container)
        {
            Container.Register<DbContext, ARCHONEntities>((factory, context) => new ARCHONEntities());
        }

        public override void Convert()
        {
            var sourceAgents = Container.GetInstance<IJobServiceRepository>().GetAll();
            List<MVPSI.JAMS.Agent> convertedAgents = new List<MVPSI.JAMS.Agent>();

            foreach (var jobService in sourceAgents)
            {
                MVPSI.JAMS.Agent convertedAgent = new MVPSI.JAMS.Agent();
                ConvertAgentDetails(jobService, convertedAgent);
                convertedAgents.Add(convertedAgent);
            }
            Container.Dispose();
            Directory.CreateDirectory($@"{ConversionBaseHelper.XmlOutputLocation}\Agents\");
            JAMSXmlSerializer.WriteXml(convertedAgents, $@"{ConversionBaseHelper.XmlOutputLocation}\Agents\Agents.xml");

        }

        public void ConvertAgentDetails(JobService sourceAgent, MVPSI.JAMS.Agent targetAgent)
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
