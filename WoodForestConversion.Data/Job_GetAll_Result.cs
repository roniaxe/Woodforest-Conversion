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
    
    public partial class Job_GetAll_Result
    {
        public int DisplayID { get; set; }
        public System.Guid JobUID { get; set; }
        public string JobName { get; set; }
        public byte Style { get; set; }
        public Nullable<System.Guid> Category { get; set; }
        public byte Priority { get; set; }
        public bool RequiresVerification { get; set; }
        public short Weight { get; set; }
        public int Controls { get; set; }
        public Nullable<System.Guid> StartStep { get; set; }
        public byte Frequency { get; set; }
        public int Interval { get; set; }
        public int ArchiveAfter { get; set; }
        public System.DateTime StopAtUtc { get; set; }
        public byte ServiceLevel { get; set; }
        public int ServiceDuration { get; set; }
        public byte AlertSeverity { get; set; }
        public string Note { get; set; }
        public string HelpFile { get; set; }
        public Nullable<System.DateTime> LastArchiveUtc { get; set; }
        public Nullable<System.DateTime> LastOccurenceUtc { get; set; }
        public bool IsLive { get; set; }
        public bool IsDeleted { get; set; }
        public Nullable<System.Guid> InstanceUID { get; set; }
        public Nullable<byte> JobStatus { get; set; }
        public Nullable<bool> IsReoccurrence { get; set; }
        public Nullable<System.Guid> ServiceUID { get; set; }
        public Nullable<bool> MetDependencies { get; set; }
        public Nullable<System.Guid> OnStep { get; set; }
        public Nullable<System.Guid> RestartFrom { get; set; }
        public Nullable<System.DateTime> StartedOnUtc { get; set; }
        public Nullable<System.DateTime> CompletedOnUtc { get; set; }
        public Nullable<System.Guid> CompletedBy { get; set; }
        public Nullable<System.DateTime> VerifiedOnUtc { get; set; }
        public Nullable<System.Guid> VerifiedBy { get; set; }
        public Nullable<System.DateTime> SpawnedAtUtc { get; set; }
        public Nullable<System.DateTime> LastModifiedUtc { get; set; }
        public Nullable<bool> HasCompleted { get; set; }
    }
}
