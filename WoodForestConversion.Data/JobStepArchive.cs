//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WoodForestConversion.Data
{
    using System;
    using System.Collections.Generic;
    
    public partial class JobStepArchive
    {
        public long ArchiveID { get; set; }
        public System.Guid InstanceUID { get; set; }
        public System.Guid JobInstanceUID { get; set; }
        public System.Guid StepUID { get; set; }
        public byte StepStatus { get; set; }
        public Nullable<System.DateTime> StartedAtUtc { get; set; }
        public Nullable<System.DateTime> CompletedAtUtc { get; set; }
        public System.DateTime SpawnedAtUtc { get; set; }
        public System.DateTime ArchivedAtUtc { get; set; }
        public int ExitCode { get; set; }
    }
}