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
    
    public partial class JobInstance_Get_Result
    {
        public System.Guid InstanceUID { get; set; }
        public System.Guid JobUID { get; set; }
        public byte JobStatus { get; set; }
        public bool IsReoccurrence { get; set; }
        public bool MetDependencies { get; set; }
        public Nullable<System.Guid> ServiceUID { get; set; }
        public Nullable<System.Guid> OnStep { get; set; }
        public Nullable<System.Guid> RestartFrom { get; set; }
        public Nullable<System.DateTime> StartedOnUtc { get; set; }
        public Nullable<System.DateTime> CompletedOnUtc { get; set; }
        public Nullable<System.Guid> CompletedBy { get; set; }
        public Nullable<System.DateTime> VerifiedOnUtc { get; set; }
        public Nullable<System.Guid> VerifiedBy { get; set; }
        public System.DateTime SpawnedAtUtc { get; set; }
        public System.DateTime LastModifiedUtc { get; set; }
        public bool HasCompleted { get; set; }
    }
}
