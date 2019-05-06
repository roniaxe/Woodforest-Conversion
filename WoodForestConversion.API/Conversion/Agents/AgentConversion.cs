using MVPSI.JAMS;
using System;
using System.Collections.Generic;
using System.IO;
using WoodForestConversion.API.Conversion.Base;
using WoodForestConversion.Data;

namespace WoodForestConversion.API.Conversion.Agents
{
    public class AgentConversion : IAgentConverter<JobService, Agent>
    {
        private readonly TextWriter _log;
        public AgentConversion(TextWriter log)
        {
            _log = log;
        }

        public ICollection<Agent> Convert()
        {
            using (var archonEntities = new ARCHONEntities())
            {
                _log.WriteLine("Starting Agent Conversion");
                _log.WriteLine("-----------------------");
                _log.WriteLine();

                var sourceAgents = archonEntities.JobServices;
                List<Agent> convertedAgents = new List<Agent>();


                foreach (var jobService in sourceAgents)
                {
                    _log.WriteLine($"Converting Agent: {jobService.ServiceName}");
                    Agent convertedAgent = new Agent();

                    ConvertAgentDetails(jobService, convertedAgent);                   

                    convertedAgents.Add(convertedAgent);
                }

                return convertedAgents;
            }
        }

        public void ConvertAgentDetails(JobService sourceAgent, Agent targetAgent)
        {
            targetAgent.AgentName = sourceAgent.ServiceName;
            targetAgent.Online = sourceAgent.Available;
            targetAgent.AgentTypeName = "Outgoing";
            targetAgent.AgentPlatform = AgentPlatform.Windows;
            targetAgent.PlatformTypeName = "Windows";
        }
    }
}
