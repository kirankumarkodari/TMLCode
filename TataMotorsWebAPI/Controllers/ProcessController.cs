using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Hosting;
using System.Configuration;
using System.IO;
using System.Collections;
using System.Threading;
using System.Data.Entity;
using Microsoft.AspNet.SignalR;
using System.Data;
using System.Data.OleDb;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using TataMotorsWebAPI.Models;
using TataMotorsWebAPI.Models.DTOClasses;
using TataMotorsWebAPI.Common;
using TataMotorsWebAPI.Hubs;
using System.Diagnostics;

namespace TataMotorsWebAPI.Controllers
{
    [RoutePrefix("Services/Process")]
    public class ProcessController : ApiController
    {
        public enum FileTypes { ConsumptionFile, GenerationFile, TimeSlotConsumptionFile, MeterConsumptionFile, OpenAccessBillFile };
        private enum ConsumptionRowType { None, DateTotal, MTRTotal, ConsTotal, MaxMFKVA, Other };
        private enum GenerationRowType { None, DateTotal, MTRTotal, ConsTotal, Other };

        private Dictionary<string, string> openAccessSummaryData = new Dictionary<string, string>()
        {
            {Constants.cTotalDrawalUnits, "0"},
            {Constants.cTotalInjectionUnits,"0"},
            {Constants.cMSEDCLTariff,"0"},
            {Constants.cTempTariff,"0"},
            {Constants.cOffsetAgainstDrawal,"0" },
            {Constants.cCurrentMonthGeneration,"0" },
            {Constants.cLastMonthBanked, "0" },
            {Constants.cOverInjectedUnits, "0" },
            {Constants.cBilledDemand,"0" },
            {Constants.cHighestRecordedDemand,"0" },
            {Constants.cRKVAH,"0" },
            {Constants.cPF,"0" },
            {Constants.cLF,"0" },
            {Constants.cAZone,"0" },
            {Constants.cBZone,"0" },
            {Constants.cCZone, "0" },
            {Constants.cDZone, "0" }

        };

        private Dictionary<string, string> openAccessBillFiledsMSEDCLList = new Dictionary<string, string>()
        {
            { Constants.cDemandCharge,"0" },
            { Constants.cEnergyCharges, "0" },
            { Constants.cTODTariffEC, "0" },
            { Constants.cFACCharges, "0" },
            { Constants.cEHVRebate, "0" },
            { Constants.cInfraCharge, "0" },
            { Constants.cElectricityDuty, "0" },
            { Constants.cTaxOnSale, "0" },
            { Constants.cPFPenaltyIncentive, "0" },
            { Constants.cDemandPenalty, "0" },
            { Constants.cRLCRefund, "0" },
            { Constants.cCrossSubsidySurcharge, "0" },
            { Constants.cWheelingcharges, "0" },
            { Constants.cTransmissionCharges, "0" },
            { Constants.cOperatingCharges, "0" },
            { Constants.cDebitAdjustments, "0" }
        };

        private Dictionary<string, string> openAccessBillFiledsOAList = null;

        /*
         * In Open Access Bill File Processing have two types of formats are available. 
             1. Last Month Banked carry forward --> File contains "Current Month Generation" &&  "Last Month Banked"
             2. without carry forward  --> File does not conatins above words.....

            Based on above category, calculation process shall be done
        */
        private Boolean IsLastMonthBankedFile = true;
        private const string cLastMonthBankedKeyword = "Current Month Last Month";
        private string HIGHEST_RECORDED_DEMAND = "0";  // Value taken from CONSUMPTIOn FILE @ Max MF KVA but this is stored in TML_TS_CONSUMPTION_SUMMARY(HIGHEST_RECORDED_DEMAND)


        private enum FileProcessingState
        {
            processInit = 0, validateFiles = 1, convertingAllFiles = 2, dataAlreadyExisted = 3,
            processConsumptionFile = 4, processGenerationFile = 5,
            processTimeSlotConsumptionFile = 6, processMeterConsumptionFile = 7, processOpenAccessBillFile = 8,
            processAdditionalParamsInfo = 9, Completed = 10, Failed = 11
        };
        private FileProcessingState currProcessState;

        private enum LogType { Info, Error, Alert, Warning };

        private TATADBContext tataDBContext;
        DbContextTransaction tataDBTran = null;

        private const string Comma = ", ";
        private ArrayList processList = new ArrayList();
        private Boolean SendDebugInfoToClient = false;

        public string ConsumptionFilePath = "", GenerationFilePath = "", TimeSlotConsumptionFilePath = "",
                     MeterConsumptionFilePath = "", OpenAccessBillFilePath = "";

        private string ConsumptionFileName = "", GenerationFileName = "",
                       TimeSlotConsumptionFileName = "", MeterConsumptionFileName = "", OpenAccessBillFileName = "";

        private string consumptionMonthFileDate = "", generationMonthFileDate = "", timeSlotConsumptionMonthFileDate = "",
                       meterConsumptionMonthFileDate = "", openAccessBillMonthFileDate = "";

        private Boolean IsOverwriteExistedData = false;
        private string currDateTimeDir = "";
        private string uploadFolderPath = "";
        int errorCode = 0;

        [Route("GetAdditionalInfo")]
        [HttpGet]
        public IHttpActionResult LoadAdditionalInfo()
        {

            AdditionaInfoDTO paramInfo = new AdditionaInfoDTO();
            try
            {
                tataDBContext = new TATADBContext();
                var billRatesQry = tataDBContext.TML_BILLRATESVALUES
                                   .OrderByDescending(BillInfo => BillInfo.VAL_MONYR).FirstOrDefault();
                paramInfo.IsLoaded = 1;
                if (billRatesQry != null)
                {
                    paramInfo.MSEDCL.MD_CHARGE = billRatesQry.MD_CHARGE;
                    paramInfo.MSEDCL.BASIC_CHARGE = billRatesQry.BASIC_CHARGE;
                    paramInfo.MSEDCL.AZONEVAL = billRatesQry.AZONEVAL;
                    paramInfo.MSEDCL.BZONEVAL = billRatesQry.BZONEVAL;
                    paramInfo.MSEDCL.CZONEVAL = billRatesQry.CZONEVAL;
                    paramInfo.MSEDCL.DZONEVAL = billRatesQry.DZONEVAL;
                    paramInfo.MSEDCL.FAC = billRatesQry.FAC;
                    paramInfo.MSEDCL.ELEC_CHARGES = billRatesQry.ELEC_CHARGES;
                    paramInfo.MSEDCL.TAX_CHARGE = billRatesQry.TAX_CHARGE;
                    paramInfo.MSEDCL.PF = billRatesQry.PF;
                    paramInfo.MSEDCL.EHV = billRatesQry.EHV;
                    paramInfo.MSEDCL.PROMPTPAY = billRatesQry.PROMPTPAY;

                    paramInfo.Others.MD_CHARGE = billRatesQry.OTH_MD_CHARGE;
                    paramInfo.Others.BASIC_CHARGE = billRatesQry.OTH_BASIC_CHARGE;
                    paramInfo.Others.AZONEVAL = billRatesQry.OTH_AZONEVAL;
                    paramInfo.Others.BZONEVAL = billRatesQry.OTH_BZONEVAL;
                    paramInfo.Others.CZONEVAL = billRatesQry.OTH_CZONEVAL;
                    paramInfo.Others.DZONEVAL = billRatesQry.OTH_DZONEVAL;
                    paramInfo.Others.FAC = billRatesQry.OTH_FAC;
                    paramInfo.Others.ELEC_CHARGES = billRatesQry.OTH_ELEC_CHARGES;
                    paramInfo.Others.TAX_CHARGE = billRatesQry.OTH_TAX_CHARGE;
                    paramInfo.Others.PF = billRatesQry.OTH_PF;
                    paramInfo.Others.EHV = billRatesQry.OTH_EHV;
                    paramInfo.Others.PROMPTPAY = billRatesQry.OTH_PROMPTPAY;

                    paramInfo.TransCharges = billRatesQry.TRANS_CHARGE;
                    paramInfo.WheelingCharges = billRatesQry.WHEEL_CHARGE;
                    paramInfo.CrossCharges = billRatesQry.CROSS_SUR_CHARGE;
                    paramInfo.OthPurDiscnt = billRatesQry.OTH_PUR_DISCOUNT;
                }
                return Ok(paramInfo);
            }
            catch (Exception AdditionalInfoLoadingExp)
            {
                System.Diagnostics.Debug.WriteLine("Unable to load Additional Info due to " + AdditionalInfoLoadingExp.ToString());
                return Ok(paramInfo);
            }
        }

        [Route("uploadFiles")]
        [HttpGet, HttpPost]
        public UploadResponseDTO ProcessFiles()
        {
            //System.Diagnostics.Debug.WriteLine(HttpContext.Current.Request.Files.AllKeys.Any());             
            ProcessStateChanged(FileProcessingState.processInit, "Starting...");

            UploadResponseDTO uploadResponse = new UploadResponseDTO();
            if (HttpContext.Current.Request.Files.AllKeys.Any())
            {
                if (Enum.GetNames(typeof(FileTypes)).Length != HttpContext.Current.Request.Files.Count)
                {
                    uploadResponse.Error_Code = 1;
                    uploadResponse.Msg = String.Format("{0} files selected out of {1}",
                        HttpContext.Current.Request.Files.Count,
                        Enum.GetNames(typeof(FileTypes)).Length);
                }
                else
                {
                    SendDebugInfoToClient = Convert.ToBoolean(ConfigurationManager.AppSettings["SendDebugInfoToClient"]);
                    processList.Add(GetFormattedText(LogType.Info, ""));
                    uploadFolderPath = ConfigurationManager.AppSettings["FolderPath"];
                    currDateTimeDir = DateTime.Now.ToString("yyyyMMMddHHmm");

                    DirectoryInfo di = new DirectoryInfo(HostingEnvironment.ApplicationPhysicalPath + "\\" + uploadFolderPath);

               
                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                   
                    Directory.CreateDirectory(HostingEnvironment.ApplicationPhysicalPath + "\\" + uploadFolderPath + currDateTimeDir);


                    //get & Save Selected Consumption PDF file in UploadedFiles Folder
                    var ConsumptionFile = HttpContext.Current.Request.Files[0];
                    ConsumptionFileName = System.IO.Path.GetFileName(ConsumptionFile.FileName);
                    ConsumptionFilePath = HostingEnvironment.MapPath("~/" + uploadFolderPath + currDateTimeDir + "\\" + ConsumptionFileName);
                    ConsumptionFile.SaveAs(ConsumptionFilePath);

                    //get & Save Selected Generation PDF file in UploadedFiles Folder
                    var GenerationFile = HttpContext.Current.Request.Files[1];
                    GenerationFileName = System.IO.Path.GetFileName(GenerationFile.FileName);
                    string GenerationFolderPath = ConfigurationManager.AppSettings["FolderPath"];
                    GenerationFilePath = HostingEnvironment.MapPath("~/" + uploadFolderPath + currDateTimeDir + "\\" + GenerationFileName);
                    GenerationFile.SaveAs(GenerationFilePath);


                    //get & Save Selected TimeSlot Consumption PDF file in UploadedFiles Folder
                    var TimeSlotConsumptionFile = HttpContext.Current.Request.Files[2];
                    TimeSlotConsumptionFileName = System.IO.Path.GetFileName(TimeSlotConsumptionFile.FileName);
                    TimeSlotConsumptionFilePath = HostingEnvironment.MapPath("~/" + uploadFolderPath + currDateTimeDir + "\\" + TimeSlotConsumptionFileName);
                    TimeSlotConsumptionFile.SaveAs(TimeSlotConsumptionFilePath);

                    //get & Save Selected Meter Consumption PDF file in UploadedFiles Folder
                    var MeterConsumptionFile = HttpContext.Current.Request.Files[3];
                    MeterConsumptionFileName = System.IO.Path.GetFileName(MeterConsumptionFile.FileName);
                    MeterConsumptionFilePath = HostingEnvironment.MapPath("~/" + uploadFolderPath + currDateTimeDir + "\\" + MeterConsumptionFileName);
                    MeterConsumptionFile.SaveAs(MeterConsumptionFilePath);

                    //get & Save Selected Open Acess Bill PDF file in UploadedFiles Folder
                    var OpenAccessBillFile = HttpContext.Current.Request.Files[4];
                    OpenAccessBillFileName = System.IO.Path.GetFileName(OpenAccessBillFile.FileName);
                    OpenAccessBillFilePath = HostingEnvironment.MapPath("~/" + uploadFolderPath + currDateTimeDir + "\\" + OpenAccessBillFileName);
                    OpenAccessBillFile.SaveAs(OpenAccessBillFilePath);


                    IsOverwriteExistedData = Convert.ToBoolean(HttpContext.Current.Request.Form["IsOverwrite"]);
                    processList.Add(GetFormattedText(LogType.Info, "5 Files Saved"));
                    initProcessing();
                    uploadResponse.Error_Code = 2;
                    uploadResponse.Msg = "Process Completed";
                }
            }
            else
            {
                uploadResponse.Error_Code = 0;
                uploadResponse.Msg = "No files selected";
            }
            return uploadResponse;
        }

