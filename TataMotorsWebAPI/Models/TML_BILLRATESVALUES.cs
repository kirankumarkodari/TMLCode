namespace TataMotorsWebAPI.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("TML_BILLRATESVALUES", Schema = "TMLDB_USER")]
    public partial class TML_BILLRATESVALUES
    {
        public decimal MD_CHARGE { get; set; }

        public decimal BASIC_CHARGE { get; set; }

        public decimal FAC { get; set; }

        public decimal ELEC_CHARGES { get; set; }

        public decimal TAX_CHARGE { get; set; }

        public decimal PF { get; set; }

        public decimal EHV { get; set; }

        public decimal PROMPTPAY { get; set; }

        public decimal AZONEVAL { get; set; }

        public decimal BZONEVAL { get; set; }

        public decimal CZONEVAL { get; set; }

        public decimal DZONEVAL { get; set; }

        public decimal CROSS_SUR_CHARGE { get; set; }

        public decimal WHEEL_CHARGE { get; set; }

        public decimal TRANS_CHARGE { get; set; }

        public decimal OTH_PUR_DISCOUNT { get; set; }

        [Key]
        public DateTime VAL_MONYR { get; set; }

        public decimal OTH_MD_CHARGE { get; set; }

        public decimal OTH_BASIC_CHARGE { get; set; }

        public decimal OTH_FAC { get; set; }

        public decimal OTH_ELEC_CHARGES { get; set; }

        public decimal OTH_TAX_CHARGE { get; set; }

        public decimal OTH_PF { get; set; }

        public decimal OTH_EHV { get; set; }

        public decimal OTH_PROMPTPAY { get; set; }

        public decimal OTH_AZONEVAL { get; set; }

        public decimal OTH_BZONEVAL { get; set; }

        public decimal OTH_CZONEVAL { get; set; }

        public decimal OTH_DZONEVAL { get; set; }
    }
}
