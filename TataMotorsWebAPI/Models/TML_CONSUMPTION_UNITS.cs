namespace TataMotorsWebAPI.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("TML_CONSUMPTION_UNITS", Schema = "TMLDB_USER")]
    public partial class TML_CONSUMPTION_UNITS
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long CON_UNO { get; set; }

        public long CONSUMER_NO { get; set; }

        [StringLength(12)]
        public string SERIAL_NO { get; set; }

        public DateTime CREATED_ON { get; set; }
    }
}
