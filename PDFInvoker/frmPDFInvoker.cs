using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using TestStack.White.UIItems.WindowItems;
using TestStack.White.Factory;
using TestStack.White.UIItems.Finders;

namespace PDFInvoker
{

    public partial class frmPDFInvoker : Form
    {
        private enum LogType { Info, Error, Alert, Warning };
        private ArrayList processList = new ArrayList();
        private FileSystemWatcher m_Watcher;        
        private StringBuilder m_Sb;
        private bool m_bDirty;  
        private string AppDefaultDir = "";

        private const string UploadFileDir = "UploadedFiles\\";
        private const string ProcessFile = "ProcessFiles.txt";
        private const string PDFConverterAppPath = "Tools\\PDFdu PDF To Excel.exe";
        private const string StatusFile = "Status.txt";
        private ExitDialog exitDailog;        

        // PDFToExcel File Conversion related variables
        private Window window;
        private TestStack.White.UIItems.Button btnAddFile;
        private TestStack.White.UIItems.TextBox txtSetFileName;
        private TestStack.White.UIItems.Button btnOpen;
        TestStack.White.Application pdfToExcelApp = null;

        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public frmPDFInvoker()
        {
            InitializeComponent();
            exitDailog = new ExitDialog();
            m_Sb = new StringBuilder();
            m_bDirty = false;
#if DEBUG
                AppDefaultDir = "D:\\Projects\\TataMotors\\Tata Motors Code\\Web API\\TataMotors\\TataMotorsWebAPI\\";
#else
            AppDefaultDir = "C:\\inetpub\\wwwroot\\TML\\";//  D:\\IIS Applications\\Mobile Apps\\TataMotors\\

#endif
            tbSelectedPath.Text = AppDefaultDir + UploadFileDir + ProcessFile;            
            GetFormattedText(LogType.Info, "Application Started");
            if (Directory.Exists(AppDefaultDir + "\\" + UploadFileDir))
            {
                startWatch();
            }
            else
            {
               GetFormattedText(LogType.Info, "Default Directory not found, Please choose ProcessFiles.txt file");
            }
#if !DEBUG
            SetStartup();
#endif
        }

        
    private void SetStartup()
    {
        try
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
            ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            rk.SetValue("PDFInvoker", Application.ExecutablePath.ToString());
        }
        catch(Exception startUPEXP)
        {
            GetFormattedText(LogType.Info, "Unable to setup for startup due to " + startUPEXP.ToString());
        }
    }


        private string GetFormattedText(LogType logType, string cmdText)
        {
            string formattedText = (DateTime.Now.ToString("dd-MMM HH:mm:ss.fff @ ") + cmdText);           
            System.Diagnostics.Debug.WriteLine(formattedText);
            if(rtbEventLog.Lines.Length > 1000)
            {
                rtbEventLog.Lines = rtbEventLog.Lines.Skip(1).ToArray();                
            }
            rtbEventLog.AppendText(formattedText + Environment.NewLine);
            return formattedText;
        }

