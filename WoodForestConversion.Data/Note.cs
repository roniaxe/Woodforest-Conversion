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
    
    public partial class Note
    {
        public long DisplayID { get; set; }
        public System.Guid NoteUID { get; set; }
        public System.Guid InstanceUID { get; set; }
        public Nullable<System.Guid> StepUID { get; set; }
        public System.DateTime CreatedOnUtc { get; set; }
        public Nullable<System.Guid> CreatedBy { get; set; }
        public string Description { get; set; }
    }
}