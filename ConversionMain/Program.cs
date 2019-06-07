using System;
using System.IO;
using WoodForestConversion.API.Conversion.Agents;
using WoodForestConversion.API.Conversion.Folders;
using WoodForestConversion.API.Conversion.Jobs;
using WoodForestConversion.API.Conversion.Queues;

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
                var folderConverter = new FoldersConversion(logWriter);
                //var queueConverter = new QueueConversion();

                //var queues = queueConverter.Convert();
                //jobConverter.BatchQueues = queues;
                agentConverter.Convert();
                folderConverter.Convert();
                jobConverter.Convert();
                

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
