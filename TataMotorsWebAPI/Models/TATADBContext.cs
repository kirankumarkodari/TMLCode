namespace TataMotorsWebAPI.Models
{
    using System;
    using System.Data.Entity;
    using System.Diagnostics;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class TATADBContext : DbContext
    {
        public TATADBContext()
            : base("name=   TATADBContext")
        {
            Debug.WriteLine("DBContext created : " + DateTime.Now.ToString());
            Database.Log = message => Trace.Write(message);
        }


        public virtual DbSet<TML_CONSUMPTION_UNITS> TML_CONSUMPTION_UNITS { get; set; }
        public virtual DbSet<TML_GENERATION_UNITS> TML_GENERATION_UNITS { get; set; }
        public virtual DbSet<TML_BILLRATESVALUES> TML_BILLRATESVALUES { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TML_CONSUMPTION_UNITS>()
                 .Property(e => e.SERIAL_NO)
                 .IsUnicode(false);

            modelBuilder.Entity<TML_GENERATION_UNITS>()
                .Property(e => e.GENERATION_TYPE)
                .IsUnicode(false);

            modelBuilder.Entity<TML_GENERATION_UNITS>()
                .Property(e => e.SERIAL_NO)
                .IsUnicode(false);

            modelBuilder.Entity<TML_GENERATION_UNITS>()
                .Property(e => e.UNIT_NAME)
                .IsUnicode(false);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.MD_CHARGE)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.BASIC_CHARGE)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.FAC)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.ELEC_CHARGES)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.TAX_CHARGE)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.PF)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.EHV)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.PROMPTPAY)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.AZONEVAL)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.BZONEVAL)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.CZONEVAL)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.DZONEVAL)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.CROSS_SUR_CHARGE)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.WHEEL_CHARGE)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.TRANS_CHARGE)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.OTH_MD_CHARGE)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.OTH_BASIC_CHARGE)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.OTH_FAC)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.OTH_ELEC_CHARGES)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.OTH_TAX_CHARGE)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.OTH_PF)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.OTH_EHV)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.OTH_PROMPTPAY)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.OTH_AZONEVAL)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.OTH_BZONEVAL)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.OTH_CZONEVAL)
                .HasPrecision(38, 0);

            modelBuilder.Entity<TML_BILLRATESVALUES>()
                .Property(e => e.OTH_DZONEVAL)
                .HasPrecision(38, 0);
           
        }

        public long createGenerationTable(long Consumer_No, string Serial_Number)
        {
            String Generation_Tbl_Name = "TML_" + Serial_Number.Trim() + "_DATA";
            try
            {
                const String Generation_Table_Desc = "( READING_DATE DATE, "
                                                  + " SLOT_NO	NUMBER, "
                                                  + " KWH_UNITS	NUMBER, "
                                                  + " MF	    NUMBER, "
                                                  + " MF_KWH_UNITS	NUMBER, "
                                                  + " PCT  	        NUMBER, "
                                                  + " PCT_KWH_UNITS	NUMBER, "
                                                  + " EPA_CAP	    NUMBER, "
                                                  + " KWH_UNIT_AFT_EPA	NUMBER, "
                                                  + " DISTR_LOSS_PCT    NUMBER, "
                                                  + " AFT_DIST_LOSS	NUMBER, "
                                                  + " TRANS_LOSS_PCT    NUMBER, "
                                                  + " AFT_TRANS_LOSS	NUMBER, "
                                                  + " FINAL_KWH_UNITS	NUMBER )";

                var GenerationSerQry = this.TML_GENERATION_UNITS.Where(GenerationInfo => GenerationInfo.CONSUMER_NO == Consumer_No
                    && GenerationInfo.SERIAL_NO == Serial_Number).FirstOrDefault();
                long GEN_UNO = 0;
                if(GenerationSerQry == null)
                {
                    GEN_UNO = this.Database.SqlQuery<long>("SELECT GEN_UNO_SEQ.NEXTVAL FROM DUAL").FirstOrDefault<long>();
                    TML_GENERATION_UNITS newGenrtUnit = new TML_GENERATION_UNITS();
                    newGenrtUnit.GEN_UNO = GEN_UNO;
                    newGenrtUnit.CONSUMER_NO = Consumer_No;
                    newGenrtUnit.CREATED_ON = DateTime.Now;
                    newGenrtUnit.GENERATION_TYPE = "";
                    newGenrtUnit.SERIAL_NO = Serial_Number;
                    this.Entry(newGenrtUnit).State = System.Data.Entity.EntityState.Added;
                    this.SaveChanges();
                }
                else
                {
                    GEN_UNO = GenerationSerQry.GEN_UNO;
                }
                var tblQry = this.Database.SqlQuery<string>("SELECT TABLE_NAME FROM USER_TABLES WHERE TABLE_NAME = '" + Generation_Tbl_Name + "'").FirstOrDefault();
                if ( (tblQry == null) || (tblQry.Count() <= 0) )
                {
                    this.Database.ExecuteSqlCommand("CREATE TABLE " + Generation_Tbl_Name + Generation_Table_Desc);
                }
                return GEN_UNO;
            }
            catch(Exception tblCreaExp)
            {
                Debug.WriteLine("Error raised @Table Creation of " + Generation_Tbl_Name + " due to " + tblCreaExp.ToString());
                return 0;
            }
        }

        public long checkConsumptionSerialNo(long Consumer_No, string SerialNo)
        {
            try
            {
                var ConUnoQry = this.TML_CONSUMPTION_UNITS.Where(ConsUnoData => ConsUnoData.CONSUMER_NO == Consumer_No && ConsUnoData.SERIAL_NO == SerialNo).FirstOrDefault();
                if(ConUnoQry == null)
                {
                    long CON_UNO = this.Database.SqlQuery<long>("SELECT CON_UNO_SEQ.NEXTVAL FROM DUAL").FirstOrDefault<long>();
                    TML_CONSUMPTION_UNITS newCons = new TML_CONSUMPTION_UNITS();
                    newCons.CON_UNO = CON_UNO;
                    newCons.CONSUMER_NO = Consumer_No;
                    newCons.SERIAL_NO = SerialNo;
                    newCons.CREATED_ON = DateTime.Now;
                    this.Entry(newCons).State = System.Data.Entity.EntityState.Added;
                    this.SaveChanges();
                    return CON_UNO;
                }
                else
                {
                    return ConUnoQry.CON_UNO;
                }
            }
            catch(Exception tblCosnumpExp)
            {
                Debug.WriteLine("Error raised @CheckConsumptionSerialNo due to " + tblCosnumpExp.ToString());
                return 0;
            }
        }

    }
}
