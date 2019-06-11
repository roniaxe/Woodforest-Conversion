using MVPSI.JAMS;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using WoodForestConversion.API.Conversion.Base;
using WoodForestConversion.API.Conversion.ConversionBase;
using WoodForestConversion.Data;

namespace WoodForestConversion.API.Conversion.Agents
{
    public class AgentConversion : IEntityToConvert
    {
        private readonly TextWriter _log;
        public AgentConversion(TextWriter log)
        {
            _log = log;
        }

        public void Convert()
        {
            using (var archonEntities = new ARCHONEntities())
            {
                var sourceAgents = archonEntities.JobServices;
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
