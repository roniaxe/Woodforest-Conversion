using System;
using System.Diagnostics;
using System.IO;
using WoodForestConversion.API.Conversion.Agents;
using WoodForestConversion.API.Conversion.Folders;
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
                var folderConverter = new FoldersConversion(logWriter);

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