        public void initProcessing()
        {
            try
            {
                processList.Add(GetFormattedText(LogType.Info, "Validating files"));
                ProcessStateChanged(FileProcessingState.validateFiles, "Validating Files");

                processList.Add(GetFormattedText(LogType.Info, "Directory Created"));
                if (IsValidPDFFile(FileTypes.ConsumptionFile) && IsValidPDFFile(FileTypes.GenerationFile) &&
                    IsValidPDFFile(FileTypes.TimeSlotConsumptionFile) && IsValidPDFFile(FileTypes.MeterConsumptionFile) &&
                    IsValidPDFFile(FileTypes.OpenAccessBillFile))
                {

                    if (AllFilesAreSameMonth())
                    {
                        File.WriteAllText(HostingEnvironment.ApplicationPhysicalPath + "\\"
                            + uploadFolderPath + currDateTimeDir + "\\" + "Status.txt", "State=Init,Status=ToBeStart");

                        TextWriter tsw = new StreamWriter(HostingEnvironment.ApplicationPhysicalPath + "\\" + uploadFolderPath + "ProcessFiles.txt", true);
                        tsw.WriteLine(String.Format("DIR={0},CON={1},GEN={2},TSC={3},MCON={4},OAB={5}",
                            currDateTimeDir, ConsumptionFileName, GenerationFileName,
                            TimeSlotConsumptionFileName, MeterConsumptionFileName, OpenAccessBillFileName));
                        tsw.Close();

                        processList.Add(GetFormattedText(LogType.Info, "File Conversion Started"));

                        if (waitForConvertPDFFiles())
                        {
                            try
                            {
                                tataDBContext = new TATADBContext();
                                tataDBTran = tataDBContext.Database.BeginTransaction();
                                processList.Add(GetFormattedText(LogType.Info, "DB Connected"));
                                if (IsDataAvailableInDB())
                                {
                                    if (IsOverwriteExistedData)
                                    {
                                        processList.Add(GetFormattedText(LogType.Info, "'" + consumptionMonthFileDate
                                            + "' month data is already existed, Deleting existed data"));
                                        ProcessStateChanged(FileProcessingState.dataAlreadyExisted, "Overwriting \""
                                            + consumptionMonthFileDate + "\"" + " month data....");
                                        DeleteExistedRecords(consumptionMonthFileDate);
                                        errorCode = ErrorCodes.CompltedWithOverwrite;
                                        savePDFFiles();
                                    }
                                    else
                                    {
                                        errorCode = ErrorCodes.CompltedWithoutOverwrite;
                                        ProcessStateChanged(FileProcessingState.Completed, "\"" + consumptionMonthFileDate + "\" month data is already existed");
                                    }
                                }
                                else
                                {
                                    processList.Add(GetFormattedText(LogType.Info, "'" + consumptionMonthFileDate + "' month data is not available. So start data saving"));
                                    savePDFFiles();
                                }
                            }
                            catch (Exception dbConnectExp)
                            {
                                processList.Add(GetFormattedText(LogType.Error, "Unable to connect DB due to " + dbConnectExp.ToString()));
                                RollbackTransaction();
                                errorCode = ErrorCodes.UnableToConnDB;
                                ProcessStateChanged(FileProcessingState.Failed, "Oops! Unable to Process. Please try again later");
                            }
                        }
                        else
                        {
                            errorCode = ErrorCodes.UnableToConvertFile;
                            processList.Add(GetFormattedText(LogType.Info, "Unable to Process"));
                            RollbackTransaction();
                            ProcessStateChanged(FileProcessingState.Failed, "Oops! Unable to Process. Please try again later");
                        }
                    }
                    else  // Month names not same of all files
                    {
                        errorCode = ErrorCodes.AllFileContainsDifferentMonths;
                        processList.Add(GetFormattedText(LogType.Info, "Files contains different months data"));
                        ProcessStateChanged(FileProcessingState.Failed, "Files contains different months data");
                    }
                }
            }
            catch (Exception initProcessExp)
            {
                RollbackTransaction();
                processList.Add(GetFormattedText(LogType.Error, "Unable to process due to " + initProcessExp.ToString()));
                errorCode = ErrorCodes.UnableToProcess;
                ProcessStateChanged(FileProcessingState.Failed, "Oops! Unable to Process. Please try again later");
            }
        }

