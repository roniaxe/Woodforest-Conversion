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
    
    public partial class StatusChange_GetByInstance_Result
    {
        public System.Guid ChangeUID { get; set; }
        public byte OverallStatus { get; set; }
        public Nullable<System.Guid> StepUID { get; set; }
        public Nullable<byte> StepStatus { get; set; }
        public Nullable<System.Guid> RestartFrom { get; set; }
        public System.DateTime OccurredAtUtc { get; set; }
        public Nullable<System.Guid> ChangedBy { get; set; }
        public string Operator { get; set; }
    }
}
