using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TataMotorsWebAPI.Common
{
    public class Constants
    {
        #region Open Acces Bill related constants

        public const string cTotalDrawalUnits = "Total Drawal Units";
        public const string cTotalInjectionUnits = "Total Injection Units";
        public const string cMSEDCLTariff = "MSEDCL Tariff";
        public const string cTempTariff = "Temp Tariff";
        public const string cOffsetAgainstDrawal = "Offset Against Drawal";
        public const string cCurrentMonthGeneration = "Current Month Generation";
        public const string cLastMonthBanked = "Last Month Banked";
        public const string cOverInjectedUnits = "Over Injected Units";
        public const string cBilledDemand = "Billed Demand";
        public const string cHighestRecordedDemand = "Highest Recorded Demand";
        public const string cRKVAH = "RKVAH";
        public const string cPF = "PF";
        public const string cLF = "LF";
        public const string cAZone = "AZone";
        public const string cBZone = "BZone";
        public const string cCZone = "CZone";
        public const string cDZone = "DZone";
        
        public const string cDemandCharge = "Demand Charges";
        public const string cEnergyCharges = "Energy Charges";
        public const string cTODTariffEC =  "TOD Tariff EC";
        public const string cFACCharges =  "FAC Charges";
        public const string cEHVRebate  =  "EHV Rebate";
        public const string cInfraCharge = "Infra Charge";
        public const string cElectricityDuty = "Electricity Duty";
        public const string cTaxOnSale =  "Tax On Sale";
        public const string cPFPenaltyIncentive =  "PF Penalty/Incentive";
        public const string cDemandPenalty = "Demand Penalty";
        public const string cRLCRefund = "RLC Refund";
        public const string cCrossSubsidySurcharge = "Cross Subsidy Surcharge";
        public const string cWheelingcharges =  "Wheeling charges";
        public const string cTransmissionCharges = "Transmission Charges";
        public const string cOperatingCharges = "Operating Charges";
        public const string cDebitAdjustments  = "Debit Adjustments";
        // Above all fields are common for MSEDCL & Open Access in Open Access Bill File

        public const string cMSEDCLTotalBill = "Total Bill for MSEDCL consumption (A)";
        public const string cOpenAccessTotalBill = "Total Bill for Open Access (B)";
        #endregion

    }
}