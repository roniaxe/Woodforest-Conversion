using System;
using System.IO;
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

                jobConverter.Convert();
                agentConverter.Convert();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadKey();
            }
            finally
            {
                logWriter?.Dispose();
            }
        }
    }
}
