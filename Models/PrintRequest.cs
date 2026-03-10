using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nat.Rpt.Models
{
    public class PrintRequest
    {
        public string ivstrB64Document { get; set; }
        public string ivstrTemplatePath { get; set; }
    }
}