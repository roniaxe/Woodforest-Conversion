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
    
    public partial class JobService_GetByPool_Result
    {
        public System.Guid ServiceUID { get; set; }
        public string ServiceName { get; set; }
        public byte ServiceType { get; set; }
        public byte JobPool { get; set; }
        public bool Available { get; set; }
        public short Capacity { get; set; }
        public System.DateTime RegisteredOnUtc { get; set; }
        public System.DateTime LastReportedUtc { get; set; }
    }
}