        private Boolean AllFilesAreSameMonth()
        {
            /*
             *  Converting String to Date Format for comparision of all files are same month or not
             *  Consumption, Generation, Time Slot Consumption Date Format is :- MMM-YY
             *  Meter Consumption, Open Access Bill Date Format is :- MMM-YYYY  
             */

            DateTime consumptionFileDate = DateTime.Parse("01-" + consumptionMonthFileDate);
            DateTime generationFileDate = DateTime.Parse("01-" + generationMonthFileDate);
            DateTime timeSlotConsuFileDate = DateTime.Parse("01-" + timeSlotConsumptionMonthFileDate);
            DateTime meterConsFileDate = DateTime.Parse(meterConsumptionMonthFileDate);
            DateTime openAccessBillFileDate = DateTime.Parse(openAccessBillMonthFileDate);

            if ((consumptionFileDate.CompareTo(generationFileDate) == 0) &&
                 (consumptionFileDate.CompareTo(timeSlotConsuFileDate) == 0) &&
                 (consumptionFileDate.CompareTo(meterConsFileDate) == 0) &&
                 (consumptionFileDate.CompareTo(openAccessBillFileDate) == 0))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void RollbackTransaction()
        {
            try
            {
                if (tataDBTran != null)
                {
                    tataDBTran.Rollback();
                    tataDBTran.Dispose();
                }
            }
            catch (Exception tranRollbackExp)
            {
                processList.Add(GetFormattedText(LogType.Error, "Error occurred while transaction rollback due to " + tranRollbackExp.ToString()));
            }
        }

        private void savePDFFiles()
        {
            processList.Add(GetFormattedText(LogType.Info, "Consumption Excel File Processing Initiated"));

            ProcessStateChanged(FileProcessingState.processConsumptionFile, "Processing Consumption File");
            Import_To_ConsumptionDatabase(ConsumptionFilePath.ToLower().Replace(".pdf", ".xls"));
            if (currProcessState == FileProcessingState.processConsumptionFile)
            {
                processList.Add(GetFormattedText(LogType.Info, "Consumption Excel File Processing Completed"));

                processList.Add(GetFormattedText(LogType.Info, "Generation Excel File Processing Initiated"));
                ProcessStateChanged(FileProcessingState.processGenerationFile, "Processing Generation File");
                Import_To_GenerationDatabase(GenerationFilePath.ToLower().Replace(".pdf", ".xls"));
            }

            if (currProcessState == FileProcessingState.processGenerationFile)
            {
                processList.Add(GetFormattedText(LogType.Info, "Generation Excel File Processing Completed"));
                processList.Add(GetFormattedText(LogType.Info, "Timeslot Consumption Excel File Processing Initiated"));
                ProcessStateChanged(FileProcessingState.processTimeSlotConsumptionFile, "Processing Timeslot Consumption File");
                Import_To_TSCDatabase(TimeSlotConsumptionFilePath.ToLower().Replace(".pdf", ".xls"));
            }

            if (currProcessState == FileProcessingState.processTimeSlotConsumptionFile)
            {
                processList.Add(GetFormattedText(LogType.Info, "Timeslot Consumption Excel File Processing Completed"));
                processList.Add(GetFormattedText(LogType.Info, "Meter Consumption Excel File Processing Initiated"));
                ProcessStateChanged(FileProcessingState.processMeterConsumptionFile, "Processing Meter Consumption File");
                Import_To_MTR_CONSDatabase(MeterConsumptionFilePath.ToLower().Replace(".pdf", ".xls"));
            }

            if (currProcessState == FileProcessingState.processMeterConsumptionFile)
            {
                processList.Add(GetFormattedText(LogType.Info, "Meter Consumption Excel File Processing Completed"));
                processList.Add(GetFormattedText(LogType.Info, "Open Access Bill Excel File Processing Initiated"));
                ProcessStateChanged(FileProcessingState.processOpenAccessBillFile, "Processing Open Access Bill File");
                Import_To_Open_Access_BillDatabase(OpenAccessBillFilePath.ToLower().Replace(".pdf", ".xls"));
            }

            if (currProcessState == FileProcessingState.processOpenAccessBillFile)
            {
                processList.Add(GetFormattedText(LogType.Info, "Open Access Bill Excel File Processing Completed"));
                processList.Add(GetFormattedText(LogType.Info, "Additional Paramaters Info Saving Initiated"));
                ProcessStateChanged(FileProcessingState.processAdditionalParamsInfo, "Timeslot Consumption File");
                SaveAdditionParamsInfo();
                processList.Add(GetFormattedText(LogType.Info, "Additional Paramaters Info Saving Completed"));
                if (currProcessState == FileProcessingState.processAdditionalParamsInfo)
                {
                    if (tataDBTran != null)
                    {
                        tataDBTran.Commit();
                    }
                    ProcessStateChanged(FileProcessingState.Completed, "Saved Successfully");
                }
            }
        }

        private Boolean waitForConvertPDFFiles()
        {
            try
            {
                TimeSpan timeOut = TimeSpan.FromMinutes(15);  // Set as Time Out 15min 
                int elapsed = 0;
                string curStatus = "";
                string stsText = "", strState = "";
                Boolean isConverted = false;
                do
                {
                    Thread.Sleep(1000);
                    elapsed += 1000;
                    //string stsText = File.ReadAllText(HostingEnvironment.ApplicationPhysicalPath + "\\" + uploadFolderPath + currDateTimeDir + "\\" + "Status.txt");
                    //lastline.Split(',').Select(x => x.Split('=')).ToDictionary(x => x[0], x => x[1]);
                    Dictionary<string, string> processFileList = null;
                    try
                    {
                        var inStream = new FileStream(HostingEnvironment.ApplicationPhysicalPath + "\\" + uploadFolderPath + currDateTimeDir + "\\" + "Status.txt",
                            FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        using (var textReader = new StreamReader(inStream))
                        {
                            stsText = textReader.ReadToEnd();
                        }
                        inStream.Close();
                        if (!String.IsNullOrEmpty(stsText))
                            processFileList = stsText.Split(',').Select(x => x.Split('=')).ToDictionary(x => x[0], x => x[1]);
                    }
                    catch (Exception stsSplitExp)
                    {
                        Debug.WriteLine("Error raised @ Wait For Convert Files due to " + stsSplitExp.ToString());
                    }


                    if ((processFileList != null) && (processFileList.ContainsKey("Status")))
                    {
                        processList.Add(GetFormattedText(LogType.Info, processFileList["Status"]));
                        curStatus = processFileList["Status"];
                    }
                    else
                    {
                        processList.Add(GetFormattedText(LogType.Info, "Unable to find some keyword"));
                    }

                    if (curStatus.Contains("Completed"))
                    {
                        isConverted = true;
                        break;
                    }
                    else if (curStatus.Contains("Failed"))
                    {
                        isConverted = false;
                        break;
                    }
                    else
                    {
                        if ((processFileList != null) && (processFileList.ContainsKey("State")))
                        {
                            if (!curStatus.Equals(processFileList["State"]))
                            {
                                switch (processFileList["State"])
                                {
                                    case "CONS":
                                        processList.Add("Reading Consumption File...");
                                        ProcessStateChanged(FileProcessingState.convertingAllFiles, "Reading Consumption File...");
                                        break;

                                    case "GEN":
                                        processList.Add("Reading Generation File...");
                                        ProcessStateChanged(FileProcessingState.convertingAllFiles, "Reading Generation File...");
                                        break;

                                    case "TSC":
                                        processList.Add("Reading Time Slot Consumption File...");
                                        ProcessStateChanged(FileProcessingState.convertingAllFiles, "Reading Time Slot Consumption File...");
                                        break;

                                    case "MCON":
                                        processList.Add("Reading Meter Consumption File...");
                                        ProcessStateChanged(FileProcessingState.convertingAllFiles, "Reading Meter Consumption File...");
                                        break;

                                    case "OAB":
                                        processList.Add("Reading Open Access Bill File...");
                                        ProcessStateChanged(FileProcessingState.convertingAllFiles, "Reading Open Access Bill File...");
                                        break;

                                }
                                curStatus = processFileList["State"];
                            }
                        }
                    }

                } while (elapsed < timeOut.TotalMilliseconds);

                if (isConverted)
                {
                    return true;
                }
                else if (curStatus.Contains("Failed"))
                {
                    processList.Add(GetFormattedText(LogType.Info, curStatus));
                    errorCode = ErrorCodes.UnableToConvertFile;
                    return false;
                }
                else
                {
                    processList.Add(GetFormattedText(LogType.Info, "Convert Timed out"));
                    errorCode = ErrorCodes.ConvertTimedOut;
                    return false;
                }
            }
            catch (Exception waitPDFConverExp)
            {
                processList.Add(GetFormattedText(LogType.Info, "Error raised at waitForConvertPDFFile due to " + waitPDFConverExp.ToString()));
                errorCode = ErrorCodes.UnableToConvertFile;
                return false;
            }
        }

        private Boolean IsValidPDFFile(FileTypes fileType)
        {
            try
            {
                System.Text.StringBuilder text = new System.Text.StringBuilder();
                string filePath = ""; string fileContains = "", splitText = "", cReadingDate = "";
                if (fileType == FileTypes.ConsumptionFile)
                {
                    filePath = ConsumptionFilePath;
                    fileContains = "CONSUMER SIDE";
                    splitText = "Slot";
                    cReadingDate = "Reading Date:";
                }
                else if (fileType == FileTypes.GenerationFile)
                {
                    filePath = GenerationFilePath;
                    fileContains = "GENERATION SIDE";
                    splitText = "Slot";
                    cReadingDate = "Reading Date:";
                }
                else if (fileType == FileTypes.TimeSlotConsumptionFile)
                {
                    filePath = TimeSlotConsumptionFilePath;
                    fileContains = "TIME SLOT CONSUMPTION";
                    splitText = "Reading";
                    cReadingDate = "Reading Date :";
                }
                else if (fileType == FileTypes.MeterConsumptionFile)
                {
                    filePath = MeterConsumptionFilePath;
                    fileContains = "OPEN ACCESS METER CONSUMPTION";
                    splitText = " Page";
                    cReadingDate = "FOR THE MONTH";
                }
                else // Open Access Bill File
                {
                    filePath = OpenAccessBillFilePath;
                    fileContains = "Maharashtra State Electricity Distribution";
                    splitText = "Last Month";
                    cReadingDate = "Bill Month";
                }

                using (PdfReader reader = new PdfReader(filePath))
                {
                    for (int i = 1; i <= reader.NumberOfPages; i++)
                    {
                        text.Append(PdfTextExtractor.GetTextFromPage(reader, i));
                        if (text.ToString().Contains(fileContains))
                        {
                            if (text.ToString().Contains(cReadingDate))
                            {
                                String ReadingDate = text.ToString();
                                ReadingDate = ReadingDate.Split(new string[] { cReadingDate },
                                    StringSplitOptions.None)[1].Split(new string[] { splitText }, StringSplitOptions.None)[0].Trim();
                                ReadingDate = ReadingDate.Replace(":", "");

                                if ((fileType == FileTypes.ConsumptionFile) ||
                                     (fileType == FileTypes.GenerationFile) ||
                                     (fileType == FileTypes.TimeSlotConsumptionFile))
                                {
                                    ReadingDate = ReadingDate.Substring(3, 6);  // fetch MMM-YY from DD-MMM-YY
                                }

                                if (fileType == FileTypes.ConsumptionFile)
                                {
                                    consumptionMonthFileDate = ReadingDate;
                                }
                                else if (fileType == FileTypes.GenerationFile)
                                {
                                    generationMonthFileDate = ReadingDate;
                                }
                                else if (fileType == FileTypes.TimeSlotConsumptionFile)
                                {
                                    timeSlotConsumptionMonthFileDate = ReadingDate;
                                }
                                else if (fileType == FileTypes.MeterConsumptionFile)
                                {
                                    meterConsumptionMonthFileDate = ReadingDate;
                                }
                                else if (fileType == FileTypes.OpenAccessBillFile)
                                {
                                    openAccessBillMonthFileDate = ReadingDate;
                                }
                                break;
                            }
                        }
                        else  // If file does not contain "fileContains (see in variable)" word in concern file
                        {
                            if (fileType == FileTypes.ConsumptionFile)
                            {
                                errorCode = ErrorCodes.InvalidConsumptionFileSelected;
                                processList.Add(GetFormattedText(LogType.Info, "Invalid Consumption file selected"));
                                ProcessStateChanged(FileProcessingState.Failed, "Invalid Consumption file selected");
                            }
                            else if (fileType == FileTypes.GenerationFile)
                            {
                                errorCode = ErrorCodes.InvalidGenerationFileSelected;
                                processList.Add(GetFormattedText(LogType.Info, "Invalid Generation file selected"));
                                ProcessStateChanged(FileProcessingState.Failed, "Invalid Generation file selected");
                            }
                            else if (fileType == FileTypes.TimeSlotConsumptionFile)
                            {
                                errorCode = ErrorCodes.InvalidTimeSlotConsFileSelected;
                                processList.Add(GetFormattedText(LogType.Info, "Invalid Time Slot Consumption file selected"));
                                ProcessStateChanged(FileProcessingState.Failed, "Invalid Time Slot Consumption file selected");
                            }
                            else if (fileType == FileTypes.MeterConsumptionFile)
                            {
                                errorCode = ErrorCodes.InvalidMeterConsFileSelected;
                                processList.Add(GetFormattedText(LogType.Info, "Invalid Meter Consumption file selected"));
                                ProcessStateChanged(FileProcessingState.Failed, "Invalid Meter Consumption file selected");
                            }
                            else if (fileType == FileTypes.OpenAccessBillFile)
                            {
                                errorCode = ErrorCodes.InvalidOpenAccessBillFileSelected;
                                processList.Add(GetFormattedText(LogType.Info, "Invalid Open Access Bill file selected"));
                                ProcessStateChanged(FileProcessingState.Failed, "Invalid Open Access Bill file selected");
                            }
                            return false;
                        }
                    }
                }

                if (fileType == FileTypes.OpenAccessBillFile)
                {
                    IsLastMonthBankedFile = text.ToString().Contains(cLastMonthBankedKeyword);
                }

                return true;
            }
            catch (Exception verifyPDFFiles)
            {
                processList.Add(GetFormattedText(LogType.Error, "Unable to load PDF files (Before Processing) due to " + verifyPDFFiles.ToString()));
                if (fileType == FileTypes.ConsumptionFile)
                {
                    errorCode = ErrorCodes.UnableToProcessConsPDFFile;
                }
                else if (fileType == FileTypes.GenerationFile)
                {
                    errorCode = ErrorCodes.UnableToProcessGenPDFFile;
                }
                else if (fileType == FileTypes.TimeSlotConsumptionFile)
                {
                    errorCode = ErrorCodes.UnableToProcessTimeSlotConsPDFFile;
                }
                else if (fileType == FileTypes.MeterConsumptionFile)
                {
                    errorCode = ErrorCodes.UnableToProcessMeterConsPDFFile;
                }
                else if (fileType == FileTypes.OpenAccessBillFile)
                {
                    errorCode = ErrorCodes.UnableToProcessOpenAccessBillPDFFile;
                }
                RollbackTransaction();
                processList.Add(GetFormattedText(LogType.Info, "Oops! Unable to Process. Please try again later"));
                ProcessStateChanged(FileProcessingState.Failed, "Oops! Unable to Process. Please try again later");
                return false;
            }
        }

        private Boolean IsDataAvailableInDB()
        {            
            try
            {
                int ConsumpRecCnt = tataDBContext.Database.SqlQuery<int>
                    ("SELECT COUNT(*) FROM TML_CONSUMPTION_DATA WHERE TO_CHAR(READING_DATE,'MON-YY') = '" + consumptionMonthFileDate + "'").FirstOrDefault();
                if (ConsumpRecCnt > 0)
                {
                    processList.Add(GetFormattedText(LogType.Info, "'" + consumptionMonthFileDate + "' month data is available in Consumption"));
                    return true;
                }
                else
                {
                    int GenRecCnt = tataDBContext.Database.SqlQuery<int>
                                        ("SELECT COUNT(*) FROM TML_GENERATION_SUMMARY WHERE TO_CHAR(READING_DATE,'MON-YY') = '"
                                        + generationMonthFileDate + "'").FirstOrDefault();
                    if (GenRecCnt > 0)
                    {
                        processList.Add(GetFormattedText(LogType.Info, "'" + consumptionMonthFileDate + "' month data is available in Generation"));
                        return true;
                    }
                    else
                    {
                        int BillRateValesCnt = tataDBContext.Database.SqlQuery<int>("SELECT COUNT(*) FROM TML_BILLRATESVALUES WHERE TO_CHAR(VAL_MONYR,'MON-YY') = '"
                            + consumptionMonthFileDate + "'").FirstOrDefault();
                        if (BillRateValesCnt > 0)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

            }
            catch (Exception dataAvailableChkExp)
            {
                processList.Add(GetFormattedText(LogType.Info, "Unable to check dates from DB due to " + dataAvailableChkExp.ToString()));
                ProcessStateChanged(FileProcessingState.Failed, "Oops! Unable to Process. Please try again later");
                RollbackTransaction();
                return false;
            }
        }

        private string GetFormattedText(LogType logType, string cmdText)
        {
            string formattedText = (DateTime.Now.ToString("HH:mm:ss.fff @ ") + cmdText);
            if (SendDebugInfoToClient)
            {
                sendMessageToClient(cmdText);
            }
            System.Diagnostics.Debug.WriteLine(formattedText);
            return formattedText;
        }

        #region Consumption_File_Processing
        //Read and Import Consumption Excel sheet data into Database
        private void Import_To_ConsumptionDatabase(string FilePath)
        {
            try
            {
                long ConsumerNumber = 0;
                string SerialNumber = "";
                string ConsumeDate = "";

                int RowLoopvar = 0;
                int ColLoopvar = 0;
                List<string> RecordsList = new List<string>();  //list to save reading records temporarly
                bool strtRecordsave = false;  //used to know records start in reading file                


                // DB Related Variables
                long CON_UNO = 0;

                processList.Add(GetFormattedText(LogType.Info, "Consumption Excel File Loading Initiated"));
                DataTable dt = LoadExcelFile(FilePath, FileTypes.ConsumptionFile);
                processList.Add(GetFormattedText(LogType.Info, "Consumption Excel File Loading Completed"));

                if (dt != null)
                {
                    try
                    {
                        #region ProcessingConsumFile
                        //Read Data table Data added by Eswar 16 AUg 2016
                        ConsumptionRowType currRowType = ConsumptionRowType.None;
                        string Recordvalue;
                        processList.Add(GetFormattedText(LogType.Info, "Consumption " + dt.Rows.Count + " rows founded"));
                        Random randomNum = new Random(60);
                        int processIndicatorVal = randomNum.Next(60, 100);
                        for (RowLoopvar = 0; RowLoopvar < dt.Rows.Count; RowLoopvar++)
                        {
                            if ((RowLoopvar % processIndicatorVal) == 0)
                            {
                                processList.Add(GetFormattedText(LogType.Info, "Consumption -  " + RowLoopvar + " out of " +
                                    dt.Rows.Count + " rows processed"));
                                ProgressUpdate(Math.Round(Decimal.Divide(RowLoopvar, dt.Rows.Count) * 100, 0));
                            }
                            RecordsList.Clear();
                            for (ColLoopvar = 1; ColLoopvar < dt.Columns.Count; ColLoopvar++)
                            {
                                string value = dt.Rows[RowLoopvar][ColLoopvar].ToString();
                                //if loop detects records start then below if executes
                                if (strtRecordsave)
                                {
                                    if (dt.Rows[RowLoopvar][1].ToString() == "")
                                    {
                                        strtRecordsave = false;  //Set to FALSE to stop records saving

                                    }
                                    else
                                    {
                                        if (dt.Rows[RowLoopvar][ColLoopvar].ToString().Contains("Date Total"))
                                        {
                                            currRowType = ConsumptionRowType.DateTotal;
                                        }
                                        else if (dt.Rows[RowLoopvar][ColLoopvar].ToString().Contains("MTR Total"))
                                        {
                                            /* 
                                             * Temporary not saved into DB becoz of MTR row values not loaded properly Final Kwh MF kw & Final Kvah in some files. 
                                             */
                                            currRowType = ConsumptionRowType.MTRTotal;
                                        }
                                        else if (dt.Rows[RowLoopvar][ColLoopvar].ToString().Contains("Cons Total"))
                                        {
                                            currRowType = ConsumptionRowType.ConsTotal;
                                        }
                                        else if (dt.Rows[RowLoopvar][ColLoopvar].ToString().Contains("Max MF KVA"))
                                        {
                                            currRowType = ConsumptionRowType.MaxMFKVA;
                                        }
                                        else
                                        {
                                            currRowType = ConsumptionRowType.None;
                                        }

                                        switch (currRowType)
                                        {
                                            case ConsumptionRowType.DateTotal:
                                            case ConsumptionRowType.MTRTotal:
                                            case ConsumptionRowType.ConsTotal:

                                                int TotalRowLoop = RowLoopvar;
                                                for (int Totalloop = 2; Totalloop < 25; Totalloop++)
                                                {
                                                    Recordvalue = dt.Rows[TotalRowLoop][Totalloop].ToString();
                                                    if (String.IsNullOrEmpty(Recordvalue))
                                                    {
                                                        RecordsList.Add("0");
                                                    }
                                                    else
                                                    {
                                                        RecordsList.Add(Recordvalue.Replace(",", ""));
                                                    }

                                                    if (Totalloop == 24)
                                                    {
                                                        //string testdummy = DatetotalRecordList.ElementAt(0);
                                                        //Database saved Values Indexes 0,1,2,8,9,10,14,15,16,18,19,20
                                                        if (currRowType != ConsumptionRowType.MTRTotal)
                                                        {
                                                            SaveConsumptionSummary(RecordsList, ConsumeDate, currRowType, CON_UNO);
                                                        }
                                                        RecordsList.Clear();    //clear List after single record saved. 
                                                    }
                                                }
                                                RowLoopvar = RowLoopvar + 1;
                                                ColLoopvar = 0;
                                                break;


                                            case ConsumptionRowType.MaxMFKVA:  // Saved in TML_TS_CONSUMPTION_SUMMARY (HIGHEST_RECORDED_DEMAND)

                                                int MAXRowLoop = RowLoopvar;
                                                //save Cons Total Record in Datetotalrecrodlist
                                                for (int Totalloop = 2; Totalloop < 25; Totalloop++)
                                                {
                                                    Recordvalue = dt.Rows[MAXRowLoop][Totalloop].ToString();
                                                    if (String.IsNullOrEmpty(Recordvalue))
                                                    {
                                                        RecordsList.Add("0");
                                                    }
                                                    else
                                                    {
                                                        RecordsList.Add(Recordvalue.Replace(",", ""));
                                                    }
                                                    if (Totalloop == 24)
                                                    {
                                                        HIGHEST_RECORDED_DEMAND = dt.Rows[RowLoopvar][ColLoopvar + 4].ToString();                                                       
                                                    }
                                                }
                                                // RowLoopvar = RowLoopvar + 1;
                                                ColLoopvar = 24;
                                                break;


                                            default:
                                                Recordvalue = dt.Rows[RowLoopvar][ColLoopvar].ToString();
                                                if (String.IsNullOrEmpty(Recordvalue))
                                                {
                                                    RecordsList.Add("0");
                                                }
                                                else
                                                {
                                                    RecordsList.Add(Recordvalue.Replace(",", ""));
                                                }
                                                //check RecordsList conatins one row/record then save it to database 
                                                if (ColLoopvar == 24)
                                                {
                                                    string testdummy = RecordsList.ElementAt(0);
                                                    //Database saved Values Indexes 0,1,2,3,4,5,6,9,10,11,12,13,15,16,17,18,18,19,20,21,22,23
                                                    tataDBContext.Database.ExecuteSqlCommand("INSERT INTO TML_CONSUMPTION_DATA "
                                                    + "(READING_DATE, SLOT_NUMBER, KWH_UNITS, KVAH_UNITS, RKVAH_UNITS, KVA, KW, MF, PRIMARY_MF_KWH, PRIMARY_MF_KVAH, "
                                                    + "PRIMARY_MF_RKVAH, PRIMARY_MF_KVA, PRIMARY_MF_KW, SECONDARY_MF_KWH, SECONDARY_MF_KVAH, SECONDARY_MF_RKVAH, "
                                                    + "SECONDARY_MF_KVA, SECONDARY_MF_KW, FINAL_KWH, FINAL_KVAH, FINAL_RKVAH, FINAL_KVA, FINAL_KW, CON_UNO) VALUES ('"
                                                    + ConsumeDate + "'" + Comma + RecordsList[0] + Comma + RecordsList[1] + Comma + RecordsList[2] + Comma + RecordsList[3]
                                                    + Comma + RecordsList[4] + Comma + RecordsList[5] + Comma + RecordsList[6] + Comma + RecordsList[9] + Comma
                                                    + RecordsList[10] + Comma + RecordsList[11] + Comma + RecordsList[12] + Comma + RecordsList[13] + Comma
                                                    + RecordsList[15] + Comma + RecordsList[16] + Comma + RecordsList[17] + Comma + RecordsList[18] + Comma
                                                    + RecordsList[18] + Comma + RecordsList[19] + Comma + RecordsList[20] + Comma + RecordsList[21] + Comma
                                                    + RecordsList[22] + Comma + RecordsList[23] + Comma + CON_UNO
                                                    + ")");
                                                    RecordsList.Clear();    //clear List after single record saved. 
                                                }

                                                break;
                                        }
                                    }
                                }
                                else if (value.Contains("GENERATION SIDE"))
                                {
                                    break;
                                }
                                else if (value == "Consumer Number")
                                {
                                    ConsumerNumber = Int64.Parse(dt.Rows[RowLoopvar][ColLoopvar + 2].ToString());

                                }
                                else if (value == "Serial Number")
                                {
                                    SerialNumber = dt.Rows[RowLoopvar][ColLoopvar + 4].ToString();
                                    CON_UNO = tataDBContext.checkConsumptionSerialNo(ConsumerNumber, SerialNumber);
                                }
                                else if (value.Contains("Reading Date"))
                                {
                                    string tempDate = dt.Rows[RowLoopvar][ColLoopvar].ToString();
                                    ConsumeDate = tempDate.Substring(14, 9);
                                    RowLoopvar = RowLoopvar + 3;
                                    strtRecordsave = true; //set TRUE to indicate records (each Time Slot) are started
                                    break;
                                }
                            } //end of ColLoop                            
                        }   //end of RowLoop   
                        ProgressUpdate(100);
                        #endregion
                    }
                    catch (Exception ConsumptionProcessExp)
                    {
                        processList.Add(GetFormattedText(LogType.Error, "Unable to process Consumption File due to DB Exp:  " + ConsumptionProcessExp.ToString()));
                        errorCode = ErrorCodes.UnableToProcessConsumptionExcelFile;
                        ProcessStateChanged(FileProcessingState.Failed, "Oops! Unable to Process. Please try again later");
                        RollbackTransaction();
                    }
                }
            }
            catch (Exception saveConsumpExp)
            {
                processList.Add(GetFormattedText(LogType.Error, "Error raised @ Converting Consumption file due to " + saveConsumpExp.ToString()));
                errorCode = ErrorCodes.UnableToProcessConsumptionExcelFile;
                ProcessStateChanged(FileProcessingState.Failed, "Oops! Unable to Process. Please try again later");
                RollbackTransaction();
            }
        }

        private void SaveConsumptionSummary(List<string> SummaryInfo, string ReadingDate, ConsumptionRowType currRowType, long CON_UNO)
        {
            tataDBContext.Database.ExecuteSqlCommand(
                "INSERT INTO TML_CONSUMPTION_SUMMARY "
                + "(READING_DATE, READING_TYPE, KWH_UNITS, KVAH_UNITS, RKVAH_UNITS, PRIMARY_MF_KWH, PRIMARY_MF_KVAH, PRIMARY_MF_RKVAH, "
                + "SECONDARY_MF_KWH, SECONDARY_MF_KVAH, SECONDARY_MF_RKVAH, FINAL_KWH, FINAL_KVAH, FINAL_RKVAH, CON_UNO) VALUES ('"
                + ReadingDate + "'" + Comma + "'" + GetConsumptionRowDesc(currRowType) + "'" + Comma + SummaryInfo[0] + Comma + SummaryInfo[1] 
                + Comma + SummaryInfo[2]
                + Comma + SummaryInfo[8] + Comma + SummaryInfo[9] + Comma + SummaryInfo[10] + Comma + SummaryInfo[14] + Comma + SummaryInfo[15]
                + Comma + SummaryInfo[16] + Comma + SummaryInfo[18] + Comma + SummaryInfo[19] + Comma + SummaryInfo[20] + Comma + CON_UNO
                + ")");

        }

        private string GetConsumptionRowDesc(ConsumptionRowType currRowType)
        {
            switch (currRowType)
            {
                case ConsumptionRowType.None: return "";
                case ConsumptionRowType.DateTotal: return "DT";
                case ConsumptionRowType.MTRTotal: return "MTR";
                case ConsumptionRowType.ConsTotal: return "CONS";
                case ConsumptionRowType.MaxMFKVA: return "MAXMFKVA";
                case ConsumptionRowType.Other: return "";
                default: return "";
            }
        }
        #endregion

        #region Generation File Processing
        //Read and Import Generation Excel sheet data into Database
        private void Import_To_GenerationDatabase(string FilePath)
        {
            try
            {
                long ConsumerNumber = 0;
                long GEN_UNO = 0; // DB Related Variables
                string SerialNumber = "";
                string GenerationDate = "";
                int RowLoopvar = 0;
                int ColLoopvar = 0;
                List<string> RecordsList = new List<string>();  //list to save reading records temporarly
                bool strtRecordsave = false;  //used to know recrods start in reading file             

                processList.Add(GetFormattedText(LogType.Info, "Generation Excel File Loading Initiated"));
                DataTable dt = LoadExcelFile(FilePath, FileTypes.GenerationFile);
                processList.Add(GetFormattedText(LogType.Info, "Generation Excel File Loading Completed"));
                if (dt != null)
                {
                    try
                    {
                        #region ProcessingGenertFile
                        //Read Data table Data added by Eswar 16 AUg 2016
                        GenerationRowType currRowType = GenerationRowType.None;
                        string Recordvalue;
                        processList.Add(GetFormattedText(LogType.Info, "Generation " + dt.Rows.Count + " rows founded"));
                        Random randomNum = new Random(60);
                        int processIndicatorVal = randomNum.Next(60, 100);
                        for (RowLoopvar = 0; RowLoopvar < dt.Rows.Count; RowLoopvar++)
                        {
                            if ((RowLoopvar % processIndicatorVal) == 0)
                            {
                                processList.Add(GetFormattedText(LogType.Info, "Generation -  " + RowLoopvar + " out of " +
                                    dt.Rows.Count + " rows processed"));
                                ProgressUpdate(Math.Round(Decimal.Divide(RowLoopvar, dt.Rows.Count) * 100, 0));
                            }

                            RecordsList.Clear();
                            for (ColLoopvar = 1; ColLoopvar < dt.Columns.Count; ColLoopvar++)
                            {
                                string value = dt.Rows[RowLoopvar][ColLoopvar].ToString();
                                if (value == "Meter" || value == "Consumer")
                                {
                                    strtRecordsave = true;
                                }

                                //if loop detects records start then below if executes
                                if (strtRecordsave)
                                {
                                    if (dt.Rows[RowLoopvar][1].ToString() == "" || dt.Rows[RowLoopvar][1].ToString() == "Total:")
                                    {
                                        strtRecordsave = false;  //Set to FALSE to stop records saving

                                    }
                                    else
                                    {
                                        if (dt.Rows[RowLoopvar][ColLoopvar].ToString() == "Date")
                                        {
                                            currRowType = GenerationRowType.DateTotal;
                                        }
                                        else if (dt.Rows[RowLoopvar][ColLoopvar].ToString().Contains("Meter"))
                                        {
                                            currRowType = GenerationRowType.MTRTotal;
                                        }
                                        else if (dt.Rows[RowLoopvar][ColLoopvar].ToString() == "Consumer")
                                        {
                                            currRowType = GenerationRowType.ConsTotal;
                                        }
                                        else
                                        {
                                            currRowType = GenerationRowType.None;
                                        }

                                        switch (currRowType)
                                        {
                                            case GenerationRowType.DateTotal:
                                            case GenerationRowType.MTRTotal:
                                            case GenerationRowType.ConsTotal:
                                                int TotalRowLoop = RowLoopvar;
                                                //save Records in Datetotalrecrodlist
                                                for (int Totalloop = 2; Totalloop < 14; Totalloop++)
                                                {
                                                    Recordvalue = dt.Rows[TotalRowLoop][Totalloop].ToString();
                                                    if (String.IsNullOrEmpty(Recordvalue))
                                                    {
                                                        RecordsList.Add("0");
                                                    }
                                                    else
                                                    {
                                                        RecordsList.Add(Recordvalue.Replace(",", ""));
                                                    }
                                                    if (Totalloop == 13)
                                                    {
                                                        //string testdummy = RecordsList.ElementAt(0);
                                                        //Database saved Values Indexes 0,2,4,6,8,10,11
                                                        SaveGenerationSummary(RecordsList, GenerationDate, currRowType, GEN_UNO);
                                                        RecordsList.Clear();    //clear List after single record saved. 
                                                    }
                                                }
                                                RowLoopvar = RowLoopvar + 1;
                                                if (currRowType == GenerationRowType.ConsTotal)
                                                {
                                                    ColLoopvar = 13;
                                                }
                                                else
                                                {
                                                    ColLoopvar = 0;
                                                }

                                                break;

                                            default:
                                                Recordvalue = dt.Rows[RowLoopvar][ColLoopvar].ToString();
                                                if (String.IsNullOrEmpty(Recordvalue))
                                                {
                                                    RecordsList.Add("0");
                                                }
                                                else
                                                {
                                                    RecordsList.Add(Recordvalue.Replace(",", ""));
                                                }
                                                //check RecordsList conatins one row/record then save it to database 
                                                if (ColLoopvar == 13)
                                                {
                                                    //string testdummy = RecordsList.ElementAt(0);
                                                    //Database saved Values Indexes 0,1,2,3,4,5,6,7,8,9,10,11,12
                                                    tataDBContext.Database.ExecuteSqlCommand(
                                                          "INSERT INTO TML_" + SerialNumber + "_DATA "
                                                        + "(READING_DATE, SLOT_NO, KWH_UNITS, MF, MF_KWH_UNITS, PCT, PCT_KWH_UNITS, EPA_CAP, KWH_UNIT_AFT_EPA, "
                                                        + " DISTR_LOSS_PCT, AFT_DIST_LOSS, TRANS_LOSS_PCT, AFT_TRANS_LOSS, FINAL_KWH_UNITS) VALUES ('"
                                                        + GenerationDate + "'" + Comma + RecordsList[0] + Comma + RecordsList[1] + Comma + RecordsList[2]
                                                        + Comma + RecordsList[3] + Comma + RecordsList[4] + Comma + RecordsList[5] + Comma + RecordsList[6]
                                                        + Comma + RecordsList[7] + Comma + RecordsList[8] + Comma + RecordsList[9] + Comma + RecordsList[10]
                                                        + Comma + RecordsList[11] + Comma + RecordsList[12] + ")");

                                                    RecordsList.Clear();    //clear List after single record saved. 
                                                }
                                                break;
                                        }
                                    }
                                }
                                else if (value.Contains("CONSUMER SIDE"))
                                {
                                    break;
                                }
                                else if (value == "Consumer Number:")
                                {
                                    ConsumerNumber = Int64.Parse(dt.Rows[RowLoopvar][ColLoopvar + 2].ToString());
                                }
                                else if (value == "Serial Number:")
                                {
                                    SerialNumber = dt.Rows[RowLoopvar][ColLoopvar + 1].ToString();
                                    GEN_UNO = tataDBContext.createGenerationTable(ConsumerNumber, SerialNumber);
                                }
                                else if (value.Contains("Reading Date"))
                                {
                                    string tempDate = dt.Rows[RowLoopvar][ColLoopvar].ToString();
                                    GenerationDate = tempDate.Substring(14, 9);

                                    //Start grab records after Reading Date Detected
                                    RowLoopvar = RowLoopvar + 1;
                                    strtRecordsave = true; //set TRUE to indicate records are started
                                    break;
                                }
                            } //end ColLoop
                        }  //end RowLoop
                        ProgressUpdate(100);
                        #endregion
                    }
                    catch (Exception tataDBGenertContextExp)
                    {
                        processList.Add(GetFormattedText(LogType.Error, "Unable to process Generation File due to DB Exp:  " + tataDBGenertContextExp.ToString()));
                        errorCode = ErrorCodes.UnableToProcessGenerationExcelFile;
                        ProcessStateChanged(FileProcessingState.Failed, "Oops! Unable to Process. Please try again later");
                        RollbackTransaction();
                    }
                }
            }
            catch (Exception saveGenertExp)
            {
                processList.Add(GetFormattedText(LogType.Error, "Error raised @ Converting Generation file due to " + saveGenertExp.ToString()));
                errorCode = ErrorCodes.UnableToProcessGenerationExcelFile;
                ProcessStateChanged(FileProcessingState.Failed, "Oops! Unable to Process. Please try again later");
                RollbackTransaction();
            }
        }

        private void SaveGenerationSummary(List<string> SummaryInfo, string ReadingDate, GenerationRowType currRowType, long GEN_UNO)
        {
            if (currRowType == GenerationRowType.ConsTotal)
            {
                tataDBContext.Database.ExecuteSqlCommand(
                               "INSERT INTO TML_GENERATION_SUMMARY "
                               + "(READING_DATE, READING_TYPE, KWH_UNITS, MF_KWH_UNITS, PCT_KWH_UNITS, KWH_UNIT_AFT_EPA, AFT_DIST_LOSS, AFT_TRANS_LOSS, "
                               + "FINAL_KWH_UNITS) VALUES ('"
                               + ReadingDate + "'" + Comma + "'" + GetGenerationRowDesc(currRowType) + "'" + Comma + SummaryInfo[0] + Comma + SummaryInfo[2]
                               + Comma + SummaryInfo[4] + Comma + SummaryInfo[6] + Comma + SummaryInfo[8] + Comma + SummaryInfo[10] + Comma + SummaryInfo[11]
                               + ")");
            }
            else
            {
                tataDBContext.Database.ExecuteSqlCommand(
                "INSERT INTO TML_GENERATION_SUMMARY "
                + "(READING_DATE, READING_TYPE, KWH_UNITS, MF_KWH_UNITS, PCT_KWH_UNITS, KWH_UNIT_AFT_EPA, AFT_DIST_LOSS, AFT_TRANS_LOSS, "
                + "FINAL_KWH_UNITS, GEN_UNO) VALUES ('"
                + ReadingDate + "'" + Comma + "'" + GetGenerationRowDesc(currRowType) + "'" + Comma + SummaryInfo[0] + Comma + SummaryInfo[2]
                + Comma + SummaryInfo[4] + Comma + SummaryInfo[6] + Comma + SummaryInfo[8] + Comma + SummaryInfo[10] + Comma + SummaryInfo[11]
                + Comma + GEN_UNO
                + ")");
            }
        }

        private string GetGenerationRowDesc(GenerationRowType currRowType)
        {
            switch (currRowType)
            {
                case GenerationRowType.None: return "";
                case GenerationRowType.DateTotal: return "DT";
                case GenerationRowType.MTRTotal: return "MTR";
                case GenerationRowType.ConsTotal: return "CONS";
                case GenerationRowType.Other: return "";
                default: return "";
            }
        }

        #endregion

        #region TimeSlot Consumption File Processing

        private void Import_To_TSCDatabase(string FilePath)
        {
            string Readingdate = "";
            string ConsumerNumber = "";
            float curConMtrUnits = 0, MaxConMtrUnits = 0;
            float curGenMtrNCNSUnits = 0, MaxGenMtrNCNSUnits = 0;
            float MaxGenMtrUnits = 0;
            double HighestRecordedDemand = 0;
            double MaximumOAGeneration = 0;
            double BillingDemand = 0;
            int RowLoopvar = 0;
            int ColLoopvar = 0;
            string Max_Reading_Date_Rec = "";
            bool strtRecordsave = false;  //used to know recrods start in reading file
            int SaveReadDateTrigger = 1;  //trigger to save reading date or not '1' for save '0' for notsave
            processList.Add(GetFormattedText(LogType.Info, "Time Slot Consumption Excel File Loading Initiated"));
            DataTable dt = LoadExcelFile(FilePath, FileTypes.TimeSlotConsumptionFile);
            processList.Add(GetFormattedText(LogType.Info, "Time Slot Consumption Excel File Loading Completed"));
            if (dt != null)
            {
                try
                {
                    #region ProcessingTimeSlotConsFile
                    Random randomNum = new Random(60);
                    int processIndicatorVal = randomNum.Next(60, 100);
                    processList.Add(GetFormattedText(LogType.Info, "Timeslot Consumption" + dt.Rows.Count + " rows founded"));
                    for (RowLoopvar = 0; RowLoopvar < dt.Rows.Count; RowLoopvar++)
                    {
                        if ((RowLoopvar % processIndicatorVal) == 0)
                        {
                            processList.Add(GetFormattedText(LogType.Info, "Timeslot Consumption -  " + RowLoopvar + " out of " +
                                dt.Rows.Count + " rows processed"));
                            ProgressUpdate(Math.Round(Decimal.Divide(RowLoopvar, dt.Rows.Count) * 100, 0));
                        }

                        for (ColLoopvar = 1; ColLoopvar < dt.Columns.Count; ColLoopvar++)
                        {
                            string value = dt.Rows[RowLoopvar][ColLoopvar].ToString();
                            //if loop detects records start then below if executes
                            if (strtRecordsave)
                            {
                                if (value == "REF")
                                {
                                    RowLoopvar = RowLoopvar + 3;
                                    strtRecordsave = false;
                                    break;
                                }
                                else if (value == "TOTAL")
                                {
                                    strtRecordsave = false;
                                    SaveReadDateTrigger = 0;
                                    break;
                                }
                                else
                                {
                                    //logic to find & Save max of Consumer unit and its relavant Gen Mtr unit(NCNS)
                                    curConMtrUnits = Convert.ToSingle(dt.Rows[RowLoopvar][2]);
                                    if (curConMtrUnits > MaxConMtrUnits)
                                    {
                                        MaxConMtrUnits = curConMtrUnits;
                                        MaxGenMtrUnits = Convert.ToSingle(dt.Rows[RowLoopvar][4]);
                                        Max_Reading_Date_Rec = Readingdate;
                                    }

                                    curGenMtrNCNSUnits = Convert.ToSingle(dt.Rows[RowLoopvar][4]);
                                    if (curGenMtrNCNSUnits > MaxGenMtrNCNSUnits)
                                    {
                                        MaxGenMtrNCNSUnits = curGenMtrNCNSUnits;
                                    }
                                    break;
                                }
                            }
                            else if (value.Contains("Reading Date") && SaveReadDateTrigger == 1)
                            {
                                string tempDate = dt.Rows[RowLoopvar][ColLoopvar].ToString();
                                Readingdate = tempDate.Substring(15, 9);
                                break;
                            }
                            else if (value.Contains("Consumer Number") && SaveReadDateTrigger == 1)
                            {
                                ConsumerNumber = dt.Rows[RowLoopvar][ColLoopvar + 3].ToString();

                            }
                            else if (value == "Reading")
                            {
                                RowLoopvar = RowLoopvar + 3;
                                strtRecordsave = true;
                            }
                            else if(value == "Final Billed Demand")                            
                            {
                                // Final Billing Demand value is available in various columns. 
                                string tempBillingDemand = "";
                                ColLoopvar++;
                                do 
                                {
                                    if ( (dt.Rows[RowLoopvar][ColLoopvar].ToString().Length != 0) && (!dt.Rows[RowLoopvar][ColLoopvar].ToString().Equals(":")) )
                                    {
                                        tempBillingDemand = dt.Rows[RowLoopvar][ColLoopvar].ToString();                                       
                                        break;
                                    }
                                    ColLoopvar++;
                                } while (ColLoopvar < dt.Columns.Count);
                                
                                if (tempBillingDemand.Length == 0)
                                {
                                    throw new ArgumentException("Final Billed Demand is not found", "Final Billed Demand");
                                }
                                else
                                {
                                    BillingDemand = Convert.ToSingle(tempBillingDemand);
                                }
                            }
                        }
                    }
                    //HighestRecordedDemand = MaxConMtrUnits * 4;  // Commneted by JVK & Eswar on 12-Dec-16 becoz this value taken from Consumption File
                    MaximumOAGeneration = MaxGenMtrUnits * 4;
                    //  BillingDemand = MaxConMtrUnits - MaxGenMtrNCNSUnits;   // Commented by JVK & Eswar on 12-Dec-16 becoz this value taken in same file but value taken at last page Final Billed Demand
                    tataDBContext.Database.ExecuteSqlCommand(
                                    "INSERT INTO TML_TS_CONSUMPTION_SUMMARY "
                                    + "(MAX_READING_DATE, HIGHEST_RECORDED_DEMAND, MAXIMUM_OA_GENERATION, BILLING_DEMAND ) VALUES ('"
                                    + Max_Reading_Date_Rec + "'" + Comma + HIGHEST_RECORDED_DEMAND + Comma + MaximumOAGeneration
                                    + Comma + BillingDemand + ")");

                    ProgressUpdate(100);

                    #endregion
                }
                catch(ArgumentException FinalBilledDemndNotFoundExp)
                {                    
                    processList.Add(GetFormattedText(LogType.Error, "Unable to find Fnal Billed Demand in Time Slot Consumption File"));
                    errorCode = ErrorCodes.UnableToProcessTimeSlotConsExcelFile;
                    ProcessStateChanged(FileProcessingState.Failed, "Oops! Unable to find Fnal Billed Demand in Time Slot Consumption File. Please try again later");
                    RollbackTransaction();
                }
                catch (Exception tataDBTSCContextExp)
                {
                    
                    processList.Add(GetFormattedText(LogType.Error, "Unable to process Time Slot Consumption File due to Exp:  " + tataDBTSCContextExp.ToString()));
                    errorCode = ErrorCodes.UnableToProcessTimeSlotConsExcelFile;
                    ProcessStateChanged(FileProcessingState.Failed, "Oops! Unable to Process. Please try again later");
                    RollbackTransaction();
                }
            }
        }

        #endregion TimeSlot Consumption FileProcessing


        #region Meter Consumption File Processing
        //=================MTR_CONS File Processing====================================================
        //Read and Import MTR_CONS Excel sheet data into Database
        private void Import_To_MTR_CONSDatabase(string FilePath)
        {

            string Readingdate = "";
            string serialNumber = "";
            string OAunitsAdjstd = "";
            string finalUnits = "";
            string OAunitsAgnstAdjstd = "";
            int RowLoopvar = 0;
            int ColLoopvar = 0;
            List<string> OAunitsAdjstdList = new List<string>();  //list to save reading records temporarly
            List<string> finalUnitsList = new List<string>();  //list to save reading records temporarly
            List<string> OAunitsAgnstAdjstdList = new List<string>();  //list to save reading records temporarly

            bool strtRecordsave = false;  //used to know recrods start in reading file        

            processList.Add(GetFormattedText(LogType.Info, "Meter Consumption Excel File Loading Initiated"));
            DataTable dt = LoadExcelFile(FilePath, FileTypes.MeterConsumptionFile);
            processList.Add(GetFormattedText(LogType.Info, "Meter Consumption Excel File Loading Completed"));

            if (dt != null)
            {
                try
                {
                    for (RowLoopvar = 0; RowLoopvar < dt.Rows.Count; RowLoopvar++)
                    {
                        processList.Add(GetFormattedText(LogType.Info, "Meter Consumption -  " + RowLoopvar + " out of " +
                                    dt.Rows.Count + " rows processed"));
                        ProgressUpdate(Math.Round(Decimal.Divide(RowLoopvar, dt.Rows.Count) * 100, 0));

                        for (ColLoopvar = 1; ColLoopvar < dt.Columns.Count; ColLoopvar++)
                        {
                            string value = dt.Rows[RowLoopvar][ColLoopvar].ToString();
                            //if loop detects records start then below if executes
                            if (strtRecordsave)
                            {
                                if (value == "Units for OA Adjustment(KWH) :")
                                {
                                    OAunitsAdjstd = dt.Rows[RowLoopvar][ColLoopvar + 1].ToString();
                                    OAunitsAdjstdList.Add(OAunitsAdjstd);
                                }
                                else if (value == "Final Units(KWH) :")
                                {
                                    finalUnits = dt.Rows[RowLoopvar][ColLoopvar + 1].ToString();
                                    finalUnitsList.Add(finalUnits);
                                }
                                else if (value == "Units Adjusted against OA(KWH) :")
                                {
                                    OAunitsAgnstAdjstd = dt.Rows[RowLoopvar][ColLoopvar + 1].ToString();
                                    OAunitsAgnstAdjstdList.Add(OAunitsAgnstAdjstd);
                                    strtRecordsave = false;
                                    TML_GENERATION_UNITS genUnitsRec =
                                            tataDBContext.TML_GENERATION_UNITS.Where(genInfo => genInfo.SERIAL_NO.Trim() == serialNumber.Trim()).FirstOrDefault();
                                    if (genUnitsRec == null)
                                    {
                                        processList.Add(GetFormattedText(LogType.Error, "Unable to find Serial number " + serialNumber + " in DB"));
                                        errorCode = ErrorCodes.UnableToProcessMeterConsExcelFile;
                                        ProcessStateChanged(FileProcessingState.Failed, "Oops! Unable to Process. Please try again later");
                                        RollbackTransaction();
                                        return;
                                    }
                                    else
                                    {
                                        tataDBContext.Database.ExecuteSqlCommand(
                                              "INSERT INTO TML_METER_CONSUM_MONTH_SUMMARY " +
                                              "(FINAL_UNIT_KWH, UNITS_ADJST_AGNST_OA_KWH, UNITS_ADJST_OA_KWH, READING_DATE, GEN_UNO) "
                                              + " VALUES (" + finalUnits + Comma + OAunitsAgnstAdjstd + Comma + OAunitsAdjstd + Comma
                                              + "'01-" + consumptionMonthFileDate + "'" + Comma + genUnitsRec.GEN_UNO + ")");
                                        break;
                                    }
                                }

                            }
                            else if (value.Contains("OPEN ACCESS METER CONSUMPTION REPORT FOR THE MONTH"))
                            {
                                string tempDate = dt.Rows[RowLoopvar][ColLoopvar + 2].ToString();
                                //Readingdate = tempDate.Substring(15, 9);
                                Readingdate = tempDate;
                                break;
                            }
                            else if (value.Contains("Serial Number :"))
                            {
                                serialNumber = dt.Rows[RowLoopvar][ColLoopvar + 1].ToString();
                                strtRecordsave = true;
                                RowLoopvar = RowLoopvar + 8;
                                break;
                            }
                        }
                    }
                    ProgressUpdate(100);

                }
                catch (Exception meterConsmExcelFileProcessExp)
                {
                    processList.Add(GetFormattedText(LogType.Error, "Unable to process Meter Consumption File due to DB Exp:  " +
                        meterConsmExcelFileProcessExp.ToString()));
                    errorCode = ErrorCodes.UnableToProcessMeterConsExcelFile;
                    ProcessStateChanged(FileProcessingState.Failed, "Oops! Unable to Process. Please try again later");
                    RollbackTransaction();
                }
            }
        }
        #endregion

        #region Open Access Bill File Processing
        //=================OPEN_ACCESS File Processing====================================================

        private void Import_To_Open_Access_BillDatabase(string FilePath)

        {
            int RowLoopvar = 0;
            int ColLoopvar = 0;
            openAccessBillFiledsOAList = new Dictionary<string, string>(openAccessBillFiledsMSEDCLList) { };
            openAccessBillFiledsMSEDCLList.Add(Constants.cMSEDCLTotalBill, "0");
            openAccessBillFiledsOAList.Add(Constants.cOpenAccessTotalBill, "0");
            Dictionary<string, string> tempDictionary = null;

            processList.Add(GetFormattedText(LogType.Info, "Open Access Bill Excel File Loading Initiated"));
            DataTable dt = LoadExcelFile(FilePath, FileTypes.OpenAccessBillFile);
            processList.Add(GetFormattedText(LogType.Info, "Open Access Bill Excel File Loading Completed"));

            #region Last Month Banked Process

            for (RowLoopvar = 0; RowLoopvar < dt.Rows.Count; RowLoopvar++)
            {
                processList.Add(GetFormattedText(LogType.Info, "Open Access Bill  -  " + RowLoopvar + " out of " +
                                    dt.Rows.Count + " rows processed"));
                ProgressUpdate(Math.Round(Decimal.Divide(RowLoopvar, dt.Rows.Count) * 100, 0));

                for (ColLoopvar = 1; ColLoopvar < dt.Columns.Count; ColLoopvar++)
                {
                    string value = dt.Rows[RowLoopvar][ColLoopvar].ToString();

                    if (value == "Summary Of Energy Drawal")
                    {
                        //to know the file is lastbanked or not
                        if (!IsLastMonthBankedFile)
                        {
                            RowLoopvar = RowLoopvar + 5;
                            ColLoopvar = 1;
                            openAccessSummaryData[Constants.cTotalDrawalUnits] = dt.Rows[RowLoopvar][ColLoopvar + 2].ToString();
                            openAccessSummaryData[Constants.cTotalInjectionUnits] = dt.Rows[RowLoopvar][ColLoopvar + 4].ToString();
                            openAccessSummaryData[Constants.cMSEDCLTariff] = dt.Rows[RowLoopvar][ColLoopvar + 6].ToString();
                            openAccessSummaryData[Constants.cTempTariff] = dt.Rows[RowLoopvar][ColLoopvar + 7].ToString();
                            openAccessSummaryData[Constants.cOffsetAgainstDrawal] = dt.Rows[RowLoopvar][ColLoopvar + 8].ToString();
                            openAccessSummaryData[Constants.cOverInjectedUnits] = dt.Rows[RowLoopvar][ColLoopvar + 9].ToString();
                            break;
                        }
                        else
                        {
                            RowLoopvar = RowLoopvar + 7;
                            ColLoopvar = 1;
                            openAccessSummaryData[Constants.cTotalDrawalUnits] = dt.Rows[RowLoopvar][ColLoopvar + 1].ToString();
                            openAccessSummaryData[Constants.cTotalInjectionUnits] = dt.Rows[RowLoopvar][ColLoopvar + 3].ToString();
                            openAccessSummaryData[Constants.cMSEDCLTariff] = dt.Rows[RowLoopvar][ColLoopvar + 5].ToString();
                            openAccessSummaryData[Constants.cTempTariff] = dt.Rows[RowLoopvar][ColLoopvar + 6].ToString();
                            openAccessSummaryData[Constants.cCurrentMonthGeneration] = dt.Rows[RowLoopvar][ColLoopvar + 7].ToString();
                            openAccessSummaryData[Constants.cLastMonthBanked] = dt.Rows[RowLoopvar][ColLoopvar + 8].ToString();
                            openAccessSummaryData[Constants.cOverInjectedUnits] = dt.Rows[RowLoopvar][ColLoopvar + 9].ToString();
                            break;
                        }
                    }
                    else if (value == "Billed Demand")
                    {
                        openAccessSummaryData[Constants.cBilledDemand] = dt.Rows[RowLoopvar][ColLoopvar + 1].ToString();
                        break;
                    }
                    else if (value == "Highest Recorded Demand")
                    {
                        openAccessSummaryData[Constants.cHighestRecordedDemand] = dt.Rows[RowLoopvar][ColLoopvar + 1].ToString();
                        openAccessSummaryData[Constants.cRKVAH] = dt.Rows[RowLoopvar][ColLoopvar + 2].ToString();
                        openAccessSummaryData[Constants.cPF] = dt.Rows[RowLoopvar][ColLoopvar + 3].ToString();
                        openAccessSummaryData[Constants.cLF] = dt.Rows[RowLoopvar][ColLoopvar + 4].ToString();
                        openAccessSummaryData[Constants.cAZone] = dt.Rows[RowLoopvar][ColLoopvar + 6].ToString();
                        openAccessSummaryData[Constants.cBZone] = dt.Rows[RowLoopvar][ColLoopvar + 7].ToString();
                        openAccessSummaryData[Constants.cCZone] = dt.Rows[RowLoopvar][ColLoopvar + 8].ToString();
                        openAccessSummaryData[Constants.cDZone] = dt.Rows[RowLoopvar][ColLoopvar + 9].ToString();
                        break;
                    }
                    else if (value == "Bill for MSEDCL consumption" || value == "Bill for Open Access")
                    {

                        if (value == "Bill for MSEDCL consumption")
                        {
                            tempDictionary = openAccessBillFiledsMSEDCLList;
                        }
                        else
                        {
                            tempDictionary = openAccessBillFiledsOAList;
                        }

                        int billValuesLoop = 0;
                        RowLoopvar = RowLoopvar + 2; //go to strating value row of bill table
                        String negavtiveSymbol = "";
                        //loop to iterate row wise for same column to get bill values
                        for (billValuesLoop = 0; billValuesLoop < 17; billValuesLoop++)
                        {
                            value = dt.Rows[RowLoopvar + billValuesLoop][ColLoopvar].ToString();
                            //dataList.Add(dt.Rows[RowLoopvar + billValuesLoop][ColLoopvar + 9].ToString()); //Save read vaule in bill data
                            negavtiveSymbol = "";

                            if (dt.Rows[RowLoopvar + billValuesLoop][ColLoopvar + 8].ToString().Equals("-"))
                            {
                                negavtiveSymbol = "-";
                            }
                            else
                            {
                                negavtiveSymbol = "";
                            }

                            if (value == Constants.cDemandCharge)
                            {
                                tempDictionary[Constants.cDemandCharge] = negavtiveSymbol + dt.Rows[RowLoopvar + billValuesLoop][ColLoopvar + 9].ToString();
                            }
                            else if (value == Constants.cEnergyCharges)
                            {
                                tempDictionary[Constants.cEnergyCharges] = negavtiveSymbol + dt.Rows[RowLoopvar + billValuesLoop][ColLoopvar + 9].ToString();
                            }
                            else if (value == Constants.cTODTariffEC)
                            {
                                tempDictionary[Constants.cTODTariffEC] = negavtiveSymbol + dt.Rows[RowLoopvar + billValuesLoop][ColLoopvar + 9].ToString();
                            }
                            else if (value.Contains(Constants.cFACCharges))
                            {
                                tempDictionary[Constants.cFACCharges] = negavtiveSymbol + dt.Rows[RowLoopvar + billValuesLoop][ColLoopvar + 9].ToString();
                            }
                            else if (value.Contains("Addl Charges"))
                            {
                                var checkAddlVal = dt.Rows[RowLoopvar + billValuesLoop][ColLoopvar + 3].ToString();
                                if (checkAddlVal.Contains(Constants.cEHVRebate))
                                {
                                    tempDictionary[Constants.cEHVRebate] = negavtiveSymbol + dt.Rows[RowLoopvar + billValuesLoop][ColLoopvar + 9].ToString();
                                }
                                else if (checkAddlVal.Contains(Constants.cInfraCharge))
                                {
                                    tempDictionary[Constants.cInfraCharge] = negavtiveSymbol + dt.Rows[RowLoopvar + billValuesLoop][ColLoopvar + 9].ToString();
                                }
                            }
                            else if (value == Constants.cElectricityDuty)
                            {
                                tempDictionary[Constants.cElectricityDuty] = negavtiveSymbol + dt.Rows[RowLoopvar + billValuesLoop][ColLoopvar + 9].ToString();
                            }
                            else if (value == Constants.cTaxOnSale)
                            {
                                tempDictionary[Constants.cTaxOnSale] = negavtiveSymbol + dt.Rows[RowLoopvar + billValuesLoop][ColLoopvar + 9].ToString();
                            }
                            else if (value == Constants.cPFPenaltyIncentive)
                            {
                                tempDictionary[Constants.cPFPenaltyIncentive] = negavtiveSymbol + dt.Rows[RowLoopvar + billValuesLoop][ColLoopvar + 9].ToString();
                            }
                            else if (value == Constants.cDemandPenalty)
                            {
                                tempDictionary[Constants.cDemandPenalty] = negavtiveSymbol + dt.Rows[RowLoopvar + billValuesLoop][ColLoopvar + 9].ToString();
                            }
                            else if (value == Constants.cRLCRefund)
                            {
                                tempDictionary[Constants.cRLCRefund] = negavtiveSymbol + dt.Rows[RowLoopvar + billValuesLoop][ColLoopvar + 9].ToString();
                            }
                            else if (value == Constants.cCrossSubsidySurcharge)
                            {
                                tempDictionary[Constants.cCrossSubsidySurcharge] = negavtiveSymbol + dt.Rows[RowLoopvar + billValuesLoop][ColLoopvar + 9].ToString();
                            }
                            else if (value == Constants.cWheelingcharges)
                            {
                                tempDictionary[Constants.cWheelingcharges] = negavtiveSymbol + dt.Rows[RowLoopvar + billValuesLoop][ColLoopvar + 9].ToString();
                            }
                            else if (value == Constants.cTransmissionCharges)
                            {
                                tempDictionary[Constants.cTransmissionCharges] = negavtiveSymbol + dt.Rows[RowLoopvar + billValuesLoop][ColLoopvar + 9].ToString();
                            }
                            else if (value == Constants.cOperatingCharges)
                            {
                                tempDictionary[Constants.cOperatingCharges] = negavtiveSymbol + dt.Rows[RowLoopvar + billValuesLoop][ColLoopvar + 9].ToString();
                            }
                            else if (value == Constants.cDebitAdjustments)
                            {
                                tempDictionary[Constants.cDebitAdjustments] = negavtiveSymbol + dt.Rows[RowLoopvar + billValuesLoop][ColLoopvar + 9].ToString();
                            }
                            else if (value == Constants.cMSEDCLTotalBill)
                            {
                                tempDictionary[Constants.cMSEDCLTotalBill] = negavtiveSymbol + dt.Rows[RowLoopvar + billValuesLoop][ColLoopvar + 9].ToString();
                            }
                            else if (value == Constants.cOpenAccessTotalBill)
                            {
                                tempDictionary[Constants.cOpenAccessTotalBill] = negavtiveSymbol + dt.Rows[RowLoopvar + billValuesLoop][ColLoopvar + 9].ToString();
                            }
                        }
                        RowLoopvar = RowLoopvar + billValuesLoop - 1;
                        break;
                    }
                }
            }

            var AccessSummaryKeys = new List<string>(openAccessSummaryData.Keys);
            foreach (string key in AccessSummaryKeys)
            {
                openAccessSummaryData[key] = openAccessSummaryData[key].Replace(",", "").Trim();
                if (openAccessSummaryData[key].Length == 0)
                {
                    openAccessSummaryData[key] = "0";
                }
            }


            var MSEDCLKeys = new List<string>(openAccessBillFiledsMSEDCLList.Keys);
            foreach (string key in MSEDCLKeys)
            {
                openAccessBillFiledsMSEDCLList[key] = openAccessBillFiledsMSEDCLList[key].Replace(",", "").Trim();
                if (openAccessBillFiledsMSEDCLList[key].Length == 0)
                {
                    openAccessBillFiledsMSEDCLList[key] = "0";
                }
            }

            var OpenAccessKeys = new List<string>(openAccessBillFiledsOAList.Keys);
            foreach (string key in OpenAccessKeys)
            {
                openAccessBillFiledsOAList[key] = openAccessBillFiledsOAList[key].Replace(",", "").Trim();
                if (openAccessBillFiledsOAList[key].Length == 0)
                {
                    openAccessBillFiledsOAList[key] = "0";
                }
            }


            tataDBContext.Database.ExecuteSqlCommand(
                "INSERT INTO TML_OPEN_ACCESS_BILL_SUMMARY (READING_DATE, TOTAL_DRAWAL_UNITS, TOTAL_INJECTION_UNITS, MSEDCL_TARIFF,"
                + "TEMP_TARIFF, CURR_MONTH_GEN, LAST_MONTH_BANKED, OFFSET_AGNST_DRAWAL, OVER_INJECTED_UNITS, BILLED_DEMAND, RKVAH, PF,"
                + "LF, AZONE, BZONE, CZONE, DZONE, HIGHEST_RECORDED_DEMAND) VALUES ('"
                + "01-" + consumptionMonthFileDate + "'" + Comma + openAccessSummaryData[Constants.cTotalDrawalUnits] + Comma
                + openAccessSummaryData[Constants.cTotalInjectionUnits] + Comma
                + openAccessSummaryData[Constants.cMSEDCLTariff] + Comma + openAccessSummaryData[Constants.cTempTariff] + Comma
                + openAccessSummaryData[Constants.cCurrentMonthGeneration] + Comma + openAccessSummaryData[Constants.cLastMonthBanked] + Comma
                + openAccessSummaryData[Constants.cOffsetAgainstDrawal] + Comma
                + openAccessSummaryData[Constants.cOverInjectedUnits] + Comma + openAccessSummaryData[Constants.cBilledDemand] + Comma
                + openAccessSummaryData[Constants.cRKVAH] + Comma + openAccessSummaryData[Constants.cPF] + Comma
                + openAccessSummaryData[Constants.cLF] + Comma + openAccessSummaryData[Constants.cAZone] + Comma
                + openAccessSummaryData[Constants.cBZone] + Comma + openAccessSummaryData[Constants.cCZone] + Comma
                + openAccessSummaryData[Constants.cDZone] + Comma + openAccessSummaryData[Constants.cHighestRecordedDemand] + ")");

            tataDBContext.Database.ExecuteSqlCommand(
                "INSERT INTO TML_OPEN_ACCESS_MSEDCL_BILL (READING_DATE, DEMAND_CHARGES, ENERGY_CHARGES, TOD_TARIFF_EC,"
                + " FAC_CHARGERS, ADDL_CHARGES_EHV, ADDL_CHARGES_INFRA, ELECTRICITY_DUTY, TAX_ON_SALE, PF_PENALTY, "
                + "DEMAND_PENALTY, RLC_REFUND, CROSS_SUBSIDY_SURCHG, WHEELING_CHARGERS, TRANSMISSION_CHARGES, OPERATING_CHARGES, "
                + "DEBIT_ADJUST, TOTAL_BILL) VALUES ('"
                + "01-" + consumptionMonthFileDate + "'" + Comma + openAccessBillFiledsMSEDCLList[Constants.cDemandCharge] + Comma
                + openAccessBillFiledsMSEDCLList[Constants.cEnergyCharges] + Comma + openAccessBillFiledsMSEDCLList[Constants.cTODTariffEC] + Comma
                + openAccessBillFiledsMSEDCLList[Constants.cFACCharges] + Comma + openAccessBillFiledsMSEDCLList[Constants.cEHVRebate] + Comma
                + openAccessBillFiledsMSEDCLList[Constants.cInfraCharge] + Comma + openAccessBillFiledsMSEDCLList[Constants.cElectricityDuty] + Comma
                + openAccessBillFiledsMSEDCLList[Constants.cTaxOnSale] + Comma + openAccessBillFiledsMSEDCLList[Constants.cPFPenaltyIncentive] + Comma
                + openAccessBillFiledsMSEDCLList[Constants.cDemandPenalty] + Comma + openAccessBillFiledsMSEDCLList[Constants.cRLCRefund] + Comma
                + openAccessBillFiledsMSEDCLList[Constants.cCrossSubsidySurcharge] + Comma + openAccessBillFiledsMSEDCLList[Constants.cWheelingcharges] + Comma
                + openAccessBillFiledsMSEDCLList[Constants.cTransmissionCharges] + Comma + openAccessBillFiledsMSEDCLList[Constants.cOperatingCharges] + Comma
                + openAccessBillFiledsMSEDCLList[Constants.cDebitAdjustments] + Comma + openAccessBillFiledsMSEDCLList[Constants.cMSEDCLTotalBill] + ")");

            tataDBContext.Database.ExecuteSqlCommand(
                "INSERT INTO TML_OPEN_ACCESS_BILL (READING_DATE, DEMAND_CHARGES, ENERGY_CHARGES, TOD_TARIFF_EC,"
                + " FAC_CHARGERS, ADDL_CHARGES_EHV, ADDL_CHARGES_INFRA, ELECTRICITY_DUTY, TAX_ON_SALE, PF_PENALTY, "
                + "DEMAND_PENALTY, RLC_REFUND, CROSS_SUBSIDY_SURCHG, WHEELING_CHARGERS, TRANSMISSION_CHARGES, OPERATING_CHARGES, "
                + "DEBIT_ADJUST, TOTAL_BILL) VALUES ('"
                + "01-" + consumptionMonthFileDate + "'" + Comma + openAccessBillFiledsOAList[Constants.cDemandCharge] + Comma
                + openAccessBillFiledsOAList[Constants.cEnergyCharges] + Comma + openAccessBillFiledsOAList[Constants.cTODTariffEC] + Comma
                + openAccessBillFiledsOAList[Constants.cFACCharges] + Comma + openAccessBillFiledsOAList[Constants.cEHVRebate] + Comma
                + openAccessBillFiledsOAList[Constants.cInfraCharge] + Comma + openAccessBillFiledsOAList[Constants.cElectricityDuty] + Comma
                + openAccessBillFiledsOAList[Constants.cTaxOnSale] + Comma + openAccessBillFiledsOAList[Constants.cPFPenaltyIncentive] + Comma
                + openAccessBillFiledsOAList[Constants.cDemandPenalty] + Comma + openAccessBillFiledsOAList[Constants.cRLCRefund] + Comma
                + openAccessBillFiledsOAList[Constants.cCrossSubsidySurcharge] + Comma + openAccessBillFiledsOAList[Constants.cWheelingcharges] + Comma
                + openAccessBillFiledsOAList[Constants.cTransmissionCharges] + Comma + openAccessBillFiledsOAList[Constants.cOperatingCharges] + Comma
                + openAccessBillFiledsOAList[Constants.cDebitAdjustments] + Comma + openAccessBillFiledsOAList[Constants.cOpenAccessTotalBill] + ")");

            ProgressUpdate(100);

            #endregion
        }

        #endregion

        private Boolean SaveAdditionParamsInfo()
        {
            try
            {
                tataDBContext.Database.ExecuteSqlCommand(
                    "INSERT INTO TML_BILLRATESVALUES (VAL_MONYR, MD_CHARGE, BASIC_CHARGE, AZONEVAL, BZONEVAL, CZONEVAL, DZONEVAL, FAC, ELEC_CHARGES, "
                + " TAX_CHARGE, PF, EHV, PROMPTPAY, OTH_MD_CHARGE, OTH_BASIC_CHARGE, OTH_AZONEVAL, OTH_BZONEVAL, OTH_CZONEVAL, OTH_DZONEVAL, OTH_FAC, "
                + " OTH_ELEC_CHARGES, OTH_TAX_CHARGE, OTH_PF, OTH_EHV, OTH_PROMPTPAY , CROSS_SUR_CHARGE, WHEEL_CHARGE, TRANS_CHARGE, OTH_PUR_DISCOUNT) VALUES ('"
                + "01-" + consumptionMonthFileDate + "'" + Comma + GetFieldValueFromForm("Field1") + Comma + GetFieldValueFromForm("Field2") + Comma
                + GetFieldValueFromForm("Field3") + Comma + GetFieldValueFromForm("Field4") + Comma + GetFieldValueFromForm("Field5") + Comma
                + GetFieldValueFromForm("Field6") + Comma + GetFieldValueFromForm("Field7") + Comma + GetFieldValueFromForm("Field8") + Comma
                + GetFieldValueFromForm("Field9") + Comma + GetFieldValueFromForm("Field10") + Comma + GetFieldValueFromForm("Field11") + Comma
                + GetFieldValueFromForm("Field12") + Comma + GetFieldValueFromForm("Field13") + Comma + GetFieldValueFromForm("Field14") + Comma
                + GetFieldValueFromForm("Field15") + Comma + GetFieldValueFromForm("Field16") + Comma + GetFieldValueFromForm("Field17") + Comma
                + GetFieldValueFromForm("Field18") + Comma + GetFieldValueFromForm("Field19") + Comma + GetFieldValueFromForm("Field20") + Comma
                + GetFieldValueFromForm("Field21") + Comma + GetFieldValueFromForm("Field22") + Comma + GetFieldValueFromForm("Field23") + Comma
                + GetFieldValueFromForm("Field24") + Comma + GetFieldValueFromForm("CrossCharges") + Comma + GetFieldValueFromForm("WheelingCharges") + Comma
                + GetFieldValueFromForm("TransCharges") + Comma + GetFieldValueFromForm("OthPurDiscnt") + ")");
                return true;
            }
            catch (Exception AdditionalParamSaveExp)
            {
                processList.Add(GetFormattedText(LogType.Error, "Unable to process Additonal Parameters Infos save due to DB Exp:  " + AdditionalParamSaveExp.ToString()));
                errorCode = ErrorCodes.UnableToSaveAdditionalParamsInfo;
                ProcessStateChanged(FileProcessingState.Failed, "Oops! Unable to Process Additional Parameters Info Save. Please try again later");
                RollbackTransaction();
                return false;
            }
        }

        private string GetFieldValueFromForm(string FieldName)
        {
            return HttpContext.Current.Request.Form[FieldName];
        }

        // Load Excel File into DataTable & return to caller.....
        private DataTable LoadExcelFile(string FilePath, FileTypes currFileType)
        {
            try
            {
                /* FileInfo f = new FileInfo(FilePath);
                f.MoveTo(System.IO.Path.ChangeExtension(FilePath, ".xlsx"));*/
                String FileExt = System.IO.Path.GetExtension(FilePath);
                //String FileExt = ".xlsx";
                string conStr = "";
                string isHDR = "No";
                switch (FileExt)
                {
                    case ".xls": //Excel 97-03
                        conStr = ConfigurationManager.ConnectionStrings["Excel07ConString"].ConnectionString;
                        break;
                    case ".xlsx": //Excel 07
                        conStr = ConfigurationManager.ConnectionStrings["Excel07ConString"].ConnectionString;
                        break;
                }
                conStr = String.Format(conStr, FilePath, isHDR);
                processList.Add(GetFormattedText(LogType.Info, conStr));
                OleDbConnection connExcel = new OleDbConnection(conStr);
                OleDbCommand cmdExcel = new OleDbCommand();
                OleDbDataAdapter oda = new OleDbDataAdapter();
                DataTable dt = new DataTable();
                cmdExcel.Connection = connExcel;

                //Get the name of First Sheet
                connExcel.Open();
                DataTable dtExcelSchema;
                dtExcelSchema = connExcel.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                string SheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
                connExcel.Close();

                //Read Data from First Sheet
                connExcel.Open();
                cmdExcel.CommandText = "SELECT * From [" + SheetName + "]";
                // cmdExcel.CommandText = "SELECT * From [" + SheetName + "B6:Y99]";
                oda.SelectCommand = cmdExcel;
                oda.Fill(dt);
                oda.Dispose();
                connExcel.Close();
                connExcel.Dispose();
                //processList.Add(GetFormattedText(LogType.Info, conStr));
                return dt;
            }
            catch (Exception loadExcelFile)
            {
                processList.Add(GetFormattedText(LogType.Error, "Unable to Load Excel File due to " + loadExcelFile.ToString()));
               
                if (currFileType == FileTypes.ConsumptionFile)
                {
                    errorCode = ErrorCodes.UnableToLoadConsumptionExcelFile;
                    ProcessStateChanged(FileProcessingState.Failed, "Oops! Unable to Process. Please try again later");
                    RollbackTransaction();
                }
                else if (currFileType == FileTypes.GenerationFile)
                {
                    errorCode = ErrorCodes.UnableToLoadGenerationExcelFile;

                    ProcessStateChanged(FileProcessingState.Failed, "Oops! Unable to Process. Please try again later");
                    RollbackTransaction();
                }
                else if (currFileType == FileTypes.TimeSlotConsumptionFile)
                {
                    errorCode = ErrorCodes.UnableToLoadTimeSlotConsExcelFile;
                    ProcessStateChanged(FileProcessingState.Failed, "Oops! Unable to Process. Please try again later");
                    RollbackTransaction();
                }
                else if (currFileType == FileTypes.MeterConsumptionFile)
                {
                    errorCode = ErrorCodes.UnableToLoadMeterConsExcelFile;
                    ProcessStateChanged(FileProcessingState.Failed, "Oops! Unable to Process. Please try again later");
                    RollbackTransaction();
                }
                else if (currFileType == FileTypes.OpenAccessBillFile)
                {
                    errorCode = ErrorCodes.UnableToLoadOpenAccessBillExcelFile;
                    ProcessStateChanged(FileProcessingState.Failed, "Oops! Unable to Process. Please try again later");
                    RollbackTransaction();
                }
                return null;
            }
        }


        #region DeleteExistedData
        private void DeleteExistedRecords(string currentMonth)
        {
            tataDBContext.Database.ExecuteSqlCommand("DELETE FROM TML_CONSUMPTION_DATA WHERE TO_CHAR(READING_DATE,'MON-YY') = '" + currentMonth + "'");
            tataDBContext.Database.ExecuteSqlCommand("DELETE FROM TML_CONSUMPTION_SUMMARY WHERE TO_CHAR(READING_DATE,'MON-YY') = '" + currentMonth + "'");
            var GenerationData = tataDBContext.TML_GENERATION_UNITS.ToList();

            foreach (TML_GENERATION_UNITS genRow in GenerationData)
            {
                tataDBContext.Database.ExecuteSqlCommand("DELETE FROM TML_" + genRow.SERIAL_NO + "_DATA WHERE TO_CHAR(READING_DATE,'MON-YY') = '" + currentMonth + "'");
            }
            tataDBContext.Database.ExecuteSqlCommand("DELETE FROM TML_GENERATION_SUMMARY WHERE TO_CHAR(READING_DATE,'MON-YY') = '" + currentMonth + "'");
            tataDBContext.Database.ExecuteSqlCommand("DELETE FROM TML_TS_CONSUMPTION_SUMMARY WHERE TO_CHAR(MAX_READING_DATE,'MON-YY') = '" + currentMonth + "'");
            tataDBContext.Database.ExecuteSqlCommand("DELETE FROM TML_BILLRATESVALUES WHERE TO_CHAR(VAL_MONYR,'MON-YY') = '" + currentMonth + "'");
            tataDBContext.Database.ExecuteSqlCommand("DELETE FROM TML_METER_CONSUM_MONTH_SUMMARY WHERE TO_CHAR(READING_DATE,'MON-YY') = '" + currentMonth + "'");
            tataDBContext.Database.ExecuteSqlCommand("DELETE FROM TML_OPEN_ACCESS_BILL_SUMMARY WHERE TO_CHAR(READING_DATE,'MON-YY') = '" + currentMonth + "'");
            tataDBContext.Database.ExecuteSqlCommand("DELETE FROM TML_OPEN_ACCESS_MSEDCL_BILL WHERE TO_CHAR(READING_DATE,'MON-YY') = '" + currentMonth + "'");
            tataDBContext.Database.ExecuteSqlCommand("DELETE FROM TML_OPEN_ACCESS_BILL WHERE TO_CHAR(READING_DATE,'MON-YY') = '" + currentMonth + "'");
            processList.Add(GetFormattedText(LogType.Info, "'" + consumptionMonthFileDate + "' deleted existed data"));
        }
        #endregion

        // Communication between Client & Server
        #region SendingInfoToClient

        private void ProcessStateChanged(FileProcessingState processState, string Msg = "")
        {
            currProcessState = processState;
            var context = GlobalHost.ConnectionManager.GetHubContext<UploadProcessHub>();
            context.Clients.All.processStateChanged(processState, Msg, errorCode);
        }

        private void ProgressUpdate(decimal Percentage)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<UploadProcessHub>();
            context.Clients.All.progessUpdate(Percentage);
        }

        private void sendMessageToClient(string Msg)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<UploadProcessHub>();
            context.Clients.All.updateProcessLog(Msg);
        }

        private void BothFilesHaveDifferentMonths(string Msg)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<UploadProcessHub>();
            context.Clients.All.updateProcessLog(Msg);
        }

        #endregion
    }

}