        private Boolean ConvertPDFToExcel(Dictionary<string,string> ProcessInfo)
        {
            /*
             *  USE FULL LINKS FOR TestStack
             *  https://github.com/TestStack/White
             *  http://teststackwhite.readthedocs.io/en/latest/
             *  http://teststackwhite.readthedocs.io/en/latest/UIItems/#controltype-to-uiitem-mapping-for-primary-uiitems
             *  http://www.dreamincode.net/forums/topic/322108-c%23-teststackwhite-for-beginners/ (Examples)
             * */
            string windowslist = "Windows Count is :";
            
            processList.Add(GetFormattedText(LogType.Info, "PDF To Excel Conversion Process Initialization"));
            Boolean completedStatus = false;
            String tempStr = "";
            try
            {
                File.WriteAllText(AppDefaultDir + UploadFileDir + ProcessInfo["DIR"] + "\\" + StatusFile,
                    "Temp=0,Status=Convert Process Initiated");
                processList.Add(GetFormattedText(LogType.Info, "Convert Process Initiated"));

                pdfToExcelApp = TestStack.White.Application.Launch(AppDefaultDir + PDFConverterAppPath);                                
                SetForegroundWindow(pdfToExcelApp.Process.MainWindowHandle);

                processList.Add(GetFormattedText(LogType.Info, "Application Launched"));

                List<Window> allWindows = pdfToExcelApp.GetWindows();
                windowslist = windowslist + allWindows.Count();

                foreach (Window aWindow in allWindows)
                {
                    windowslist = windowslist + "- Window Name is :" + aWindow.Title;
                }
                windowslist = windowslist + " Window Title is " + pdfToExcelApp.Process.MainWindowTitle;
                processList.Add(GetFormattedText(LogType.Info, windowslist));

                window = pdfToExcelApp.GetWindow(SearchCriteria.ByText("PDFdu PDF To Excel (Registered) v1.3"), InitializeOption.NoCache);
                window.WaitWhileBusy();
                SetForegroundWindow(pdfToExcelApp.Process.MainWindowHandle);
                window.WaitWhileBusy();

                //window.LogStructure();  // To Display all components list with AutomationId in Window.



                processList.Add(GetFormattedText(LogType.Info, "Window Detected"));
                File.WriteAllText(AppDefaultDir + UploadFileDir + ProcessInfo["DIR"] + "\\" + StatusFile,
                    "Temp=0,Status=Window Detected");

                // Merge multiple worksheets into a single worksheet
                TestStack.White.UIItems.CheckBox chkMergeMultiple = window.Get<TestStack.White.UIItems.CheckBox>("chkCombine");
                window.WaitWhileBusy();
                SetForegroundWindow(pdfToExcelApp.Process.MainWindowHandle);
                window.WaitWhileBusy();                

                chkMergeMultiple.Checked = true;
                window.WaitWhileBusy();
                SetForegroundWindow(pdfToExcelApp.Process.MainWindowHandle);
                window.WaitWhileBusy();

                // Show Output folder when done
                TestStack.White.UIItems.CheckBox chkOpen = window.Get<TestStack.White.UIItems.CheckBox>("chkOpen");
                window.WaitWhileBusy();
                chkOpen.Checked = false;
                window.WaitWhileBusy();
                SetForegroundWindow(pdfToExcelApp.Process.MainWindowHandle);
                window.WaitWhileBusy();

                // Original Folder
                TestStack.White.UIItems.RadioButton rdoSaveOld = window.Get<TestStack.White.UIItems.RadioButton>("rdoSaveOld");
                window.WaitWhileBusy();
                rdoSaveOld.IsSelected = true;
                window.WaitWhileBusy();
                SetForegroundWindow(pdfToExcelApp.Process.MainWindowHandle);
                window.WaitWhileBusy();

                TestStack.White.UIItems.RadioButton rdoXlsx = window.Get<TestStack.White.UIItems.RadioButton>("rdoXlsx");
                window.WaitWhileBusy();
                rdoXlsx.IsSelected = true;
                window.WaitWhileBusy();
                SetForegroundWindow(pdfToExcelApp.Process.MainWindowHandle);
                window.WaitWhileBusy();

                // Custom File Name
                TestStack.White.UIItems.CheckBox chkCustomFileName = window.Get<TestStack.White.UIItems.CheckBox>("chkFileName");
                window.WaitWhileBusy();
                chkCustomFileName.Checked = false;
                window.WaitWhileBusy();
                SetForegroundWindow(pdfToExcelApp.Process.MainWindowHandle);
                window.WaitWhileBusy();

                // Add file name prefix
                TestStack.White.UIItems.CheckBox chkBeforeName = window.Get<TestStack.White.UIItems.CheckBox>("chkBeforeName");
                window.WaitWhileBusy();
                chkBeforeName.Checked = false;
                window.WaitWhileBusy();
                SetForegroundWindow(pdfToExcelApp.Process.MainWindowHandle);
                window.WaitWhileBusy();

                // Add file name suffix
                TestStack.White.UIItems.CheckBox chkAfterName = window.Get<TestStack.White.UIItems.CheckBox>("chkAfterName");
                window.WaitWhileBusy();
                chkAfterName.Checked = false;
                window.WaitWhileBusy();
                SetForegroundWindow(pdfToExcelApp.Process.MainWindowHandle);
                window.WaitWhileBusy();

                AddFilesToConvert(AppDefaultDir + UploadFileDir + ProcessInfo["DIR"] + "\\" + ProcessInfo["CON"]);
                processList.Add(GetFormattedText(LogType.Info, "Consumption File Assigned"));
                window.WaitWhileBusy();

                AddFilesToConvert(AppDefaultDir + UploadFileDir + ProcessInfo["DIR"] + "\\" + ProcessInfo["GEN"]);
                processList.Add(GetFormattedText(LogType.Info, "Generation File Assigned"));
                window.WaitWhileBusy();

                AddFilesToConvert(AppDefaultDir + UploadFileDir + ProcessInfo["DIR"] + "\\" + ProcessInfo["TSC"]);
                processList.Add(GetFormattedText(LogType.Info, "Time Slot File Assigned"));
                window.WaitWhileBusy();

                AddFilesToConvert(AppDefaultDir + UploadFileDir + ProcessInfo["DIR"] + "\\" + ProcessInfo["MCON"]);
                processList.Add(GetFormattedText(LogType.Info, "Meter Consumption File Assigned"));
                window.WaitWhileBusy();

                AddFilesToConvert(AppDefaultDir + UploadFileDir + ProcessInfo["DIR"] + "\\" + ProcessInfo["OAB"]);
                processList.Add(GetFormattedText(LogType.Info, "Open Access Bill File Assigned"));
                window.WaitWhileBusy();

               

                processList.Add(GetFormattedText(LogType.Info, "Convert Processing Initiated......"));
                // Now Start Convert PDF to Excel
                TestStack.White.UIItems.Button btnMakePDF = window.Get<TestStack.White.UIItems.Button>("btnMakePDF");
                window.WaitWhileBusy();
                btnMakePDF.Click();
                window.WaitWhileBusy();

                processList.Add(GetFormattedText(LogType.Info, "Convert Processing Started......"));

                // Display Progress Window
                TestStack.White.UIItems.Label lblDoing = window.Get<TestStack.White.UIItems.Label>(SearchCriteria.ByAutomationId("lblDoing"));
                window.WaitWhileBusy();
                String currProgressText = "";              
                do
                {
                    processList.Add(GetFormattedText(LogType.Info, "Progress Text : " + lblDoing.Text));
                    if (!currProgressText.Equals(lblDoing.Text))
                    {
                        if(lblDoing.Text.Contains("1/5"))
                        {
                            tempStr = "CONS";
                        }
                        else if (lblDoing.Text.Contains("2/5"))
                        {
                            tempStr = "GEN";
                        }
                        else if (lblDoing.Text.Contains("3/5"))
                        {
                            tempStr = "TSC";
                        }
                        else if (lblDoing.Text.Contains("4/5"))
                        {
                            tempStr = "MCON";
                        }
                        else if (lblDoing.Text.Contains("5/5"))
                        {
                            tempStr = "OAB";
                        }
                        File.WriteAllText(AppDefaultDir + UploadFileDir + ProcessInfo["DIR"] + "\\" + StatusFile,
                        String.Format("State={0},Status={1}", tempStr, lblDoing.Text));
                    }
                    SetForegroundWindow(pdfToExcelApp.Process.MainWindowHandle);
                    window.WaitWhileBusy();
                    Thread.Sleep(500);
                } while (!lblDoing.Text.Contains("Please Wait ..."));   
             
                window.WaitWhileBusy();
                SetForegroundWindow(pdfToExcelApp.Process.MainWindowHandle);
                window.WaitWhileBusy();

                completedStatus = true;
                File.WriteAllText(AppDefaultDir + UploadFileDir + ProcessInfo["DIR"] + "\\" + StatusFile,
                    String.Format("State={0},Status={1}","Completed","Completed"));                 
                processList.Add(GetFormattedText(LogType.Info, "Convert Process Completed"));

                // Display Message Dialog after Process completed..
                SetForegroundWindow(pdfToExcelApp.Process.MainWindowHandle);
                window.WaitWhileBusy();
                TestStack.White.UIItems.Button btnCompleted = window.Get<TestStack.White.UIItems.Button>("2");
                window.WaitWhileBusy();
                btnCompleted.Click();

                pdfToExcelApp.Kill();
                
                processList.Add(GetFormattedText(LogType.Info, "Convert PDF To Closed"));              
                return true;
            }
            catch (Exception convertPDFToExcelExp)
            {
                var annotatedException = new Exception("ConvertPDFToExcel-" + windowslist, convertPDFToExcelExp);                
                processList.Add(GetFormattedText(LogType.Error, "Unable to convert PDF To Excel due to " + convertPDFToExcelExp.ToString()));
                if (!completedStatus)  // Some times exception raised after file processing completed.. like Unable to find Completed button
                {
                    File.WriteAllText(AppDefaultDir + UploadFileDir + ProcessInfo["DIR"] + "\\" + StatusFile,
                    String.Format("State={0},Status={1}", tempStr, "Failed"));
                }
                if (pdfToExcelApp != null)
                {
                    pdfToExcelApp.Kill();
                }                
                return false;
            }
        }

