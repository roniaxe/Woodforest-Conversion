using System;

namespace DataVisualization.DTOs
{
    internal class ExecMethodDto
    {
        public string ConfigFile { get; set; }
        public Guid JobId { get; set; }
        public string JobName { get; set; }
        public Guid ModuleId { get; set; }
        public string ModuleName { get; set; }
        public string ModuleAssembly { get; set; }
        public string ModuleObject { get; set; }
        public string ConfigContent { get; set; }
        public Guid StepId { get; set; }
        public Guid? Category { get; internal set; }
    }
}