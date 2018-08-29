using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TataMotorsWebAPI.Models.DTOClasses
{
    
    public class BillRates
    {
        public decimal MD_CHARGE { get; set; }
        public decimal BASIC_CHARGE { get; set; }
        public decimal AZONEVAL { get; set; }
        public decimal BZONEVAL { get; set; }
        public decimal CZONEVAL { get; set; }
        public decimal DZONEVAL { get; set; }
        public decimal FAC { get; set; }
        public decimal ELEC_CHARGES { get; set; }
        public decimal TAX_CHARGE { get; set; }
        public decimal PF { get; set; }
        public decimal EHV { get; set; }
        public decimal PROMPTPAY { get; set; }
    }
    
    public class AdditionaInfoDTO
    {

        public BillRates MSEDCL { get; set; }
        public BillRates Others { get; set; }          
        public decimal CrossCharges { get; set; }
        public decimal WheelingCharges { get; set; }
        public decimal TransCharges { get; set; }
        public decimal OthPurDiscnt { get; set; }
        public Byte IsLoaded { get; set; }  // IsLoaded -> 0 - False, 1 - True

        public AdditionaInfoDTO()
        {
            MSEDCL = new BillRates();
            Others = new BillRates();
            IsLoaded = 0;  
        }
    }
}