        private void AddFilesToConvert(String FileName)
        {
            // Add File for Consumption File
            SetForegroundWindow(pdfToExcelApp.Process.MainWindowHandle);
            btnAddFile = window.Get<TestStack.White.UIItems.Button>("btnAddFile");
            window.WaitWhileBusy();
            btnAddFile.Click();
            window.WaitWhileBusy();
            SetForegroundWindow(pdfToExcelApp.Process.MainWindowHandle);
            window.WaitWhileBusy();

            // Set File which one is to be process
            SetForegroundWindow(pdfToExcelApp.Process.MainWindowHandle);
            window.WaitWhileBusy();
            txtSetFileName = window.Get<TestStack.White.UIItems.TextBox>("1148");
            window.WaitWhileBusy();
            SetForegroundWindow(pdfToExcelApp.Process.MainWindowHandle);
            window.WaitWhileBusy();
            txtSetFileName.SetValue(FileName);
            window.WaitWhileBusy();
            SetForegroundWindow(pdfToExcelApp.Process.MainWindowHandle);
            window.WaitWhileBusy();

            // Click Open Button
            SetForegroundWindow(pdfToExcelApp.Process.MainWindowHandle);
            window.WaitWhileBusy();
            btnOpen = window.Get<TestStack.White.UIItems.Button>("1");
            window.WaitWhileBusy();
            btnOpen.Click();
            window.WaitWhileBusy();
            SetForegroundWindow(pdfToExcelApp.Process.MainWindowHandle);
            window.WaitWhileBusy();
        }

