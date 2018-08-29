namespace TataMotorsWebAPI.Common
{
    public static class ErrorCodes
    {
        public static int UnableToProcessConsPDFFile = 110;
        public static int InvalidConsumptionFileSelected = 111;
        public static int UnableToLoadConsumptionExcelFile = 112;
        public static int UnableToProcessConsumptionExcelFile = 113;

        public static int UnableToProcessGenPDFFile = 210;
        public static int InvalidGenerationFileSelected = 211;
        public static int UnableToLoadGenerationExcelFile = 212;
        public static int UnableToProcessGenerationExcelFile = 213;

        public static int UnableToProcessTimeSlotConsPDFFile = 310;
        public static int InvalidTimeSlotConsFileSelected = 311;
        public static int UnableToLoadTimeSlotConsExcelFile = 312;
        public static int UnableToProcessTimeSlotConsExcelFile = 313;

        public static int UnableToProcessMeterConsPDFFile = 410;
        public static int InvalidMeterConsFileSelected = 411;
        public static int UnableToLoadMeterConsExcelFile = 412;
        public static int UnableToProcessMeterConsExcelFile = 413;

        public static int UnableToProcessOpenAccessBillPDFFile = 510;
        public static int InvalidOpenAccessBillFileSelected = 511;
        public static int UnableToLoadOpenAccessBillExcelFile = 512;
        public static int UnableToProcessOpenAccessExcelFile = 513;


        public static int AllFileContainsDifferentMonths = 910;  // If all files (Consumption, Generation, TimeSlotConsumption) contains different        
        public static int UnableToConvertFile = 911;       
        public static int UnableToConnDB = 912;
        public static int UnableToExecuteQry = 913;      
        public static int CompltedWithOverwrite = 914;  // Overwrite checked by user & (Overwrite & New Month) successfully completed
        public static int CompltedWithoutOverwrite = 915; // Overwrite not checked by user & Data is already existed & send successfully completed..        
        public static int UnableToProcess = 916;
        public static int ConvertTimedOut = 917;   // Error occurred Timed out operation of PDF To Excel Conversion
        public static int UnableToSaveAdditionalParamsInfo = 918;
    }
}