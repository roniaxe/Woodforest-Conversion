using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MVPSI.JAMS;
using WoodForestConversion.API.Conversion.Agents;
using WoodForestConversion.API.Conversion.Base;
using WoodForestConversion.API.Conversion.Jobs;
using WoodForestConversion.Data;
using Job = MVPSI.JAMS.Job;

namespace WoodForestConversion.API.Conversion
{
    public class ConvertMain
    {
        private readonly Dictionary<string, string> _keywordsDictionary;
        private IConverter<Job> JobConverter { get; }
        private IConverter<Agent> AgentConverter { get;}

        public ConvertMain(IJobConverter<Data.Job, Job> jobConverter, IAgentConverter<JobService, Agent> agentConverter)
        {
            JobConverter = jobConverter;
            AgentConverter = agentConverter;
            ARCHONEntities entities = new ARCHONEntities();
            _keywordsDictionary = entities.Keywords
                .ToList()
                .GroupBy(kw => kw.Keyword1, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().KeyValue, StringComparer.OrdinalIgnoreCase);
        }
        public void Convert()
        {
            var convertedAgents = AgentConverter.Convert();
            var convertedJobs = JobConverter.Convert();
        }

        private string CopyWithKeywordCheck(string value)
        {
            var firstHashPosition = value.IndexOf('#');
            if (firstHashPosition == -1) return value;
            var secondHashPosition = value.IndexOf('#', firstHashPosition + 1);
            if (secondHashPosition == -1) return value;

            _keywordsDictionary.TryGetValue(value.Substring(firstHashPosition + 1, secondHashPosition - 1), out var keywordValue);
            return keywordValue;
        }
    }
}