        private void startWatch()
        {
            if(m_Watcher != null)
            {
                GetFormattedText(LogType.Info, "Trying Watcher restarting...");
                m_Watcher.EnableRaisingEvents = false;
                m_Watcher.Dispose();
            }
            else
            {
                GetFormattedText(LogType.Info, "New Watcher starting...");
            }
            m_Watcher = new FileSystemWatcher();
            m_Watcher.Filter = tbSelectedPath.Text.Substring(tbSelectedPath.Text.LastIndexOf('\\') + 1);
            m_Watcher.Path = tbSelectedPath.Text.Substring(0, tbSelectedPath.Text.Length - m_Watcher.Filter.Length);
            m_Watcher.IncludeSubdirectories = true;            
            m_Watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                     | NotifyFilters.FileName | NotifyFilters.DirectoryName;

            m_Watcher.Changed += new FileSystemEventHandler(OnChanged);
            m_Watcher.Created += new FileSystemEventHandler(OnChanged);
            //m_Watcher.Deleted += new FileSystemEventHandler(OnChanged);
            m_Watcher.Renamed += new RenamedEventHandler(OnRenamed);
            m_Watcher.EnableRaisingEvents = true;
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (!m_bDirty)
            {
                m_Sb.Remove(0, m_Sb.Length);
                m_Sb.Append(e.FullPath);
                m_Sb.Append(" ");
                m_Sb.Append(e.ChangeType.ToString());
                m_Sb.Append("    ");
                m_Sb.Append(DateTime.Now.ToString());                
                m_bDirty = true;
            }
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            if (!m_bDirty)
            {
                m_Sb.Remove(0, m_Sb.Length);
                m_Sb.Append(e.OldFullPath);
                m_Sb.Append(" ");
                m_Sb.Append(e.ChangeType.ToString());
                m_Sb.Append(" ");
                m_Sb.Append("to ");
                m_Sb.Append(e.Name);
                m_Sb.Append("    ");
                m_Sb.Append(DateTime.Now.ToString());                
                m_bDirty = true;                
            }
        }

        private void btnChoosePath_Click(object sender, EventArgs e)
        {            
            if (odlgSelectFile.ShowDialog() == DialogResult.OK)
            {
                tbSelectedPath.Text = odlgSelectFile.FileName;
                GetFormattedText(LogType.Info, "Default Path Changed : " + tbSelectedPath.Text);
                startWatch();
            }
        }

        private void tmrEditNotify_Tick(object sender, EventArgs e)
        {
            if (m_bDirty)
            {
                GetFormattedText(LogType.Info, m_Sb.ToString());
                Dictionary<string,string> ProcessInfo = LoadProcessFile();
                if (ProcessInfo != null)
                {
                    this.BringToFront();
                    this.WindowState = FormWindowState.Normal;
                    SetForegroundWindow(this.Handle);                    
                    ConvertPDFToExcel(ProcessInfo);
                }
                m_bDirty = false;
            }
        }
        
        private Dictionary<string,string> LoadProcessFile()
        {
          
            string lastline = File.ReadLines(tbSelectedPath.Text).Last();
            GetFormattedText(LogType.Info, "Last File Upload is :" + lastline);
            var processFileList = lastline.Split(',').Select(x => x.Split('=')).ToDictionary(x => x[0], x => x[1]);
            if(processFileList.Count < 3)
            {
                GetFormattedText(LogType.Info, "Unable to start Process becoz of some fields info not availble" + lastline);
                return null;
            }
            else if (File.Exists(AppDefaultDir + UploadFileDir + processFileList["DIR"] + StatusFile))
            {
                GetFormattedText(LogType.Info, "Already processed" + lastline);
                return null; 
            }
            else
            {
                return processFileList;
            }           
        }

        private void frmPDFInvoker_FormClosing(object sender, FormClosingEventArgs e)
        {
            exitDailog.clearPassword();
            if ((exitDailog.ShowDialog() == DialogResult.OK) && (exitDailog.GetPassword().Equals("Eff@TML")))
            {
                e.Cancel = false;
            }
            else
            {
                // Cancel the Closing event from closing the form.
                e.Cancel = true;
                GetFormattedText(LogType.Error, "Invalid Password");
            }

        }
    }
}
