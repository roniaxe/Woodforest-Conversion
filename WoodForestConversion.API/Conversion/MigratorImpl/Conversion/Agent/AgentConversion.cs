using System;
using LightInject;
using MVPSI.JAMS;
using System.Collections.Generic;
using System.IO;
using Serilog;
using WoodForestConversion.API.Conversion.ConversionBase;
using WoodForestConversion.API.Conversion.MigratorImpl.Conversion.Abstract;
using WoodForestConversion.API.Conversion.MigratorImpl.Conversion.Core;
using WoodForestConversion.API.Conversion.MigratorImpl.Repositories.JobService;
using WoodForestConversion.Data;

namespace WoodForestConversion.API.Conversion.MigratorImpl.Conversion.Agent
{
    public class AgentConversion : AbstractConverter<Data.JobService, MVPSI.JAMS.Agent>
    {
        public AgentConversion(ServiceContainer container) : base(container)
        {
            Source = Container.GetInstance<IJobServiceRepository>().GetAll();
        }

        public override void Convert()
        {
            try
            {
                foreach (var jobService in Source)
                {
                    var newAgent = GetInstance();

                    ConvertAgentDetails(jobService, newAgent);

                    Target.Add(newAgent);
                }

                SerializerHelper.Serialize(Target);
            }
            catch (Exception exception)
            {
                Log.Logger.Error(exception, exception.Message);
                throw;
            }
        }

        private void ConvertAgentDetails(JobService sourceAgent, MVPSI.JAMS.Agent targetAgent)
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
