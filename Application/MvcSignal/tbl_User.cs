//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MvcSignal
{
    using System;
    using System.Collections.Generic;
    
    public partial class tbl_User
    {
        public int UserID { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int AdminCode { get; set; }
        public string ImagePath { get; set; }
        public Nullable<int> DepartmentID { get; set; }
    
        public virtual tbl_Dep tbl_Dep { get; set; }
    }
}
