using System.IO;
using WoodForestConversion.API.Conversion;
using WoodForestConversion.API.Conversion.Agents;
using WoodForestConversion.API.Conversion.Jobs;

namespace WoodForest.Conversion
{
    public class Program
    {
        public static void Main(string[] args)
        {
            TextWriter logWriter = null;
            try
            {
                logWriter = File.CreateText("log.txt");
                var jobConverter = new JobConversion(logWriter);
                var agentConverter = new AgentConversion(logWriter);

                ConvertMain convertMain = new ConvertMain(jobConverter, agentConverter);
                convertMain.Convert();
            }
            finally
            {
                logWriter?.Dispose();
            }
        }
    }
}
