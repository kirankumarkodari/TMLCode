using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TataMotorsWebAPI.Models.DTOClasses
{
    public class UploadResponseDTO
    {
        public int Error_Code { get; set; }
        public string Msg { get; set; }
    }
}