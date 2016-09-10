//*****************************
//#define dev
//*****************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.IO;

namespace APS_DiscProcessor
{
    public partial class Main_Looper_Form : Form
    {
        TaskMethods TM = null;
        string sLogText = string.Empty;
        FileStream fsLock;
        StringBuilder sbLog = new StringBuilder();
        string sCDSConnString = APS_DiscProcessor.Properties.Settings.Default.CDSConnString.ToString();
        string sDP2ConnString = APS_DiscProcessor.Properties.Settings.Default.DP2ConnString.ToString();
        string sDiscProcessorConnString = APS_DiscProcessor.Properties.Settings.Default.DiscProcessorConnString.ToString();
        bool bDoAll = false;
        bool bCheckForRenderedImages = false;
        bool bRunTaskChecks = false;
        bool bIdle = false;
        bool bGatherAndProcessSittingBasedWorkOnly = false;
        bool bGatherAndProcessFrameBasedWorkOnly = false;

#if(dev)
        bool bDebug = true;
#endif
#if(!dev)
        bool bDebug = false;
#endif

        int iStartRowCount;
        int iEndRowCount;

        public Main_Looper_Form()
        {
            InitializeComponent();

            TM = new TaskMethods();
        }

        #region Form events.

        private void Main_Looper_Form_Load(object sender, EventArgs e)
        {
            DataTable dTblDPLooperMachine = new DataTable("dTblDPLooperMachine");
            string sCommText = "SELECT [Value] FROM [Variables] WHERE [Label] = 'DP_LooperMachine'";

            TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblDPLooperMachine);

            if (dTblDPLooperMachine.Rows.Count > 0)
            {
                string sDPLooperMachine = Convert.ToString(dTblDPLooperMachine.Rows[0]["Value"]).Trim();
                string sMachineName = Convert.ToString(Environment.MachineName).Trim();

                if (sDPLooperMachine == sMachineName)
                {
                    this.comboBoxWork.SelectedIndex = 0;
                }
                else if (sDPLooperMachine != sMachineName)
                {
                    this.comboBoxWork.SelectedIndex = 5;
                }                
            }
            else if (dTblDPLooperMachine.Rows.Count == 0)
            {
                string sStop = string.Empty;
            }

            string sVersion = string.Empty;

            DataTable dTblVersion = new DataTable("dTblVersion");
            sCommText = "SELECT * FROM [ChangeLog] WHERE [App] = 'Processor'";

            TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblVersion);

            if (dTblVersion.Rows.Count > 0)
            {
                DataRow dRowVersion = dTblVersion.Rows[dTblVersion.Rows.Count - 1];

                sVersion = Convert.ToString(dRowVersion["Version"]).Trim();

                this.Text = "A.P.S. DiscProcessor v" + sVersion;

                Application.DoEvents();
            }
            else if (dTblVersion.Rows.Count == 0)
            {
                string sStop = string.Empty;
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            this.DoWork01();
        }

        private void rtxtboxLog_TextChanged(object sender, EventArgs e)
        {
            try
            {
                rtxtboxLog.SelectionStart = rtxtboxLog.Text.Length;
                rtxtboxLog.ScrollToCaret();
                rtxtboxLog.Refresh();
            }
            catch(Exception ex)
            {
                TM.SaveExceptionToDB(ex);
            }
        }

        private void timerDoWork01_Tick(object sender, EventArgs e)
        {
            this.DoWork01();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // On click event verify the user wishes to exit the program.
            DialogResult verifyExit;
            verifyExit = MessageBox.Show("Exit the program?", "Exit?", MessageBoxButtons.YesNo);
            // Exit the application if yes is chosen.
            if (verifyExit == DialogResult.Yes)
            {
                Application.Exit();
            }
            else if (verifyExit == DialogResult.No)
            {
                // Do nothing if the user answers no.
                return;
            }
        }

        private void resubmitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            ICDorMEGResubmit RS = new ICDorMEGResubmit();
            RS.ShowDialog();
            this.Enabled = true;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            About Abt = new About();
            Abt.ShowDialog();
            this.Enabled = true;
        }

        private void showLogFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataTable dTblLogFile = new DataTable("dTblLogFile");
            string sCommText = "SELECT [Value] FROM [Variables] WHERE [Label] = 'CurrentDPLogFile'";

            TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblLogFile);

            if (dTblLogFile.Rows.Count > 0)
            {
                string sLogFile = Convert.ToString(dTblLogFile.Rows[0]["Value"]).Trim();

                if (sLogFile != string.Empty || sLogFile != "")
                {
                    if (File.Exists(sLogFile))
                    {
                        Process.Start(sLogFile);
                    }
                    else if (!File.Exists(sLogFile))
                    {
                        MessageBox.Show("Log file does not exist in specified location.");
                    }
                }
                else if (sLogFile == string.Empty)
                {
                    MessageBox.Show("No current log file.");
                }
            }
            else if (dTblLogFile.Rows.Count == 0)
            {
                string sStop = string.Empty;
            }
        }

        private void forceNewLogFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (bDebug == true)
            {
                string sCommText = "UPDATE [Variables] SET [Value] = '' WHERE [Label] = 'CurrentDPLogFile'";
                bool bSuccess = false;

                TM.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                if (bSuccess == true)
                {
                    MessageBox.Show("Successfully forced the generation of a new log file.");
                }
                else if (bSuccess != true)
                {
                    MessageBox.Show("Failed to force the generation of a new log file.");
                }
            }
            else if (bDebug != true)
            {
                bool bLoginSuccess = false;
                string sLogInUser = string.Empty;

                this.Enabled = false;
                Login Lgn02 = new Login();
                Lgn02.ShowDialog();
                bLoginSuccess = Lgn02.bLoginSuccess;
                sLogInUser = Lgn02.sUser;

                if (bLoginSuccess == true)
                {
                    DataTable dTblCurrentLogFile = new DataTable("dTblCurrentLogFile");
                    string sCommText = "SELECT [Value] FROM [Variables] WHERE [Label] = 'CurrentDPLogFile'";

                    TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblCurrentLogFile);

                    if (dTblCurrentLogFile.Rows.Count > 0)
                    {
                        string sCurrentLogFile = Convert.ToString(dTblCurrentLogFile.Rows[0]["Value"]).Trim();

                        if (sCurrentLogFile != string.Empty || sCurrentLogFile != "")
                        {
                            sCommText = "UPDATE [Variables] SET [Value] = '' WHERE [Label] = 'CurrentDPLogFile'";
                            bool bSuccess = false;

                            TM.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                            if (bSuccess == true)
                            {
                                string sDTime1 = DateTime.Now.ToString("MM-dd-yy").Trim();
                                string sDTime2 = DateTime.Now.ToString("HH:mm:ss").Trim();
                                string sDTime3 = "[" + sDTime1 + "][" + sDTime2 + "]";

                                string sText = Environment.NewLine + Environment.NewLine + sDTime3 + "[Secured login by: " + sLogInUser + " ]" + Environment.NewLine + Environment.NewLine +
                                    " Forced the generation of a new log file." + Environment.NewLine + Environment.NewLine;

                                File.AppendAllText(sCurrentLogFile, Environment.NewLine + Environment.NewLine + sText + Environment.NewLine + Environment.NewLine);

                                MessageBox.Show("Successfully forced the generation of a new log file.");
                            }
                            else if (bSuccess != true)
                            {
                                string sDTime1 = DateTime.Now.ToString("MM-dd-yy").Trim();
                                string sDTime2 = DateTime.Now.ToString("HH:mm:ss").Trim();
                                string sDTime3 = "[" + sDTime1 + "][" + sDTime2 + "]";

                                string sText = Environment.NewLine + Environment.NewLine + sDTime3 + "[Secured login by: " + sLogInUser + " ]" + Environment.NewLine + Environment.NewLine +
                                    " Failed to force the generation of a new log file." + Environment.NewLine + Environment.NewLine;

                                File.AppendAllText(sCurrentLogFile, Environment.NewLine + Environment.NewLine + sText + Environment.NewLine + Environment.NewLine);

                                MessageBox.Show("Failed to force the generation of a new log file.");
                            }

                            this.Enabled = true;
                        }
                        else if (sCurrentLogFile == string.Empty)
                        {
                            MessageBox.Show("No current log file on record.");
                            this.Enabled = true;
                        }
                    }
                    else if (dTblCurrentLogFile.Rows.Count == 0)
                    {
                        string sStop = string.Empty;
                        MessageBox.Show("Error gathering data from the Variables table.");
                        this.Enabled = true;
                    }
                }
                else if (bLoginSuccess != true)
                {
                    MessageBox.Show("Login failed. Action not allowed.");
                    this.Enabled = true;
                }
            }
        }

        private void changeVariablesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.comboBoxWork.SelectedIndex = 5;

            if (bDebug == true)
            {
                ProjectTableViewer CV = new ProjectTableViewer();
                CV.ShowDialog();
                this.Enabled = true;
            }
            else if (bDebug != true)
            {
                bool bLoginSuccess = false;
                string sLogInUser = string.Empty;

                this.Enabled = false;
                Login Lgn01 = new Login();
                Lgn01.ShowDialog();
                bLoginSuccess = Lgn01.bLoginSuccess;
                sLogInUser = Lgn01.sUser;

                if (bLoginSuccess == true)
                {
                    string sCurrentLogFile = string.Empty;
                    bool bLogFileExists = false;

                    TM.GetCurrentLogFile(ref sCurrentLogFile, ref bLogFileExists);

                    if (bLogFileExists == true)
                    {
                        string sDTime1 = DateTime.Now.ToString("MM-dd-yy").Trim();
                        string sDTime2 = DateTime.Now.ToString("HH:mm:ss").Trim();
                        string sDTime3 = "[" + sDTime1 + "][" + sDTime2 + "]";

                        string sText = Environment.NewLine + Environment.NewLine + sDTime3 + "[Secured login by: " + sLogInUser + " ]";

                        File.AppendAllText(sCurrentLogFile, Environment.NewLine + Environment.NewLine + sText + Environment.NewLine + Environment.NewLine);

                        ProjectTableViewer pTV = new ProjectTableViewer();
                        pTV.ShowDialog();
                        this.Enabled = true;
                    }
                    else if (bLogFileExists != true)
                    {
                        MessageBox.Show("No log file recorded in the Variables table.");
                        this.Enabled = true;
                    }
                }
                else if (bLoginSuccess != true)
                {
                    MessageBox.Show("Secured login failed.");
                    this.Enabled = true;
                }
            }
        }

        private void comboBoxWork_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxWork.SelectedIndex == 0)
            {
                this.lblMode.ForeColor = Color.Green;

                bGatherAndProcessFrameBasedWorkOnly = false;
                bCheckForRenderedImages = false;
                bRunTaskChecks = false;
                bIdle = false;
                bGatherAndProcessSittingBasedWorkOnly = false;
                bDoAll = true;

                this.btnStart.Enabled = true;

                this.timerDoWork01.Enabled = true;
                this.timerDoWork01.Start();
            }
            else if (comboBoxWork.SelectedIndex == 1)
            {
                this.lblMode.ForeColor = Color.Green;

                bDoAll = false;
                bCheckForRenderedImages = false;
                bRunTaskChecks = false;
                bIdle = false;
                bGatherAndProcessSittingBasedWorkOnly = false;
                bGatherAndProcessFrameBasedWorkOnly = true;

                this.btnStart.Enabled = true;

                this.timerDoWork01.Enabled = true;
                this.timerDoWork01.Start();
            }
            else if (comboBoxWork.SelectedIndex == 2)
            {
                this.lblMode.ForeColor = Color.Green;

                bGatherAndProcessFrameBasedWorkOnly = false;
                bDoAll = false;
                bCheckForRenderedImages = false;
                bRunTaskChecks = false;
                bIdle = false;
                bGatherAndProcessSittingBasedWorkOnly = true;

                this.btnStart.Enabled = true;

                this.timerDoWork01.Enabled = true;
                this.timerDoWork01.Start();
            }
            else if (comboBoxWork.SelectedIndex == 3)
            {
                this.lblMode.ForeColor = Color.Green;

                bGatherAndProcessFrameBasedWorkOnly = false;
                bRunTaskChecks = false;
                bIdle = false;
                bGatherAndProcessSittingBasedWorkOnly = false;
                bDoAll = false;
                bCheckForRenderedImages = true;

                this.btnStart.Enabled = true;

                this.timerDoWork01.Enabled = true;
                this.timerDoWork01.Start();
            }
            else if (comboBoxWork.SelectedIndex == 4)
            {
                this.lblMode.ForeColor = Color.Green;

                bGatherAndProcessFrameBasedWorkOnly = false;
                bIdle = false;
                bGatherAndProcessSittingBasedWorkOnly = false;
                bDoAll = false;
                bCheckForRenderedImages = false;
                bRunTaskChecks = true;

                this.btnStart.Enabled = true;

                this.timerDoWork01.Enabled = true;
                this.timerDoWork01.Start();
            }
            else if (comboBoxWork.SelectedIndex == 5)
            {
                this.lblMode.ForeColor = Color.Red;

                bGatherAndProcessFrameBasedWorkOnly = false;
                bGatherAndProcessSittingBasedWorkOnly = false;
                bDoAll = false;
                bCheckForRenderedImages = false;
                bRunTaskChecks = false;
                bIdle = true;

                this.Clear();

                this.toolStripStatusLabel1.Text = "[ Status: Idle ]";
                Application.DoEvents();

                this.btnStart.Enabled = false;

                this.timerDoWork01.Stop();
                this.timerDoWork01.Enabled = false;
            }
        }

        private void reportGeneratorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            ReportGenerator RG = new ReportGenerator();
            RG.ShowDialog();
            this.Enabled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            string sStop = string.Empty;

            e.SuppressKeyPress = true; // do this to 'eat' the keystroke
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Escape)
            {
                this.comboBoxWork.SelectedIndex = 5;
            }
        }

        #endregion        

        private void DoWork01()
        {
            try
            {
                if (bIdle != true)
                {
                    this.btnStart.Enabled = false;
                    this.comboBoxWork.Enabled = false;

                    this.timerDoWork01.Stop();
                    this.timerDoWork01.Enabled = false;

                    string sDTStart = DateTime.Now.ToString("HH:mm:ss").Trim();

                    this.toolStripStatusLabel1.Text = "Status: [Current cycle started: " + DateTime.Now.ToString().Trim() + "]";
                    Application.DoEvents();

                    this.Clear(); // Clears the form and all global variable objects.

                    if (comboBoxWork.SelectedIndex == 0)
                    {
                        this.InitialGatheringFromCDS(); // Gather work to do based on disc type from CDS.
                        this.ProcessDiscOrdersRecords(); // Processes the records from the DiscOrders table and injects the collected data into the FrameData table.
                        this.ProcessFrameDataRecords(); // Generate the ExportDef files, Merge.txt, etc needed for disc production based on the associated FrameData records.
                    }
                    if (comboBoxWork.SelectedIndex == 0 || comboBoxWork.SelectedIndex == 3)
                    {
                        this.ProcessRenderedOrders(); // Check for rendered images based on status (35) in the DiscOrders table.
                    }
                    if (comboBoxWork.SelectedIndex == 1)
                    {
                        this.GatherFrameBasedWorkOnly();
                        this.ProcessDiscOrdersRecords();
                        this.ProcessFrameDataRecords();
                        this.CheckForFrameBasedRenderedImages();
                    }
                    if (comboBoxWork.SelectedIndex == 2)
                    {
                        this.GatherAndProcessSittingBasedWorkOnly();
                        this.ProcessDiscOrdersRecords();
                        this.CheckForSittingBasedRenderedImages();
                    }
                    else if (comboBoxWork.SelectedIndex == 0 || comboBoxWork.SelectedIndex == 1 || comboBoxWork.SelectedIndex == 2 || comboBoxWork.SelectedIndex == 3 || comboBoxWork.SelectedIndex == 4)
                    {
                        string sTaskText = "[Checking for errors.]";
                        this.LogText(sTaskText);

                        TM.CheckForErrors(); // Checks the DiscOrders and Errors table for errors, issues or exceptions.

                        sTaskText = "[Checking for stalled work.]";
                        this.LogText(sTaskText);

                        TM.CheckForStalledWork(); // Checks the status of orders and notifies of any stalled work (ex: not rendered after 2 hours)

                        sTaskText = "[Checking for resubmits.]";
                        this.LogText(sTaskText);

                        TM.CheckForResubmits(); // Checks a Fox Pro table (DP_resubmits) for any user submitted work.
                    }

                    string sDTime1 = DateTime.Now.ToString("MM-dd-yy").Trim();
                    string sDTime2 = DateTime.Now.ToString("HH:mm:ss").Trim();
                    string sDTime3 = "[" + sDTime1 + "][" + sDTime2 + "]";

                    string sText = sDTime3 + "[This cycle has completed.]" + Environment.NewLine + sDTime3 + "[Idle for 10 minutes.]" + Environment.NewLine + Environment.NewLine;
                    this.SetLogTextFinished(sText);

                    string sNextCycle = DateTime.Now.AddMinutes(10).ToString("HH:mm:ss").Trim();

                    string sDTEnd = DateTime.Now.ToString("HH:mm:ss").Trim();

                    TimeSpan tSpanDuration = DateTime.Parse(sDTEnd).Subtract(DateTime.Parse(sDTStart));

                    this.toolStripStatusLabel1.Text = "[ Status: Idle ][ Duration of last cycle: " + tSpanDuration + " ][ Next cycle: " + sNextCycle + " ]";
                    Application.DoEvents();

                    this.btnStart.Enabled = true;
                    this.comboBoxWork.Enabled = true;

                    this.timerDoWork01.Enabled = true;
                    this.timerDoWork01.Start();
                }
                else if (bIdle == true)
                {
                    MessageBox.Show("Idling...");
                }
            }
            catch(Exception ex)
            {
                TM.SaveExceptionToDB(ex);
            }
        }

        private void InitialGatheringFromCDS()
        {
            this.SearchCDSForAllReadyWork(); // Search the CDS tables for any jobs containing ICD orders.
        }

        private void GatherFrameBasedWorkOnly()
        {
            this.SearchCDSForFrameBasedReadyWork();
        }

        private void GatherAndProcessSittingBasedWorkOnly()
        {
            this.SearchCDSForSittingBasedReadyWork();
        }

        private void ProcessDiscOrdersRecords()
        {
            this.QueryDiscOrdersForDistinctProdNum(); // Search the DiscOrders table for jobs ready to be processed.
        }

        private void ProcessFrameDataRecords()
        {
            this.ExportDefGeneration(); // Search the FrameData table for jobs to be processed.
        }

        private void ProcessRenderedOrders()
        {
            this.CheckForFrameBasedRenderedImages();
            this.CheckForSittingBasedRenderedImages();
        }

        #region Search for work in the CDS tables.

        // Search CDS for ICD ready jobs.
        private void SearchCDSForAllReadyWork()
        {
            try
            {
                double dSearchDays = 0;
                DataTable dtLabels = new DataTable();

                string sLabel = "GatherDays";
                string sValue = "Value";
                string sVariable = string.Empty;
                bool bSuccess = true;

                TM.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                if (bSuccess == true)
                {
                    dSearchDays = Convert.ToDouble(sVariable);

                    DataTable dTblGatherDiscTypes = new DataTable("dTblGatherDiscTypes");
                    string sCommText = "SELECT * FROM [GatherDiscTypes] WHERE [Gather] = '1'";

                    TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblGatherDiscTypes);

                    if (dTblGatherDiscTypes.Rows.Count > 0)
                    {
                        foreach (DataRow dRowGatherDiscTypes in dTblGatherDiscTypes.Rows)
                        {
                            string sDiscType = Convert.ToString(dRowGatherDiscTypes["GatherDiscType"]).Trim();
                            bool bWeCodeCheck = Convert.ToBoolean(dRowGatherDiscTypes["WebCodeCheck"]);
                            bool bSittingBased = Convert.ToBoolean(dRowGatherDiscTypes["SittingBased"]);
                            bool bGather = Convert.ToBoolean(dRowGatherDiscTypes["Gather"]);
                            bool bInDev = Convert.ToBoolean(dRowGatherDiscTypes["InDevelopment"]);

                            if (bGather == true && bInDev != true)
                            {
                                string sText = "[Checking for " + sDiscType + " work.]";
                                this.LogText(sText);

                                dtLabels.Clear();

                                DateTime DTNowMinus30 = DateTime.Now.AddDays(dSearchDays);
                                DateTime DTNowMinus30DateOnly = DTNowMinus30.Date;

                                string sDTNowDateOnly = DateTime.Now.Date.ToString("MM/dd/yy").Trim();
                                string sDTNowMinus30DateOnly = DTNowMinus30DateOnly.ToString("MM/dd/yy");

                                sCommText = "SELECT Lookupnum FROM ITEMS WHERE items.d_dueout > CTOD('" + sDTNowMinus30DateOnly + "') AND ITEMS.PACKAGETAG IN" +
                                    " (SELECT PACKAGETAG FROM LABELS WHERE LABELS.CODE = '" + sDiscType + "' AND LABELS.PACKAGETAG <> '    ') ORDER BY items.d_dueout ASC";

                                TM.CDSQuery(sCDSConnString, sCommText, dtLabels);

                                if (dtLabels.Rows.Count > 0)
                                {
                                    if (sDiscType != "PEC")
                                    {
                                        int iRowCount = dtLabels.Rows.Count;

                                        sText = "[Collected " + iRowCount + " records for processing.]";
                                        this.LogText(sText);

                                        this.ScanForTriggerPoints(dtLabels, sDiscType, bWeCodeCheck, bSittingBased);
                                    }
                                    else if (sDiscType == "PEC")
                                    {
                                        this.CheckForALaCarteWork(sDiscType, dtLabels, sDTNowMinus30DateOnly, bWeCodeCheck, bSittingBased);
                                    }
                                }
                                else if (dtLabels.Rows.Count == 0)
                                {
                                    if (sDiscType == "PEC")
                                    {
                                        this.CheckForALaCarteWork(sDiscType, dtLabels, sDTNowMinus30DateOnly, bWeCodeCheck, bSittingBased);
                                    }

                                    sText = "[No records collected this cycle.]";
                                    this.LogText(sText);
                                }
                            }
                        }
                    }
                    else if (dTblGatherDiscTypes.Rows.Count == 0)
                    {
                        string sStop = string.Empty;
                    }
                }
                else if (bSuccess != true)
                {
                    string sStop = string.Empty;
                }
            }
            catch(Exception ex)
            {
                TM.SaveExceptionToDB(ex);
            }
        }

        private void SearchCDSForFrameBasedReadyWork()
        {
            try
            {
                double dSearchDays = 0;
                DataTable dtLabels = new DataTable();

                string sLabel = "GatherDays";
                string sValue = "Value";
                string sVariable = string.Empty;
                bool bSuccess = true;

                TM.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                if (bSuccess == true)
                {
                    dSearchDays = Convert.ToDouble(sVariable);

                    DataTable dTblGatherDiscTypes = new DataTable("dTblGatherDiscTypes");
                    string sCommText = "SELECT * FROM [GatherDiscTypes] WHERE [Gather] = '1'";

                    TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblGatherDiscTypes);

                    if (dTblGatherDiscTypes.Rows.Count > 0)
                    {
                        foreach (DataRow dRowGatherDiscTypes in dTblGatherDiscTypes.Rows)
                        {
                            string sDiscType = Convert.ToString(dRowGatherDiscTypes["GatherDiscType"]).Trim();
                            bool bWeCodeCheck = Convert.ToBoolean(dRowGatherDiscTypes["WebCodeCheck"]);
                            bool bSittingBased = Convert.ToBoolean(dRowGatherDiscTypes["SittingBased"]);
                            bool bGather = Convert.ToBoolean(dRowGatherDiscTypes["Gather"]);
                            bool bInDev = Convert.ToBoolean(dRowGatherDiscTypes["InDevelopment"]);

                            if (bGather == true && bSittingBased == false && bInDev != true)
                            {
                                string sText = "[Checking for " + sDiscType + " work.]";
                                this.LogText(sText);

                                dtLabels.Clear();

                                DateTime DTNowMinus30 = DateTime.Now.AddDays(dSearchDays);
                                DateTime DTNowMinus30DateOnly = DTNowMinus30.Date;

                                string sDTNowDateOnly = DateTime.Now.Date.ToString("MM/dd/yy").Trim();
                                string sDTNowMinus30DateOnly = DTNowMinus30DateOnly.ToString("MM/dd/yy");

                                sCommText = "SELECT Lookupnum FROM ITEMS WHERE items.d_dueout > CTOD('" + sDTNowMinus30DateOnly + "') AND ITEMS.PACKAGETAG IN" +
                                    " (SELECT PACKAGETAG FROM LABELS WHERE LABELS.CODE = '" + sDiscType + "' AND LABELS.PACKAGETAG <> '    ') ORDER BY items.d_dueout ASC";

                                TM.CDSQuery(sCDSConnString, sCommText, dtLabels);

                                if (dtLabels.Rows.Count > 0)
                                {
                                    int iRowCount1 = dtLabels.Rows.Count;

                                    sText = "[Collected " + iRowCount1 + " records for processing.]";
                                    this.LogText(sText);

                                    this.ScanForTriggerPoints(dtLabels, sDiscType, bWeCodeCheck, bSittingBased);
                                }
                                else if (dtLabels.Rows.Count == 0)
                                {
                                    sText = "[No records collected this cycle.]";
                                    this.LogText(sText);
                                }
                            }
                        }
                    }
                    else if (dTblGatherDiscTypes.Rows.Count == 0)
                    {
                        string sStop = string.Empty;
                    }
                }
                else if (bSuccess != true)
                {
                    string sStop = string.Empty;
                }
            }
            catch (Exception ex)
            {
                TM.SaveExceptionToDB(ex);
            }
        }

        private void SearchCDSForSittingBasedReadyWork()
        {
            try
            {
                double dSearchDays = 0;
                DataTable dtLabels = new DataTable();

                string sLabel = "GatherDays";
                string sValue = "Value";
                string sVariable = string.Empty;
                bool bSuccess = true;

                TM.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                if (bSuccess == true)
                {
                    dSearchDays = Convert.ToDouble(sVariable);

                    DataTable dTblGatherDiscTypes = new DataTable("dTblGatherDiscTypes");
                    string sCommText = "SELECT * FROM [GatherDiscTypes] WHERE [Gather] = '1'";

                    TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblGatherDiscTypes);

                    if (dTblGatherDiscTypes.Rows.Count > 0)
                    {
                        foreach (DataRow dRowGatherDiscTypes in dTblGatherDiscTypes.Rows)
                        {
                            string sDiscType = Convert.ToString(dRowGatherDiscTypes["GatherDiscType"]).Trim();
                            bool bWeCodeCheck = Convert.ToBoolean(dRowGatherDiscTypes["WebCodeCheck"]);
                            bool bSittingBased = Convert.ToBoolean(dRowGatherDiscTypes["SittingBased"]);
                            bool bGather = Convert.ToBoolean(dRowGatherDiscTypes["Gather"]);
                            bool bInDev = Convert.ToBoolean(dRowGatherDiscTypes["InDevelopment"]);

                            if (bGather == true && bSittingBased == true && bInDev != true)
                            {
                                string sText = "[Checking for " + sDiscType + " work.]";
                                this.LogText(sText);

                                dtLabels.Clear();

                                DateTime DTNowMinus30 = DateTime.Now.AddDays(dSearchDays);
                                DateTime DTNowMinus30DateOnly = DTNowMinus30.Date;

                                string sDTNowDateOnly = DateTime.Now.Date.ToString("MM/dd/yy").Trim();
                                string sDTNowMinus30DateOnly = DTNowMinus30DateOnly.ToString("MM/dd/yy");

                                sCommText = "SELECT Lookupnum FROM ITEMS WHERE items.d_dueout > CTOD('" + sDTNowMinus30DateOnly + "') AND ITEMS.PACKAGETAG IN" +
                                    " (SELECT PACKAGETAG FROM LABELS WHERE LABELS.CODE = '" + sDiscType + "' AND LABELS.PACKAGETAG <> '    ') ORDER BY items.d_dueout ASC";

                                TM.CDSQuery(sCDSConnString, sCommText, dtLabels);

                                if (dtLabels.Rows.Count > 0)
                                {
                                    int iRowCount1 = dtLabels.Rows.Count;

                                    sText = "[Collected " + iRowCount1 + " records for processing.]";
                                    this.LogText(sText);

                                    this.CheckForALaCarteWork(sDiscType, dtLabels, sDTNowMinus30DateOnly, bWeCodeCheck, bSittingBased);
                                }
                                else if (dtLabels.Rows.Count == 0)
                                {
                                    sText = "[No records collected this cycle.]";
                                    this.LogText(sText);
                                }
                            }
                        }
                    }
                    else if (dTblGatherDiscTypes.Rows.Count == 0)
                    {
                        string sStop = string.Empty;
                    }
                }
                else if (bSuccess != true)
                {
                    string sStop = string.Empty;
                }
            }
            catch (Exception ex)
            {
                TM.SaveExceptionToDB(ex);
            }
        }

        private void CheckForALaCarteWork(string sDiscType, DataTable dtLabels, string sDTNowMinus30DateOnly, bool bWebCodeCheck, bool bSittingBased)
        {
            try
            {
                DataTable dTblPECFromPackages = new DataTable("dTblPECFromPackages");

                dTblPECFromPackages = dtLabels.Copy();

                DataTable dTblPECFromALaCarte = new DataTable("dTblPECFromALaCarte");
                string sCommText = "SELECT Lookupnum FROM ITEMS WHERE items.d_dueout > CTOD('" + sDTNowMinus30DateOnly + "') AND ITEMS.Lookupnum IN" +
                    " (SELECT Lookupnum FROM CODES WHERE Codes.CODE = '" + sDiscType + "') ORDER BY items.d_dueout ASC";

                TM.CDSQuery(sCDSConnString, sCommText, dTblPECFromALaCarte);

                if (dTblPECFromALaCarte.Rows.Count > 0)
                {
                    dtLabels.Clear();

                    dTblPECFromPackages.Merge(dTblPECFromALaCarte);

                    dtLabels = dTblPECFromPackages.Copy();

                    int iRowCount1 = dtLabels.Rows.Count;

                    string sText = "[Collected " + iRowCount1 + " records for processing.]";
                    this.LogText(sText);

                    this.ScanForTriggerPoints(dtLabels, sDiscType, bWebCodeCheck, bSittingBased);
                }
                else if (dTblPECFromALaCarte.Rows.Count == 0)
                {

                }
            }
            catch (Exception ex)
            {
                TM.SaveExceptionToDB(ex);
            }
        }

        // For each prod # in dtlabels query Stamps for trigger points (Action = Digi Print, PRNT_TRAV, Color QC)
        private void ScanForTriggerPoints(DataTable dtLabels, string sDiscType, bool bWebCodeCheck, bool bSittingBased)
        {
            try
            {
                DataTable dtScanned = new DataTable();
                int iCounter1 = 0;

                foreach (DataRow dr in dtLabels.Rows)
                {
                    iCounter1 += 1;

                    string sProdNum = Convert.ToString(dr["lookupnum"]).Trim();

                    string sCommText = "SELECT DISTINCT Lookupnum FROM STAMPS WHERE Lookupnum = '" + sProdNum +
                             "' AND (Action = 'DIGI PRINT' OR (Action = 'PRNT_TRAV' AND Stationid != 'RECEIVING'))";

                    DataTable dt = new DataTable();

                    TM.CDSQuery(sCDSConnString, sCommText, dt);

                    if (dt.Rows.Count > 0)
                    {
                        dtScanned.Merge(dt);
                    }
                }

                if (dtScanned.Rows.Count > 0)
                {
                    int iRowCount1 = dtScanned.Rows.Count;

                    string sText = "[Records with required trigger points this cycle: " + iRowCount1 + "]";
                    this.LogText(sText);

                    int iCounter2 = 0;

                    DataTable dtItemInfo = new DataTable();

                    foreach (DataRow dr in dtScanned.Rows)
                    {
                        iCounter2 += 1;

                        string sProdNum = Convert.ToString(dr["lookupnum"]).Trim();

                        string sCommText = "SELECT DISTINCT lookupnum, order, packagetag FROM ITEMS WHERE lookupnum = '" + sProdNum + "'";
                        DataTable dTbl = new DataTable();

                        TM.CDSQuery(sCDSConnString, sCommText, dTbl);

                        if (dTbl.Rows.Count > 0)
                        {
                            dtItemInfo.Merge(dTbl);
                        }
                        else if (dTbl.Rows.Count == 0)
                        {
                            string sStop = string.Empty;
                        }
                    }

                    if (dtItemInfo.Rows.Count > 0)
                    {
                        int iRowCount2 = dtItemInfo.Rows.Count;

                        sText = "[Records to check existence in DP2: " + iRowCount2 + "]";
                        this.LogText(sText);

                        this.ExistsInDP2(dtItemInfo, sDiscType, bWebCodeCheck, bSittingBased);
                    }
                    else if (dtItemInfo.Rows.Count == 0)
                    {
                        string sStop = string.Empty;
                    }
                }
            }
            catch(Exception ex)
            {
                TM.SaveExceptionToDB(ex);
            }
        }

        // Query each refnum from dtItemInfo against DP2-Orders table to see if it exists.
        // If it exists in DP2-Orders then check DP2-Images for frames.
        private void ExistsInDP2(DataTable dtItemInfo, string sDiscType, bool bWebCodeCheck, bool bSittingBased)
        {
            try
            {
                DataTable dtFrames = new DataTable();
                DataTable dTblSittingBasedWork = new DataTable("dTblSittingBasedWork");
                DataRow dRowSittingBasedWork;
                dTblSittingBasedWork.Columns.Add("ProdNum", typeof(string));
                dTblSittingBasedWork.Columns.Add("Sequence", typeof(string));
                dTblSittingBasedWork.Columns.Add("Sitting", typeof(string));
                dTblSittingBasedWork.Columns.Add("Quantity", typeof(string));
                string sDiscQuantity = string.Empty;
                bool bGotQuantity = false;
                string sCode = string.Empty;

                foreach (DataRow dr in dtItemInfo.Rows)
                {
                    bGotQuantity = false;
                    dTblSittingBasedWork.Clear();

                    string sRefNum = Convert.ToString(dr["Order"]).Trim();
                    string sProdNum = Convert.ToString(dr["Lookupnum"]).Trim();
                    string sPkgTag = Convert.ToString(dr["Packagetag"]).Trim();

                    string sCommText = "SELECT [ID] FROM [Orders] WHERE [ID] = '" + sRefNum + "'";
                    DataTable dt = new DataTable();
                    dt.Clear();

                    TM.SQLQuery(sDP2ConnString, sCommText, dt);

                    if (dt.Rows.Count > 0)
                    {
                        // Verify we have data in the Images table.

                        sCommText = "SELECT * FROM [Images] WHERE [OrderID] = '" + sRefNum + "'";
                        DataTable dTbl = new DataTable();
                        dTbl.Clear();

                        TM.SQLQuery(sDP2ConnString, sCommText, dTbl);

                        if (dTbl.Rows.Count > 0)
                        {
                            // This Clark query equals:
                            // Query codes for frames with "ICD" in packages or ala carte here
                            // Query labels packages codes that contain icd for packagetag supplied
                            // Query codes that contain said package codes or ala carte

                            if (bSittingBased == false)
                            {
                                sCommText = "SELECT * FROM Codes WHERE Lookupnum = '" + sProdNum + "' AND ((Codes.Code = '" + sDiscType + "' AND Package = .F.) OR (Package = .T. AND"
                                    + " Code IN (SELECT DISTINCT Packagecod FROM Labels WHERE Labels.packagetag = '" + sPkgTag + "' AND Labels.Code = '" + sDiscType + "'))) ORDER BY Sequence ASC";

                                DataTable dTbl2 = new DataTable();

                                TM.CDSQuery(sCDSConnString, sCommText, dTbl2);

                                if (dTbl2.Rows.Count > 0)
                                {
                                    dtFrames.Merge(dTbl2);
                                }
                                else if (dTbl2.Rows.Count == 0)
                                {
                                    string sStop = string.Empty;
                                }
                            }
                            else if (bSittingBased != false)
                            {
                                DataTable dTblHavePEC = new DataTable("dTblHavePEC");
                                sCommText = "SELECT DISTINCT Code From Codes WHERE Lookupnum = '" + sProdNum + "'";
                                dTblHavePEC.Clear();

                                TM.CDSQuery(sCDSConnString, sCommText, dTblHavePEC);

                                if (dTblHavePEC.Rows.Count > 0)
                                {
                                    foreach (DataRow dRowHavePEC in dTblHavePEC.Rows)
                                    {
                                        sCode = Convert.ToString(dRowHavePEC["Code"]).Trim();

                                        if (sCode == "PEC")
                                        {
                                            DataTable dTblFrames = new DataTable("dTblFrames");
                                            sCommText = "SELECT DISTINCT Sitting, Lookupnum FROM Frames WHERE Lookupnum = '" + sProdNum + "' ORDER BY Sitting DESC";
                                            dTblFrames.Clear();

                                            TM.CDSQuery(sCDSConnString, sCommText, dTblFrames);

                                            if (dTblFrames.Rows.Count > 0)
                                            {
                                                DataTable dTblDiscOrders = new DataTable("dTblDiscOrders");
                                                sCommText = "SELECT [ProdNum], [Sitting] FROM [DiscOrders] WHERE [ProdNum] = '" + sProdNum + "' ORDER BY Sitting ASC";

                                                TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblDiscOrders);

                                                if (dTblDiscOrders.Rows.Count > 0)
                                                {
                                                    foreach (DataRow dRowDiscOrders in dTblDiscOrders.Rows)
                                                    {
                                                        string sDOProdNum = Convert.ToString(dRowDiscOrders["ProdNum"]).Trim();
                                                        string sDOSitting = Convert.ToString(dRowDiscOrders["Sitting"]);

                                                        for (int i = dTblFrames.Rows.Count - 1; i >= 0; i--)
                                                        {
                                                            DataRow dRowFrames = dTblFrames.Rows[i];

                                                            string sFramesProdNum = Convert.ToString(dRowFrames["lookupnum"]).Trim();
                                                            string sFramesSitting = Convert.ToString(dRowFrames["Sitting"]);

                                                            if (sFramesProdNum == sDOProdNum && sFramesSitting == sDOSitting)
                                                            {
                                                                dRowFrames.Delete();
                                                                dTblFrames.AcceptChanges();
                                                            }
                                                        }
                                                    }
                                                }
                                                else if (dTblDiscOrders.Rows.Count == 0)
                                                {

                                                }

                                                foreach(DataRow dRowFrames in dTblFrames.Rows)
                                                {
                                                    string sSitting = Convert.ToString(dRowFrames["Sitting"]);

                                                    DataTable dTblUniqueSittings = new DataTable("dTblUniqueSittings");
                                                    sCommText = "SELECT Lookupnum, Sequence, Sitting From Frames WHERE Lookupnum = '" + sProdNum + "' AND Sitting = '" + sSitting + "' ORDER BY Sequence ASC";
                                                    dTblUniqueSittings.Clear();

                                                    TM.CDSQuery(sCDSConnString, sCommText, dTblUniqueSittings);

                                                    if (dTblUniqueSittings.Rows.Count > 0)
                                                    {
                                                        dTblSittingBasedWork.Clear();

                                                        string sUniqueSequence = Convert.ToString(dTblUniqueSittings.Rows[0]["Sequence"]).Trim();
                                                        string sUniqueProdNum = Convert.ToString(dTblUniqueSittings.Rows[0]["Lookupnum"]).Trim();
                                                        string sUniqueSitting = Convert.ToString(dTblUniqueSittings.Rows[0]["Sitting"]);

                                                        DataTable dTblQuantity = new DataTable("dTblQuantity");
                                                        sCommText = "SELECT Lookupnum, Quantity FROM Codes WHERE Lookupnum = '" + sUniqueProdNum + "' AND Sequence = " +
                                                            sUniqueSequence + " AND Code = '" + sDiscType + "'";
                                                        dTblQuantity.Clear();

                                                        TM.CDSQuery(sCDSConnString, sCommText, dTblQuantity);

                                                        if (dTblQuantity.Rows.Count > 0)
                                                        {
                                                            sDiscQuantity = Convert.ToString(dTblQuantity.Rows[0]["Quantity"]).Trim();
                                                            bGotQuantity = true;
                                                        }
                                                        else if (dTblQuantity.Rows.Count == 0)
                                                        {
                                                            string sStop = string.Empty;
                                                            bGotQuantity = false;
                                                        }

                                                        if (bGotQuantity == true)
                                                        {
                                                            dRowSittingBasedWork = dTblSittingBasedWork.NewRow();
                                                            dRowSittingBasedWork["ProdNum"] = sUniqueProdNum;
                                                            dRowSittingBasedWork["Sequence"] = sUniqueSequence;
                                                            dRowSittingBasedWork["Sitting"] = sUniqueSitting;
                                                            dRowSittingBasedWork["Quantity"] = sDiscQuantity;
                                                            dTblSittingBasedWork.Rows.Add(dRowSittingBasedWork);
                                                            dTblSittingBasedWork.AcceptChanges();
                                                            dtFrames.Merge(dTblSittingBasedWork);
                                                            dtFrames.AcceptChanges();
                                                        }
                                                        else if (bGotQuantity != true)
                                                        {
                                                            string sStop = string.Empty;
                                                            // Continue.
                                                        }
                                                    }
                                                    else if (dTblUniqueSittings.Rows.Count == 0)
                                                    {
                                                        string sStop = string.Empty;
                                                    }
                                                }
                                            }
                                            else if (dTblFrames.Rows.Count == 0)
                                            {
                                                string sStop = string.Empty;
                                            }
                                        }
                                        else if (sCode != "PEC")
                                        {
                                            string sStop = string.Empty;
                                            // Continue.
                                        }
                                    }
                                }
                                else if (dTblHavePEC.Rows.Count == 0)
                                {
                                    string sStop = string.Empty;
                                }
                            }
                        }
                        else if (dTbl.Rows.Count == 0)
                        {
                            // Continue.
                        }
                    }
                }           

                string sStop2 = string.Empty;

                this.FramesToDB(dtFrames, sDiscType, bWebCodeCheck, bSittingBased);

            }
            catch(Exception ex)
            {
                TM.SaveExceptionToDB(ex);
            }
        }

        // Insert previously gathered job info the DiscOrders table.
        private void FramesToDB(DataTable dtFrames, string sDiscType, bool bWebCodeCheck, bool bSittingBased)
        {
            try
            {
                iStartRowCount = dtFrames.Rows.Count;

                int iCounter = 0;

                string sCommText = string.Empty;
                string sOrderType = string.Empty;
                string sServiceType = string.Empty;
                string sReceived = string.Empty;
                string sImageLocation = string.Empty;
                DateTime dTm = new DateTime();
                string sSitting = string.Empty;
                string sOriginalDiscType = sDiscType;
                string sProdNum = string.Empty;
                string sFramesRefNum = string.Empty;
                string sFrameNum = string.Empty;
                string sDiscQuantity = string.Empty;
                string sCDSFrameNum = string.Empty;                

                string sCT = "INSERT INTO [DiscOrders] ([ProdNum], [RefNum], [FrameNum], [Status], [LastCheck], [CustNum], [Packagetag]," +
                    " [Quantity], [DiscType], [OrderType], [ServiceType], [Received], [ImageLocation], [RecordCollectedDate], [UniqueID], [Sitting], [ResubmitCount]) VALUES (@A, @B, @C, '10', @D, @E, @F, @G, @I, @J, @K, @L, @M, @N, @O, @P, @Q)";

                if (dtFrames.Rows.Count > 0)
                {
                    if (bSittingBased == false)
                    {
                        #region For loop to remove existing records

                        string sDOProdNum = string.Empty;
                        string sDOFrameNum = string.Empty;
                        string sDODiscType = string.Empty;
                        string sDOSitting = string.Empty;

                        DataTable dTDiscOrders = new DataTable("dTDiscOrders");
                        sCommText = "SELECT [ProdNum], [FrameNum], [DiscType], [Sitting] FROM [DiscOrders]";

                        TM.SQLQuery(sDiscProcessorConnString, sCommText, dTDiscOrders);

                        if (dTDiscOrders.Rows.Count > 0)
                        {
                            foreach (DataRow dRowDiscOrders in dTDiscOrders.Rows)
                            {
                                sDOProdNum = Convert.ToString(dRowDiscOrders["ProdNum"]).Trim();
                                sDOFrameNum = Convert.ToString(dRowDiscOrders["FrameNum"]).Trim();
                                sDOFrameNum = sDOFrameNum.TrimStart('0');
                                sDODiscType = Convert.ToString(dRowDiscOrders["DiscType"]).Trim();
                                sDOSitting = Convert.ToString(dRowDiscOrders["Sitting"]).Trim();

                                for (int i = dtFrames.Rows.Count - 1; i >= 0; i--)
                                {
                                    DataRow dRowFrames = dtFrames.Rows[i];

                                    string sFramesProdNum = Convert.ToString(dRowFrames["lookupnum"]).Trim();
                                    string sFramesFramenum = Convert.ToString(dRowFrames["sequence"]).Trim();

                                    if (sFramesProdNum == sDOProdNum && sFramesFramenum == sDOFrameNum)
                                    {
                                        dRowFrames.Delete();
                                        dtFrames.AcceptChanges();
                                    }
                                }
                            }
                        }
                        else if (dTDiscOrders.Rows.Count == 0)
                        {
                            string sStop = string.Empty;
                        }

                        #endregion
                    }

                    iEndRowCount = dtFrames.Rows.Count;


                    using(SqlConnection myConn = new SqlConnection(sDiscProcessorConnString))
                    {
                        myConn.Open();

                        foreach (DataRow dr in dtFrames.Rows)
                        {
                            bool bGotQuantity = false;

                            sDiscType = sOriginalDiscType;

                            iCounter += 1;

                            try
                            {
                                if (bSittingBased != true)
                                {
                                    sProdNum = Convert.ToString(dr["lookupnum"]).Trim();
                                    sFrameNum = Convert.ToString(dr["sequence"]).Trim();
                                    sCDSFrameNum = sFrameNum;

                                    if (bWebCodeCheck == true)
                                    {
                                        TM.CheckForWebCodes(sProdNum, ref sDiscType);
                                    }                                    

                                    DataTable dTblGetSitting = new DataTable("dTblGetSitting");
                                    sCommText = "SELECT Sitting FROM Frames WHERE Lookupnum = '" + sProdNum + "' AND Sequence = " + sFrameNum;

                                    TM.CDSQuery(sCDSConnString, sCommText, dTblGetSitting);

                                    if (dTblGetSitting.Rows.Count > 0)
                                    {
                                        sSitting = Convert.ToString(dTblGetSitting.Rows[0]["Sitting"]).Trim();
                                    }
                                    else if (dTblGetSitting.Rows.Count == 0)
                                    {
                                        string sStop = string.Empty;
                                    }

                                    sDiscQuantity = Convert.ToString(dr["quantity"]).Trim();

                                    if (sDiscQuantity != string.Empty)
                                    {
                                        bGotQuantity = true;
                                    }
                                    else if (sDiscQuantity == string.Empty)
                                    {
                                        bGotQuantity = false;
                                    }
                                }
                                else if (bSittingBased == true)
                                {
                                    sProdNum = Convert.ToString(dr["ProdNum"]).Trim();
                                    sFrameNum = Convert.ToString(dr["Sequence"]).Trim();
                                    sSitting = Convert.ToString(dr["Sitting"]);
                                    sDiscQuantity = Convert.ToString(dr["Quantity"]).Trim();
                                    sCDSFrameNum = sFrameNum;

                                    if (bWebCodeCheck == true)
                                    {
                                        TM.CheckForWebCodes(sProdNum, ref sDiscType);
                                    }                                    

                                    if (sDiscQuantity != string.Empty)
                                    {
                                        bGotQuantity = true;
                                    }
                                    else if (sDiscQuantity == string.Empty)
                                    {
                                        bGotQuantity = false;
                                    }
                                }

                                if (bGotQuantity == true)
                                {
                                    sFrameNum = sFrameNum.PadLeft(4, '0');

                                    DataTable dt = new DataTable();
                                    sCommText = "SELECT * FROM ITEMS WHERE Lookupnum = '" + sProdNum + "'";

                                    TM.CDSQuery(sCDSConnString, sCommText, dt);

                                    if (dt.Rows.Count > 0)
                                    {
                                        sFramesRefNum = Convert.ToString(dt.Rows[0]["order"]).Trim();
                                        string sCustNum = Convert.ToString(dt.Rows[0]["customer"]).Trim();
                                        string sPackagetag = Convert.ToString(dt.Rows[0]["packagetag"]).Trim();

                                        sOrderType = Convert.ToString(dt.Rows[0]["Batch"]).Trim();
                                        sServiceType = Convert.ToString(dt.Rows[0]["Sertype"]).Trim();
                                        dTm = Convert.ToDateTime(dt.Rows[0]["D_dateent"]);
                                        sReceived = dTm.ToString("M/dd/yy").Trim();

                                        // Get image name and path.
                                        DataTable dTblImage = new DataTable("dTblImage");
                                        sCommText = "SELECT Path FROM dp2image WHERE lookupnum = '" + sProdNum + "' AND Frame = " + sCDSFrameNum;

                                        TM.CDSQuery(sCDSConnString, sCommText, dTblImage);

                                        if (dTblImage.Rows.Count > 0)
                                        {
                                            sImageLocation = Convert.ToString(dTblImage.Rows[0]["Path"]).Trim();

                                            SqlCommand myCommand = myConn.CreateCommand();
                                            myCommand.CommandText = sCT;
                                            myCommand.Parameters.AddWithValue("@A", sProdNum);
                                            myCommand.Parameters.AddWithValue("@B", sFramesRefNum);
                                            myCommand.Parameters.AddWithValue("@D", DateTime.Now.ToString("MM/dd/yy H:mm:ss").Trim());
                                            myCommand.Parameters.AddWithValue("@E", sCustNum);
                                            myCommand.Parameters.AddWithValue("@F", sPackagetag);
                                            myCommand.Parameters.AddWithValue("@G", sDiscQuantity);
                                            myCommand.Parameters.AddWithValue("@I", sDiscType);
                                            myCommand.Parameters.AddWithValue("@J", sOrderType);
                                            myCommand.Parameters.AddWithValue("@K", sServiceType);
                                            myCommand.Parameters.AddWithValue("@L", sReceived);
                                            myCommand.Parameters.AddWithValue("@M", sImageLocation);
                                            myCommand.Parameters.AddWithValue("@N", DateTime.Now.ToString("MM/dd/yy H:mm:ss").Trim());

                                            if (bSittingBased != true)
                                            {
                                                myCommand.Parameters.AddWithValue("@C", sFrameNum);
                                                myCommand.Parameters.AddWithValue("@O", sProdNum + sFrameNum + sDiscType);
                                                myCommand.Parameters.AddWithValue("@P", sSitting);
                                                myCommand.Parameters.AddWithValue("@Q", 0);
                                                myCommand.ExecuteNonQuery();

                                                string sText = "[Saved record #: " + iCounter + "][Added reference #: " + sFramesRefNum + " and frame #: " + sFrameNum + " to the database for disc production.]";
                                                this.LogText(sText);

                                                // Insert record into CDS DiscOrders (for resubmits through APS_LAB)
                                                sCommText = "INSERT INTO DiscOrders (Cust_ref, Lookupnum, Sequence, Sitting, DiscType) VALUES ('" + sFramesRefNum + "', '" + sProdNum + "', " +
                                                    sCDSFrameNum + ", '" + sSitting + "', '" + sDiscType + "')";
                                                bool bSuccess = false;

                                                TM.CDSNonQuery(sCDSConnString, sCommText, ref bSuccess);

                                                if (bSuccess == true)
                                                {
                                                    // Continue.
                                                }
                                                else if (bSuccess != true)
                                                {
                                                    string sStop = string.Empty;
                                                }

                                                bool bStamped = false;
                                                string sAction = "DPRecSaved";

                                                TM.StampIt(sProdNum, sAction, ref bStamped);

                                                if (bStamped == true)
                                                {

                                                }
                                                else if (bStamped != true)
                                                {
                                                    
                                                }
                                            }
                                            else if (bSittingBased == true)
                                            {
                                                myCommand.Parameters.AddWithValue("@C", sFrameNum);
                                                myCommand.Parameters.AddWithValue("@O", sProdNum + sSitting.Trim() + sDiscType);
                                                myCommand.Parameters.AddWithValue("@P", sSitting);
                                                myCommand.Parameters.AddWithValue("@Q", 0);
                                                myCommand.ExecuteNonQuery();

                                                string sText = "[Saved record #: " + iCounter + "][Added reference #: " + sFramesRefNum + " and sitting #: " + sSitting.Trim() + " to the database for disc production.]";
                                                this.LogText(sText);

                                                // Insert record into CDS DiscOrders (for resubmits through APS_LAB)
                                                sCommText = "INSERT INTO DiscOrders (Cust_ref, Lookupnum, Sequence, Sitting, DiscType) VALUES ('" + sFramesRefNum + "', '" + sProdNum + "', " +
                                                    sCDSFrameNum + ", '" + sSitting + "', '" + sDiscType + "')";
                                                bool bSuccess = false;

                                                TM.CDSNonQuery(sCDSConnString, sCommText, ref bSuccess);

                                                if (bSuccess == true)
                                                {
                                                    // Continue.
                                                }
                                                else if (bSuccess != true)
                                                {
                                                    string sStop = string.Empty;
                                                }

                                                bool bStamped = false;
                                                string sAction = "DPRecSaved";

                                                TM.StampIt(sProdNum, sAction, ref bStamped);

                                                if (bStamped == true)
                                                {

                                                }
                                                else if (bStamped != true)
                                                {

                                                }
                                            }
                                        }
                                        else if (dTblImage.Rows.Count == 0)
                                        {
                                            string sStop = string.Empty;
                                        }
                                    }
                                    else if (dt.Rows.Count == 0)
                                    {
                                        string sStop = string.Empty;
                                        return;
                                    }
                                }
                                else if (bGotQuantity != true)
                                {
                                    string sStop = string.Empty;
                                    // Continue.
                                }
                            }
                            catch(System.Data.SqlClient.SqlException)
                            {
                                if (bSittingBased != true)
                                {
                                    string sText = "[Reference #: " + sFramesRefNum + " and frame #: " + sFrameNum + " already exist in database.]";
                                    this.LogText(sText);
                                }
                                else if (bSittingBased == true)
                                {
                                    string sText = "[Reference #: " + sFramesRefNum + " and sitting #: " + sSitting.Trim() + " already exist in database.]";
                                    this.LogText(sText);
                                }

                                iCounter -= 1;
                            }
                        }

                        string sText1 = "[Records saved for disc production this cycle: " + iCounter + "]";
                        this.LogText(sText1);
                    }
                }
                else if (dtFrames.Rows.Count == 0)
                {
                    string sText = "[No records to save to the database this cycle.]";
                    this.LogText(sText);

                    return;
                }
            }
            catch(Exception ex)
            {
                TM.SaveExceptionToDB(ex);
            }
        }

        #endregion

        #region Processing records in the DiscOrders table.

        // Query distinct production number from the DiscOrders table for gathering of associated frames in the QueryDiscOrdersForAllFrames method.
        private void QueryDiscOrdersForDistinctProdNum()
        {
            try
            {
                bool bInitialPass02 = true;
                bool bInitialPassPEC01 = true;

                DataTable dTblDiscOrders = new DataTable("dTblDiscOrders");
                string sCommText = "SELECT DISTINCT [ProdNum], [DiscType] FROM [DiscOrders] WHERE [Status] = '10'"; // Gather all production numbers ready for processing.

                TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblDiscOrders);

                if (dTblDiscOrders.Rows.Count > 0)
                {
                    string sText = "[Gathering all ready work for processing.]";
                    this.LogText(sText);

                    foreach (DataRow dRowDiscOrders in dTblDiscOrders.Rows)
                    {
                        string sPNum = Convert.ToString(dRowDiscOrders["ProdNum"]).Trim();
                        string sDiscType = Convert.ToString(dRowDiscOrders["DiscType"]).Trim();

                        bool bSittingBased = false;
                        bool bSuccess = false;

                        TM.IsDiscTypeSittingBased(sDiscType, ref bSittingBased, ref bSuccess);

                        if (bSuccess == true)
                        {
                            if (bSittingBased != true)
                            {
                                if (bDoAll == true || bGatherAndProcessFrameBasedWorkOnly == true)
                                {
                                    this.QueryDiscOrdersForAllFrames(sPNum, ref bInitialPass02, sDiscType, bSittingBased);
                                }
                            }
                            else if (bSittingBased == true)
                            {
                                if (bDoAll == true || bGatherAndProcessSittingBasedWorkOnly == true)
                                {
                                    bool bCreated = false;
                                    DataTable dTblOrder = new DataTable("dTblOrder");
                                    this.PECGatherRenderInfo(sPNum, sDiscType, ref bInitialPassPEC01, ref bCreated, dTblOrder, bSittingBased);
                                }
                            }
                        }
                        else if (bSuccess != true)
                        {

                        }                       
                    }
                }
                else if (dTblDiscOrders.Rows.Count == 0)
                {
                    string sStop = string.Empty;
                }
            }
            catch(Exception ex)
            {
                TM.SaveExceptionToDB(ex);
            }
        }

        #region ICD/ICDW and MEG/MEGW data collection methods for the FrameData table.

        // Query DiscOrders for all frames associated with each distinct production number gathered in the QueryDiscOrdersForDistinctProdNum method. (ICD/ICDW or MEG/MEGW disctypes)
        private void QueryDiscOrdersForAllFrames(string sPNum, ref bool bInitialPass02, string sDiscType, bool bSittingBased)
        {
            try
            {
                DataTable dTblStyles = new DataTable("Styles");
                DataTable dTblGSBackgrounds = new DataTable("dTblGSBackgrounds");
                DataTable dTblStylesAndGSBackgrounds = new DataTable("dTblStylesAndGSBackgrounds");

                dTblStylesAndGSBackgrounds.Columns.Add("Style", typeof(string));
                dTblStylesAndGSBackgrounds.Columns.Add("GSBkGrnd", typeof(string));
                dTblStylesAndGSBackgrounds.Columns.Add("Itype", typeof(string));
                dTblStylesAndGSBackgrounds.Columns.Add("Otype", typeof(string));
                dTblStylesAndGSBackgrounds.Columns.Add("Alt_data", typeof(string));
                dTblStylesAndGSBackgrounds.Columns.Add("Alt_type", typeof(string));
                dTblStylesAndGSBackgrounds.Columns.Add("MultiRenderGS", typeof(bool));

                string sStyle = string.Empty;
                string sGreenscreenBackground = string.Empty;
                string sItype = string.Empty;
                string sOtype = string.Empty;
                string sAltData = string.Empty;
                string sAltType = string.Empty;

                bool bHaveAltData = false;
                bool bMultiRenderGS = false;

                string sProdNum = string.Empty;
                string sFrameNum = string.Empty;
                int iIDsNeeded = 0;

                bool bInitialPass = true;
                bool bInitialPass01 = true;

                string sGSBG = string.Empty;
                string sBG = string.Empty;

                if (bInitialPass02 == true)
                {
                    string sText = "[Processing all ready work.]";
                    this.LogText(sText);

                    bInitialPass02 = false;
                }

                DataTable dt = new DataTable();
                string sCommText = "SELECT * FROM [DiscOrders] WHERE [ProdNum] = '" + sPNum + "' AND [DiscType] = '" + sDiscType + "' AND [Status] = '10'"; // Gather all frame data associated with a single production number.

                TM.SQLQuery(sDiscProcessorConnString, sCommText, dt);

                if (dt.Rows.Count > 0)
                {
                    int iUnique = 0;

                    foreach (DataRow dr in dt.Rows)
                    {
                        bInitialPass = true;
                        bInitialPass01 = true;
                        dTblStyles.Clear();
                        dTblGSBackgrounds.Clear();
                        dTblStylesAndGSBackgrounds.Clear();
                        bHaveAltData = false;
                        bMultiRenderGS = false;

                        string sCustNum = Convert.ToString(dr["CustNum"]).Trim();
                        string sPkgTag = Convert.ToString(dr["Packagetag"]).Trim();
                        sProdNum = Convert.ToString(dr["ProdNum"]).Trim();
                        sFrameNum = Convert.ToString(dr["FrameNum"]).Trim();
                        string sRefNum = Convert.ToString(dr["RefNum"]).Trim();
                        string sDiscQuantity = Convert.ToString(dr["Quantity"]).Trim();
                        string sSitting = Convert.ToString(dr["Sitting"]).Trim();
                        string sPath = Convert.ToString(dr["ImageLocation"]).Trim();

                        sCommText = "SELECT * FROM Custtrans WHERE (Itype = 'S' AND Otype = 'S') AND Customer = '" // this query will get styles or styles with 3d bits
                        + sCustNum + "' AND Packagetag = '" + sPkgTag + "' ORDER BY Alt_data ASC";

                        TM.CDSQuery(sCDSConnString, sCommText, dTblStyles);

                        sCommText = "SELECT * FROM Custtrans WHERE (Itype = 'B' AND Otype = 'B') AND Customer = '" // this query will get all gs bkgrnds
                        + sCustNum + "' AND Packagetag = '" + sPkgTag + "'";

                        TM.CDSQuery(sCDSConnString, sCommText, dTblGSBackgrounds);

                        if (dTblStyles.Rows.Count > 0)
                        {
                            if (dTblGSBackgrounds.Rows.Count > 0)
                            {
                                // I have styles and gs bkgrnds.

                                bMultiRenderGS = false;

                                foreach (DataRow dRowStyles in dTblStyles.Rows)
                                {
                                    sStyle = Convert.ToString(dRowStyles["Labdata"]).Trim();
                                    sItype = Convert.ToString(dRowStyles["Itype"]).Trim();
                                    sOtype = Convert.ToString(dRowStyles["Otype"]).Trim();
                                    sAltData = Convert.ToString(dRowStyles["Alt_data"]).Trim();
                                    sAltType = Convert.ToString(dRowStyles["Alt_type"]).Trim();

                                    foreach (DataRow dRowGSBackgrounds in dTblGSBackgrounds.Rows)
                                    {
                                        sGSBG = Convert.ToString(dRowGSBackgrounds["Labdata"]).Trim();

                                        dTblStylesAndGSBackgrounds.Rows.Add(sStyle, sGSBG, sItype, sOtype, sAltData, sAltType, bMultiRenderGS);
                                    }
                                }                                
                            }
                            else if (dTblGSBackgrounds.Rows.Count == 0)
                            {
                                // I have styles but no individual gs bkgrnds. Can still have gs bkgrnds associated with styles.

                                foreach (DataRow dRowStyles in dTblStyles.Rows)
                                {
                                    sAltData = Convert.ToString(dRowStyles["Alt_data"]).Trim();
                                    sAltType = Convert.ToString(dRowStyles["Alt_type"]).Trim();

                                    if (sAltData != string.Empty || sAltData != "")
                                    {
                                        if (sAltType == "B")
                                        {
                                            bHaveAltData = true;                                           
                                        }
                                    }                                   
                                }

                                foreach (DataRow dRowStyles2 in dTblStyles.Rows)
                                {
                                    sStyle = Convert.ToString(dRowStyles2["Labdata"]).Trim();
                                    sItype = Convert.ToString(dRowStyles2["Itype"]).Trim();
                                    sOtype = Convert.ToString(dRowStyles2["Otype"]).Trim();
                                    sAltData = Convert.ToString(dRowStyles2["Alt_data"]).Trim();
                                    sAltType = Convert.ToString(dRowStyles2["Alt_type"]).Trim();
                                    sGSBG = Convert.ToString(dRowStyles2["Alt_data"]).Trim();

                                    bMultiRenderGS = bHaveAltData;

                                    dTblStylesAndGSBackgrounds.Rows.Add(sStyle, sGSBG, sItype, sOtype, sAltData, sAltType, bMultiRenderGS);
                                }
                            }
                        }
                        else if (dTblStyles.Rows.Count == 0)
                        {
                            if (dTblGSBackgrounds.Rows.Count > 0)
                            {
                                // I have gs bkgrnds but no styles.

                                bMultiRenderGS = false;

                                foreach (DataRow dRowGSBackgrounds in dTblGSBackgrounds.Rows)
                                {
                                    sGSBG = Convert.ToString(dRowGSBackgrounds["Labdata"]).Trim();
                                    sItype = Convert.ToString(dRowGSBackgrounds["Itype"]).Trim();
                                    sOtype = Convert.ToString(dRowGSBackgrounds["Otype"]).Trim();
                                    sAltData = Convert.ToString(dRowGSBackgrounds["Alt_data"]).Trim();
                                    sAltType = Convert.ToString(dRowGSBackgrounds["Alt_type"]).Trim();
                                    sStyle = "NY";

                                    dTblStylesAndGSBackgrounds.Rows.Add(sStyle, sGSBG, sItype, sOtype, sAltData, sAltType, bMultiRenderGS);
                                }                                
                            }
                            else if (dTblGSBackgrounds.Rows.Count == 0)
                            {
                                // I have no records from either query.
                                string sStop = string.Empty;
                            }
                        }

                        int iRowCount = dTblStylesAndGSBackgrounds.Rows.Count;

                        if (dTblStylesAndGSBackgrounds.Rows.Count > 0)
                        {
                            TM.StylesAndBackgroundCount(ref iIDsNeeded, bMultiRenderGS, dTblStyles, dTblGSBackgrounds, sDiscType);

                            sCommText = "UPDATE [DiscOrders] Set [JobIDsNeeded] = '" + iIDsNeeded + "' WHERE [ProdNum] = '" + sProdNum + "' AND [Sitting] = '" + sSitting + "'";
                            bool bSuccess = false;

                            TM.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                            if (bSuccess == true)
                            {
                                foreach (DataRow dRowStylesAndGSBackgrounds in dTblStylesAndGSBackgrounds.Rows)
                                {
                                    sBG = Convert.ToString(dRowStylesAndGSBackgrounds["Style"]).Trim(); //sBG = Style
                                    sGSBG = Convert.ToString(dRowStylesAndGSBackgrounds["GSBkGrnd"]).Trim(); //sGSBG = GS Background

                                    if (sGSBG == string.Empty)
                                    {
                                        sGSBG = "NONE";
                                    }

                                    this.GatherFrameData(sProdNum, sFrameNum, sRefNum, sCustNum, sPkgTag, sPath, sDiscQuantity, sBG, ref iUnique, ref bInitialPass01, sGSBG, sDiscType, sSitting, bMultiRenderGS);
                                }
                            }
                            else if (bSuccess != true)
                            {
                                string sStop = string.Empty;
                            }
                        }
                        else if (dTblStylesAndGSBackgrounds.Rows.Count == 0)
                        {
                            sBG = "NY";
                            sGSBG = "NONE";
                            bMultiRenderGS = false;

                            iIDsNeeded = 6;

                            sCommText = "UPDATE [DiscOrders] Set [JobIDsNeeded] = '" + iIDsNeeded + "' WHERE [ProdNum] = '" + sProdNum + "' AND [Sitting] = '" + sSitting + "'";
                            bool bSuccess = false;

                            TM.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                            if (bSuccess == true)
                            {
                                this.GatherFrameData(sProdNum, sFrameNum, sRefNum, sCustNum, sPkgTag, sPath, sDiscQuantity, sBG, ref iUnique, ref bInitialPass01, sGSBG, sDiscType, sSitting, bMultiRenderGS);
                            }
                            else if (bSuccess != true)
                            {
                                string sStop = string.Empty;
                            }                            
                        }

                        // Update DiscOrders.Status = 20 (Frame data processed)
                        string sStatus = "20";

                        TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                    }
                }
                else if (dt.Rows.Count == 0)
                {
                    string sStop = string.Empty;
                }
            }
            catch(Exception ex)
            {
                TM.SaveExceptionToDB(ex);
            }
        }

        private void GetImageNameAndPath(string sProdNum, string sFrameNum, string sRefNum, string sCustNum, string sPkgTag, string sDiscQuantity, string sBG, ref int iUnique, ref bool bInitialPass, ref bool bInitialPass01, string sGSBG, string sDiscType, string sSitting, bool bMultiRenderGS)
        {
            try
            {
                if (bInitialPass == true)
                {
                    string sText = "[Gathering image name and path for reference number " + sRefNum + " and frame number " + sFrameNum + ".]";
                    this.LogText(sText);

                    bInitialPass = false;
                }

                DataTable dt = new DataTable();
                string sCommText = "SELECT Frame, Path FROM dp2image WHERE Lookupnum = '" + sProdNum +
                "' AND Frame = " + sFrameNum + " ORDER BY Lookupnum ASC";

                TM.CDSQuery(sCDSConnString, sCommText, dt);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        string sPath = Convert.ToString(dr["path"]).Trim();

                        this.GatherFrameData(sProdNum, sFrameNum, sRefNum, sCustNum, sPkgTag, sPath, sDiscQuantity, sBG, ref iUnique, ref bInitialPass01, sGSBG, sDiscType, sSitting, bMultiRenderGS);
                    }
                }
                else if (dt.Rows.Count == 0)
                {
                    string sStop = string.Empty;
                }
            }
            catch(Exception ex)
            {
                TM.SaveExceptionToDB(ex);
            }
        }

        private void GatherFrameData(string sProdNum, string sFrameNum, string sRefNum, string sCustNum, string sPkgTag, string sPath, string sDiscQuantity, string sBG, ref int iUnique, ref bool bInitialPass01, string sGSBG, string sDiscType, string sSitting, bool bMultiRenderGS)
        {
            try
            {
                bool bHaveFile = false;

                string sLabel = "ExportDefPath";
                string sValue = "Value";
                string sVariable = string.Empty;
                bool bSuccess = true;

                TM.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                if (bSuccess == true)
                {
                    string sExportDefPath = sVariable;

                    string sExportDef = string.Empty;
                    string sYear = string.Empty;
                    bSuccess = true;
                    string sUnique = string.Empty;
                    string sYearOn = string.Empty;
                    string sNameOn = string.Empty;

                    if (bInitialPass01 == true)
                    {
                        string sText = "[Gathering frame data for reference number " + sRefNum + " and frame number " + sFrameNum + ".]";
                        this.LogText(sText);

                        bInitialPass01 = false;
                    }


                    DataTable dt2 = new DataTable();
                    string sCommText = "SELECT * FROM Docstyle WHERE Docstyle = '" + sBG + "'";
                    TM.CDSQuery(sCDSConnString, sCommText, dt2);

                    if (dt2.Rows.Count > 0)
                    {
                        string sDP2Bord = Convert.ToString(dt2.Rows[0]["dp2bord"]).Trim();
                        string sDP2Bkgrnd = Convert.ToString(dt2.Rows[0]["dp2bkgrd"]).Trim();
                        string sDP2Color = Convert.ToString(dt2.Rows[0]["dp2color"]).Trim();
                        string sDP2Text = Convert.ToString(dt2.Rows[0]["dp2text"]).Trim();
                        
                        string sDP2Mask = Convert.ToString(dt2.Rows[0]["dp2mask"]).Trim();

                        #region Get the year for products with a static year.

                        DataTable dt3 = new DataTable();
                        sCommText = "SELECT * FROM Cdyear WHERE Bord = '" + sDP2Bord + "' AND Color = '" + sDP2Color + "'";
                        TM.CDSQuery(sCDSConnString, sCommText, dt3);

                        if (dt3.Rows.Count > 0)
                        {
                            sCommText = "SELECT * FROM Pkgdetails WHERE Packagetag = '" + sPkgTag + "'";
                            DataTable dt4 = new DataTable();

                            TM.CDSQuery(sCDSConnString, sCommText, dt4);

                            if (dt4.Rows.Count > 0)
                            {
                                sYear = Convert.ToString(dt4.Rows[0]["Year"]).Trim();
                            }
                            else if (dt4.Rows.Count == 0)
                            {
                                sCommText = "SELECT D_dateent FROM Items WHERE Lookupnum = '" + sProdNum + "'";
                                DataTable dt5 = new DataTable();

                                TM.CDSQuery(sCDSConnString, sCommText, dt5);

                                if (dt5.Rows.Count > 0)
                                {
                                    string sYearFull = Convert.ToString(dt5.Rows[0]["D_dateent"]).Trim();
                                    sYear = Convert.ToString(DateTime.Parse(sYearFull).Year);
                                }
                            }
                        }
                        else if (dt3.Rows.Count == 0)
                        {
                            
                        }

                        #endregion

                        if (sDP2Color == "NONE" && sDP2Bord == "NONE" && sDP2Bkgrnd == "NONE") //000
                        {
                            iUnique += +1;

                            if (sDP2Mask != "NONE")
                            {
                                sExportDef = sDP2Mask;
                            }
                            else if (sDP2Mask == "NONE")
                            {
                                sExportDef = sDP2Text;
                            }
                            
                            sUnique = iUnique + sExportDef;
                        }
                        else if (sDP2Color == "NONE" && sDP2Bord == "NONE" && sDP2Bkgrnd != "NONE") //001
                        {
                            iUnique += +1;
                            sExportDef = sDP2Text + sDP2Bkgrnd;
                            sUnique = iUnique + sExportDef;
                        }
                        else if (sDP2Color == "NONE" && sDP2Bord != "NONE" && sDP2Bkgrnd == "NONE") //010
                        {
                            // Encountered some ExportDef file names that need DP2Bord + Dp2Text that meet the above condition.
                            // I stored these in a table for easy real time handling.

                            DataTable dTblUniqueExportDefDP2Bords = new DataTable("dTblUniqueExportDefDP2Bords");
                            sCommText = "SELECT * FROM [Unique ExportDef DP2Bords]";
                            bool bUniqueFound = false;

                            TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblUniqueExportDefDP2Bords);

                            if (dTblUniqueExportDefDP2Bords.Rows.Count > 0)
                            {
                                foreach (DataRow dRowUniqueExportDefDP2Bords in dTblUniqueExportDefDP2Bords.Rows)
                                {                                    
                                    string sBord = Convert.ToString(dRowUniqueExportDefDP2Bords["DP2Bord"]).Trim();

                                    if (sDP2Bord == sBord)
                                    {
                                        iUnique += +1;
                                        sExportDef = sDP2Bord + sDP2Text;
                                        sUnique = iUnique + sExportDef;

                                        bUniqueFound = true;
                                    }
                                }

                                if (bUniqueFound != true)
                                {
                                    iUnique += +1;
                                    sExportDef = sDP2Bord;
                                    sUnique = iUnique + sExportDef;
                                }
                            }
                            else if (dTblUniqueExportDefDP2Bords.Rows.Count == 0)
                            {
                                string sStop = string.Empty;
                            }
                        }
                        else if (sDP2Color == "NONE" && sDP2Bord != "NONE" && sDP2Bkgrnd != "NONE") //011
                        {
                            iUnique += +1;
                            sExportDef = sDP2Bord + sDP2Bkgrnd;
                            sUnique = iUnique + sExportDef;
                        }
                        else if (sDP2Color != "NONE" && sDP2Bord == "NONE" && sDP2Bkgrnd == "NONE") //100
                        {
                            iUnique += +1;

                            if (sDP2Mask != "NONE")
                            {
                                sExportDef = sDP2Color + sDP2Mask;
                            }
                            else if (sDP2Mask == "NONE")
                            {
                                sExportDef = sDP2Color + sDP2Text;
                            }
                            
                            sUnique = iUnique + sExportDef;
                        }
                        else if (sDP2Color != "NONE" && sDP2Bord == "NONE" && sDP2Bkgrnd != "NONE") //101
                        {
                            iUnique += +1;
                            sExportDef = sDP2Color + sDP2Text + sDP2Bkgrnd;
                            sUnique = iUnique + sExportDef;
                        }
                        else if (sDP2Color != "NONE" && sDP2Bord != "NONE" && sDP2Bkgrnd == "NONE") //110
                        {
                            iUnique += +1;
                            sExportDef = sDP2Color + sDP2Bord;
                            sUnique = iUnique + sExportDef;
                        }
                        else if (sDP2Color != "NONE" && sDP2Bord != "NONE" && sDP2Bkgrnd != "NONE") //111
                        {
                            iUnique += +1;
                            sExportDef = sDP2Color + sDP2Bord + sDP2Bkgrnd;
                            sUnique = iUnique + sExportDef;
                        }

                        if (sYear != string.Empty)
                        {
                            sExportDef += sYear;
                        }

                        bHaveFile = false;
                        TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);

                        if (bHaveFile == true)
                        {
                            TM.GetYear(sPkgTag, ref sYearOn);
                            if (sYearOn == string.Empty)
                            {
                                sYearOn = DateTime.Now.Year.ToString();
                            }
                            TM.GetNameOnData(sProdNum, sFrameNum, ref sNameOn);

                            if (sGSBG != "NONE")
                            {
                                // Gather GSBackground path.
                                bool bGSFound = true;
                                TM.VerifyGS(sGSBG, ref bGSFound);

                                if (bGSFound == true)
                                {
                                    // Save data at this point to the FrameData table.
                                    sCommText = "INSERT INTO FrameData (ProdNum, RefNum, FrameNum, UniqueID, DP2Bord, DP2BkGrnd, DP2Color, DP2Text, ExportDefFile, MultiRenderGS, Processed, ProcessDate, PkgTag, YearOn, NameOn, GSBackground, DiscType, Sitting, DP2Mask)" +
                                        " VALUES ('" + sProdNum + "', '" + sRefNum + "', '" + sFrameNum + "', '" + sRefNum + sFrameNum + sUnique + sDiscType + "', '" + sDP2Bord + "', '" + sDP2Bkgrnd + "', '" +
                                        sDP2Color + "', '" + sDP2Text + "', '" + sExportDef + "', '" + bMultiRenderGS + "', '1', '" + DateTime.Now.ToString() + "', '" + sPkgTag + "', '" + sYearOn + "', '" + sNameOn + "', '" + sGSBG + "', '" + sDiscType + "', '" + sSitting + "', '" + sDP2Mask + "')";
                                }
                                else if (bGSFound != true)
                                {
                                    string sStop = string.Empty;

                                    // Save data at this point to the FrameData table.
                                    sCommText = "INSERT INTO FrameData (ProdNum, RefNum, FrameNum, UniqueID, DP2Bord, DP2BkGrnd, DP2Color, DP2Text, ExportDefFile, MultiRenderGS, Processed, ProcessDate, PkgTag, YearOn, NameOn, GSBackground, DiscType, Sitting, DP2Mask)" +
                                        " VALUES ('" + sProdNum + "', '" + sRefNum + "', '" + sFrameNum + "', '" + sRefNum + sFrameNum + sUnique + sDiscType + "', '" + sDP2Bord + "', '" + sDP2Bkgrnd + "', '" +
                                        sDP2Color + "', '" + sDP2Text + "', '" + sExportDef + "', '" + bMultiRenderGS + "', '1', '" + DateTime.Now.ToString() + "', '" + sPkgTag + "', '" + sYearOn + "', '" + sNameOn + "', '" + sGSBG + "', '" + sDiscType + "', '" + sSitting + "', '" + sDP2Mask + "')";

                                    string sCommText2 = "UPDATE [DiscOrders] SET [Error] = '1', [ErrorDate] = '" + DateTime.Now.ToString() + "', [ErrorDescription] = 'Greenscreen file not located.'" +
                                        " WHERE [ProdNum] = '" + sProdNum + "' AND [FrameNum] = '" + sFrameNum + "' AND [DiscType] = '" + sDiscType + "'";
                                    bool bSuccess2 = false;

                                    TM.SQLNonQuery(sDiscProcessorConnString, sCommText2, ref bSuccess2);

                                    if (bSuccess2 == true)
                                    {

                                    }
                                    else if (bSuccess2 != true)
                                    {
                                        sStop = string.Empty;
                                    }
                                }
                            }
                            else if (sGSBG == "NONE")
                            {
                                // Save data at this point to the FrameData table.
                                sCommText = "INSERT INTO FrameData (ProdNum, RefNum, FrameNum, UniqueID, DP2Bord, DP2BkGrnd, DP2Color, DP2Text, ExportDefFile, MultiRenderGS, Processed, ProcessDate, PkgTag, YearOn, NameOn, DiscType, Sitting, DP2Mask)" +
                                    " VALUES ('" + sProdNum + "', '" + sRefNum + "', '" + sFrameNum + "', '" + sRefNum + sFrameNum + sUnique + sDiscType + "', '" + sDP2Bord + "', '" + sDP2Bkgrnd + "', '" +
                                    sDP2Color + "', '" + sDP2Text + "', '" + sExportDef + "', '" + bMultiRenderGS + "', '1', '" + DateTime.Now.ToString() + "', '" + sPkgTag + "', '" + sYearOn + "', '" + sNameOn + "', '" + sDiscType + "', '" + sSitting + "', '" + sDP2Mask + "')";
                            }

                            bSuccess = true;
                            TM.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                            if (bSuccess == true)
                            {
                                
                            }
                            else if (bSuccess != true)
                            {
                                MessageBox.Show("SQL INSERT/UPDATE Failed : " + Environment.NewLine + sCommText);
                            }
                        }
                        else if (bHaveFile != true)
                        {
                            sExportDef = "NOT FOUND";

                            sCommText = "INSERT INTO FrameData (ProdNum, RefNum, FrameNum, UniqueID, DP2Bord, DP2BkGrnd, DP2Color, DP2Text, ExportDefFile, MultiRenderGS, Processed, ProcessDate, PkgTag, YearOn, NameOn, GSBackground, DiscType, Sitting, DP2Mask)" +
                            " VALUES ('" + sProdNum + "', '" + sRefNum + "', '" + sFrameNum + "', '" + sRefNum + sFrameNum + sUnique + sDiscType + "', '" + sDP2Bord + "', '" + sDP2Bkgrnd + "', '" +
                            sDP2Color + "', '" + sDP2Text + "', '" + sExportDef + "', '" + bMultiRenderGS + "', '1', '" + DateTime.Now.ToString() + "', '" + sPkgTag + "', '" + sYearOn + "', '" + sNameOn + "', '" + sGSBG + "', '" + sDiscType + "', '" + sSitting + "', '" + sDP2Mask + "')";
                            bSuccess = false;                         

                            TM.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                            if (bSuccess == true)
                            {
                                string sCommText2 = "UPDATE [DiscOrders] SET [Error] = '1', [ErrorDate] = '" + DateTime.Now.ToString() + "', [ErrorDescription] = 'ExportDef file not located.'" +
                                " WHERE [ProdNum] = '" + sProdNum + "' AND [FrameNum] = '" + sFrameNum + "' AND [DiscType] = '" + sDiscType + "'";
                                bool bSuccess2 = false;

                                TM.SQLNonQuery(sDiscProcessorConnString, sCommText2, ref bSuccess2);

                                if (bSuccess2 == true)
                                {
                                    string sStatus = "90";

                                    TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                }
                                else if (bSuccess2 != true)
                                {
                                    string sStop = string.Empty;
                                }
                            }
                            else if (bSuccess != true)
                            {
                                MessageBox.Show("SQL INSERT/UPDATE Failed : " + Environment.NewLine + sCommText);
                            }
                        }
                    }
                    else if (dt2.Rows.Count == 0) // If no DocStyle data then use the gsbkgrnd table.
                    {
                        // Save data at this point to the FrameData table.

                        DataTable dt6 = new DataTable();
                        sCommText = "SELECT * FROM gsbkgrd WHERE Gs_bkgrd = '" + sBG + "'";
                        TM.CDSQuery(sCDSConnString, sCommText, dt6);

                        if (dt6.Rows.Count > 0)
                        {
                            TM.GetYear(sPkgTag, ref sYearOn);
                            if (sYearOn == string.Empty)
                            {
                                sYearOn = DateTime.Now.Year.ToString();
                            }
                            TM.GetNameOnData(sProdNum, sFrameNum, ref sNameOn);

                            iUnique += +1;

                            sExportDef = "4UPW.txt";

                            sCommText = "INSERT INTO FrameData (ProdNum, RefNum, FrameNum, UniqueID, GSBackground, ExportDefFile, MultiRenderGS, Processed, ProcessDate, PkgTag, YearOn, NameOn, DiscType, Sitting)" +
                                " VALUES ('" + sProdNum + "', '" + sRefNum + "', '" + sFrameNum + "', '" + sRefNum + sFrameNum + iUnique + sBG + "', '" +
                                sBG + "', '" + sExportDef + "', '" + bMultiRenderGS + "', '1', '" + DateTime.Now.ToString() + "', '" + sPkgTag + "', '" + sYearOn + "', '" + sNameOn + "', '" + sDiscType + "', '" + sSitting + "')";

                            bSuccess = true;

                            TM.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                            if (bSuccess == true)
                            {
                                
                            }
                            else if (bSuccess != true)
                            {
                                MessageBox.Show("SQL INSERT/UPDATE Failed : " + Environment.NewLine + sCommText);
                            }
                        }
                        else if (dt6.Rows.Count == 0)
                        {
                            string sStop = string.Empty;
                        }
                    }
                }
                else if (bSuccess != true)
                {
                    string sStop = string.Empty;
                }
            }
            catch(Exception ex)
            {
                TM.SaveExceptionToDB(ex);
            }
        }

        #endregion

        #region PEC export def file generation and rendering methods.

        private void PECGatherRenderInfo(string sPNum, string sDiscType, ref bool bInitialPassPEC01, ref bool bCreated, DataTable dTblOrder, bool bSittingBased)
        {
            try
            {
                string sStop = string.Empty;

                DataTable dTblSitting = new DataTable("dTblJob");
                List<string> lLayouts = new List<string>();
                int iRenderedCount = 0;
                bool bHaveFile = false;
                string sExportDef = string.Empty;
                bool bInitialJobIDAssigned = false;
                string sExportDefPath = string.Empty;
                string sExportDefPathDone = string.Empty;
                string sAPSDEST = string.Empty;
                string sExportDefFile = string.Empty;
                bool bGoodResults = true;
                string sExportDefSitting = string.Empty;
                string sCustNum = string.Empty;
                string sRefNum = string.Empty;

                if (bCreated != true)
                {
                    dTblSitting.Columns.Add("Count", typeof(int));
                    dTblSitting.Columns.Add("BatchID", typeof(int));
                    dTblSitting.Columns.Add("JobID", typeof(int));
                    dTblSitting.Columns.Add("RefNum", typeof(string));
                    dTblSitting.Columns.Add("Sitting", typeof(string));
                    dTblSitting.Columns.Add("FrameNum", typeof(string));
                    dTblSitting.Columns.Add("ImageName", typeof(string));
                    dTblSitting.Columns.Add("ExportDefFile", typeof(string));
                    dTblSitting.Columns.Add("SavedExportDefPath", typeof(string));
                    dTblSitting.Columns.Add("RenderedImageLocation", typeof(string));
                    dTblSitting.Columns.Add("CustNum", typeof(string));
                    
                    lLayouts.Add("COLOR.txt");
                    lLayouts.Add("BW.txt");
                    lLayouts.Add("SEP.txt");
                    lLayouts.Add("CES.txt");

                    bCreated = true;
                }
                else if (bCreated == true)
                {

                }

                string sProdNum = sPNum;

                DataTable dTblDiscOrders = new DataTable("dTblDiscOrders");
                string sCommText = "SELECT * FROM [DiscOrders] WHERE [ProdNum] = '" + sPNum +
                    "' AND [DiscType] = '" + sDiscType + "'"; // Gather all frame data associated with a single production number.

                TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblDiscOrders);

                if (dTblDiscOrders.Rows.Count > 0)
                {
                    try
                    {
                        foreach (DataRow dRowDiscOrders in dTblDiscOrders.Rows)
                        {
                            bInitialPassPEC01 = true;

                            sRefNum = Convert.ToString(dRowDiscOrders["RefNum"]).Trim();
                            string sSitting = Convert.ToString(dRowDiscOrders["Sitting"]);
                            sExportDefSitting = sSitting;
                            sCustNum = Convert.ToString(dRowDiscOrders["CustNum"]).Trim();

                            if (bInitialPassPEC01 == true)
                            {
                                string sText = "[Gathering image name and path for reference number " + sRefNum + " and sitting number " + sSitting.Trim() + ".]";
                                this.LogText(sText);

                                bInitialPassPEC01 = false;
                            }

                            DataTable dTblFrames = new DataTable("dTblFrames");
                            sCommText = "SELECT * FROM Frames WHERE Lookupnum = '" + sProdNum + "' AND Sitting = '" + sSitting + "' ORDER By Sequence ASC";

                            TM.CDSQuery(sCDSConnString, sCommText, dTblFrames);

                            if (dTblFrames.Rows.Count > 0)
                            {
                                int idTblFramesRowCount = dTblFrames.Rows.Count;

                                bool bIDsGathered = false;
                                int iBatchID = 0;
                                int iJobID = 0;
                                int iJobIDsNeeded = idTblFramesRowCount * 4;
                                bool bLockedFile = false;
                                bool bGatherIDsSuccess = false;

                                if (bIDsGathered != true)
                                {
                                    this.LockFile(ref iBatchID, ref iJobID, ref iJobIDsNeeded, ref bLockedFile, ref bGatherIDsSuccess);
                                }
                                else if (bIDsGathered == true)
                                {
                                    // ID's previously gathered, continue.
                                }

                                int iLastJobID = iJobID + iJobIDsNeeded;

                                if (bLockedFile != true && bGatherIDsSuccess == true)
                                {
                                    dTblSitting.Clear();

                                    foreach (DataRow dRowFrames in dTblFrames.Rows)
                                    {
                                        string sSequence = Convert.ToString(dRowFrames["Sequence"]).Trim();

                                        DataTable dTblDP2Image = new DataTable("dTblDP2Image");
                                        sCommText = "SELECT Path FROM DP2Image WHERE Lookupnum = '" + sProdNum + "' AND Frame = " + sSequence;

                                        TM.CDSQuery(sCDSConnString, sCommText, dTblDP2Image);

                                        if (dTblDP2Image.Rows.Count > 0)
                                        {
                                            string sPath = Convert.ToString(dTblDP2Image.Rows[0]["Path"]).Trim();
                                            bool bExists = false;

                                            TM.ImageExists(sPath, ref bExists);

                                            if (bExists == true)
                                            {
                                                string sRenderedPath = string.Empty;

                                                string sLabel = "PECRenderedPath";
                                                string sValue = "Value";
                                                string sVariable = string.Empty;
                                                bool bSuccess = true;

                                                TM.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                                                if (bSuccess == true)
                                                {
                                                    sRenderedPath = sVariable;

                                                    sLabel = "ExportDefPath";
                                                    sValue = "Value";
                                                    sVariable = string.Empty;
                                                    bSuccess = true;

                                                    TM.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                                                    if (bSuccess == true)
                                                    {
                                                        sExportDefPath = sVariable;

                                                        sLabel = "ExportDefPathDone";
                                                        sValue = "Value";
                                                        sVariable = string.Empty;
                                                        bSuccess = true;

                                                        TM.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                                                        if (bSuccess == true)
                                                        {
                                                            sExportDefPathDone = sVariable;

                                                            if (!Directory.Exists(sExportDefPathDone))
                                                            {
                                                                Directory.CreateDirectory(sExportDefPathDone);
                                                            }
                                                        }
                                                        else if (bSuccess != true)
                                                        {
                                                            sStop = string.Empty;
                                                            bGoodResults = false;
                                                        }
                                                    }
                                                    else if (bSuccess != true)
                                                    {
                                                        sStop = string.Empty;
                                                        bGoodResults = false;
                                                    }
                                                }
                                                else if (bSuccess != true)
                                                {
                                                    sStop = string.Empty;
                                                    bGoodResults = false;
                                                }

                                                foreach (string s in lLayouts)
                                                {
                                                    if (bInitialJobIDAssigned == true)
                                                    {
                                                        // This will prevent skipping the initial gathered JobID when assigning to a rendered product.
                                                        iJobID += +1;
                                                    }
                                                    else if (bInitialJobIDAssigned != true)
                                                    {
                                                        bInitialJobIDAssigned = true;
                                                    }

                                                    if (s == "COLOR.txt")
                                                    {
                                                        sExportDef = s;

                                                        TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);

                                                        if (bHaveFile == true)
                                                        {
                                                            iRenderedCount += 1;

                                                            sAPSDEST = sRenderedPath + sRefNum + @"\" + sProdNum + @"\" + sSitting.Trim() + @"\" + sProdNum + sSitting.Trim() + sSequence + "-" + iRenderedCount + "-Color.JPG";
                                                            sExportDefFile = sExportDefPathDone + "DiscProcessor" + "_" + sRefNum + "_" + sProdNum + "_" + sSitting.Trim() + "_" + sSequence + "-" + iRenderedCount + "-Color.txt";

                                                            string text = File.ReadAllText(sExportDefPath + sExportDef);

                                                            text = text.Replace("APSPATH", sPath);
                                                            text = text.Replace("APSDEST", sAPSDEST);
                                                            text = text.Replace("APSBGID", "");

                                                            File.WriteAllText(sExportDefFile, text);

                                                            dTblSitting.Rows.Add(iRenderedCount, iBatchID, iJobID, sRefNum, sSitting, sSequence, sPath, sExportDef, sExportDefFile, sAPSDEST, sCustNum);
                                                        }
                                                        else if (bHaveFile != true)
                                                        {
                                                            sStop = string.Empty;
                                                            bGoodResults = false;
                                                        }
                                                    }
                                                    else if (s == "BW.txt")
                                                    {
                                                        sExportDef = s;

                                                        TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);

                                                        if (bHaveFile == true)
                                                        {
                                                            iRenderedCount += 1;

                                                            sAPSDEST = sRenderedPath + sRefNum + @"\" + sProdNum + @"\" + sSitting.Trim() + @"\" + sProdNum + sSitting.Trim() + sSequence + "-" + iRenderedCount + "-BW.JPG";
                                                            sExportDefFile = sExportDefPathDone + "DiscProcessor" + "_" + sRefNum + "_" + sProdNum + "_" + sSitting.Trim() + "_" + sSequence + "-" + iRenderedCount + "-BW.txt";

                                                            string text = File.ReadAllText(sExportDefPath + sExportDef);

                                                            text = text.Replace("APSPATH", sPath);
                                                            text = text.Replace("APSDEST", sAPSDEST);
                                                            text = text.Replace("APSBGID", "");

                                                            File.WriteAllText(sExportDefFile, text);

                                                            dTblSitting.Rows.Add(iRenderedCount, iBatchID, iJobID, sRefNum, sSitting, sSequence, sPath, sExportDef, sExportDefFile, sAPSDEST, sCustNum);
                                                        }
                                                        else if (bHaveFile != true)
                                                        {
                                                            sStop = string.Empty;
                                                            bGoodResults = false;
                                                        }
                                                    }
                                                    else if (s == "SEP.txt")
                                                    {
                                                        sExportDef = s;

                                                        TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);

                                                        if (bHaveFile == true)
                                                        {
                                                            iRenderedCount += 1;

                                                            sAPSDEST = sRenderedPath + sRefNum + @"\" + sProdNum + @"\" + sSitting.Trim() + @"\" + sProdNum + sSitting.Trim() + sSequence + "-" + iRenderedCount + "-SEP.JPG";
                                                            sExportDefFile = sExportDefPathDone + "DiscProcessor" + "_" + sRefNum + "_" + sProdNum + "_" + sSitting.Trim() + "_" + sSequence + "-" + iRenderedCount + "-SEP.txt";

                                                            string text = File.ReadAllText(sExportDefPath + sExportDef);

                                                            text = text.Replace("APSPATH", sPath);
                                                            text = text.Replace("APSDEST", sAPSDEST);
                                                            text = text.Replace("APSBGID", "");

                                                            File.WriteAllText(sExportDefFile, text);

                                                            dTblSitting.Rows.Add(iRenderedCount, iBatchID, iJobID, sRefNum, sSitting, sSequence, sPath, sExportDef, sExportDefFile, sAPSDEST, sCustNum);
                                                        }
                                                        else if (bHaveFile != true)
                                                        {
                                                            sStop = string.Empty;
                                                            bGoodResults = false;
                                                        }
                                                    }
                                                    else if (s == "CES.txt")
                                                    {
                                                        sExportDef = s;

                                                        TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);

                                                        if (bHaveFile == true)
                                                        {
                                                            iRenderedCount += 1;

                                                            sAPSDEST = sRenderedPath + sRefNum + @"\" + sProdNum + @"\" + sSitting.Trim() + @"\" + sProdNum + sSitting.Trim() + sSequence + "-" + iRenderedCount + "-CES.JPG";
                                                            sExportDefFile = sExportDefPathDone + "DiscProcessor" + "_" + sRefNum + "_" + sProdNum + "_" + sSitting.Trim() + "_" + sSequence + "-" + iRenderedCount + "-CES.txt";

                                                            string text = File.ReadAllText(sExportDefPath + sExportDef);

                                                            text = text.Replace("APSPATH", sPath);
                                                            text = text.Replace("APSDEST", sAPSDEST);
                                                            text = text.Replace("APSBGID", "");

                                                            File.WriteAllText(sExportDefFile, text);

                                                            dTblSitting.Rows.Add(iRenderedCount, iBatchID, iJobID, sRefNum, sSitting, sSequence, sPath, sExportDef, sExportDefFile, sAPSDEST, sCustNum);
                                                        }
                                                        else if (bHaveFile != true)
                                                        {
                                                            sStop = string.Empty;
                                                            bGoodResults = false;
                                                        }
                                                    }
                                                }
                                            }
                                            else if (bExists != true)
                                            {
                                                sStop = string.Empty;
                                                bGoodResults = false;

                                                string sErrorDescription = "[" + DateTime.Now.ToString() + " ][ Image(s) could not be located.]";

                                                TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sSequence, sDiscType, sExportDefSitting, bSittingBased);

                                                string sStatus = "90";

                                                TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sSequence, sStatus, sDiscType);

                                                TM.RemoveOrphanedExportDefFiles(dTblSitting);
                                                TM.RemoveOrphanedExportDefFiles(dTblOrder);

                                                string sText = "[Removed orphaned exportdef files for  " + sRefNum + ".]";
                                                this.LogText(sText);
                                            }
                                        }
                                        else if (dTblDP2Image.Rows.Count == 0)
                                        {
                                            sStop = string.Empty;
                                            bGoodResults = false;

                                            string sErrorDescription = "[" + DateTime.Now.ToString() + " ][ No Dp2Image records.]";

                                            TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sSequence, sDiscType, sExportDefSitting, bSittingBased);

                                            string sStatus = "90";

                                            TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sSequence, sStatus, sDiscType);

                                            TM.RemoveOrphanedExportDefFiles(dTblSitting);
                                            TM.RemoveOrphanedExportDefFiles(dTblOrder);

                                            string sText = "[Removed orphaned exportdef files for  " + sRefNum + ".]";
                                            this.LogText(sText);
                                        }
                                    }
                                }
                                else if (bLockedFile == true || bGatherIDsSuccess != true)
                                {
                                    sStop = string.Empty;
                                    bGoodResults = false;
                                }

                                dTblOrder.Merge(dTblSitting);
                            }
                            else if (dTblFrames.Rows.Count == 0)
                            {

                            }
                        }
                    }
                    catch (ObjectDisposedException odex)
                    {
                        string sStop2 = string.Empty;

                        TM.RemoveOrphanedExportDefFiles(dTblSitting);
                        TM.RemoveOrphanedExportDefFiles(dTblOrder);

                        string sText = "[Removed orphaned exportdef files for  " + sRefNum + ".]";
                        this.LogText(sText);

                        bool bSuccess = true;
                        sCommText = "UPDATE [DiscOrders] SET [Status] = '10' WHERE [RefNum] = '" + sRefNum + "' AND [DiscType] = '" + sDiscType + "'";

                        TM.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                        if (bSuccess == true)
                        {
                            bGoodResults = false;
                        }
                        else if (bSuccess != true)
                        {
                            sStop = string.Empty;
                            bGoodResults = false;
                        }
                    }

                    // End of foreach through dTblDiscOrders.

                }
                else if (dTblDiscOrders.Rows.Count == 0)
                {

                }

                // End of try block.

                if (bGoodResults == true)
                {
                    bool bInitialPass02 = true;
                    bool bInitialPass03 = true;
                    bool bInitialPass04 = true;
                    bool bInitialPass05 = true;
                    string sUniqueID = string.Empty;

                    this.ExportDefProcessing(sProdNum, bGoodResults, dTblOrder, ref bInitialPass02, ref bInitialPass03, ref bInitialPass04, ref bInitialPass05, sDiscType, sExportDefSitting, bSittingBased);
                }
                else if (bGoodResults != true)
                {
                    dTblSitting.Clear();
                    dTblOrder.Clear();

                    // Go back into foreach, order with issue will be picked up next cycle or flagged as stalled if not.
                }
            }
            catch (Exception ex)
            {
                TM.SaveExceptionToDB(ex);
            }
        }

        #endregion

        #endregion

        #region ExportDef generation, insertion of records into the JobQueue table, gather merge and nwp file creation.

        // Gather records in the FrameData table that are ready for processing. 
        private void ExportDefGeneration()
        {
            try
            {
                string sPath = string.Empty;
                string sExportDefPkgTag = string.Empty;
                string sExportDefRefNum = string.Empty;
                string sExportDefFrameNum = string.Empty;
                string sExportDefSitting = string.Empty;
                bool bMultiRenderGS;
                string sExportDefProdNum = string.Empty;
                int iJobIDsNeeded = 0;
                string sExportDef = string.Empty;
                string sYearOn = string.Empty;
                string sNameOn = string.Empty;
                string sUniqueID = string.Empty;
                DataTable dTblJob = new DataTable();
                bool bCreated = false;
                int iRenderedCount = 0;
                bool bIDsGathered = false;
                int iJobID = 0;
                int iBatchID = 0;
                int iCount = 0;
                int iLoops = 0;
                bool bInitialJobIDAssigned = false;
                bool bGoodResults = true;
                bool bInitialPass01 = true;
                bool bInitialPass02 = true;
                bool bInitialPass03 = true;
                bool bInitialPass04 = true;
                bool bInitialPass05 = true;
                string sGSBG = string.Empty;
                string sDiscType = string.Empty;

                string sCommText = "SELECT DISTINCT [FrameData].[ProdNum], [FrameData].[RefNum], [FrameData].[FrameNum], [FrameData].[Sitting], [FrameData].[DiscType] FROM [FrameData], [DiscOrders] WHERE" +
                    " [FrameData].[Processed] = '1' AND ([FrameData].[ExportDefGenerated] IS NULL OR [FrameData].[ExportDefGenerated] = '0') AND ([DiscOrders].[Error] != '1' OR [DiscOrders].[Error] IS NULL) AND [FrameData].[ProdNum] = [DiscOrders].[ProdNum]";
                DataTable dTbl = new DataTable();

                TM.SQLQuery(sDiscProcessorConnString, sCommText, dTbl);

                if (dTbl.Rows.Count > 0)
                {
                    string sText = "[Beginning ExportDef file generation for collected records.]";
                    this.LogText(sText);

                    foreach (DataRow dRow in dTbl.Rows)
                    {
                        dTblJob.Clear();
                        iRenderedCount = 0;
                        iLoops = 0;
                        bIDsGathered = false;
                        bInitialPass01 = true;

                        sExportDefProdNum = Convert.ToString(dRow["ProdNum"]).Trim();
                        sExportDefRefNum = Convert.ToString(dRow["RefNum"]).Trim();
                        sExportDefFrameNum = Convert.ToString(dRow["FrameNum"]).Trim();
                        sExportDefSitting = Convert.ToString(dRow["Sitting"]).Trim();
                        sDiscType = Convert.ToString(dRow["DiscType"]).Trim();

                        DataTable dTblGatherDiscTypes = new DataTable("dTblGatherDiscTypes");
                        sCommText = "SELECT * FROM [GatherDiscTypes] WHERE [GatherDiscType] = '" + sDiscType + "'";

                        TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblGatherDiscTypes);

                        if (dTblGatherDiscTypes.Rows.Count > 0)
                        {
                            bool bSittingBased = Convert.ToBoolean(dTblGatherDiscTypes.Rows[0]["SittingBased"]);

                            bool bSuccess = false;

                            TM.CheckForImages(sExportDefRefNum, sExportDefFrameNum, sDiscType, ref bSuccess);

                            if (bSuccess == true)
                            {
                                sCommText = "SELECT * FROM [FrameData] WHERE [ProdNum] = '" + sExportDefProdNum + "' AND [FrameNum] = '" + sExportDefFrameNum + "' AND [DiscType] = '" + sDiscType + "'";
                                DataTable dTbl2 = new DataTable();

                                TM.SQLQuery(sDiscProcessorConnString, sCommText, dTbl2);

                                if (dTbl2.Rows.Count > 0)
                                {
                                    foreach (DataRow dRow2 in dTbl2.Rows)
                                    {
                                        iCount = dTbl2.Rows.Count * 3;

                                        bInitialPass02 = true;
                                        bInitialPass03 = true;
                                        bInitialPass04 = true;
                                        bInitialPass05 = true;

                                        string sFrameNum = Convert.ToString(dRow2["FrameNum"]).Trim();

                                        // Gather image name from DiscOrders table
                                        DataTable dTblGetImageFromDiscOrders = new DataTable("dTblGetImageFromDiscOrders");
                                        sCommText = "SELECT [ImageLocation], [JobIDsNeeded] FROM [DiscOrders] WHERE [ProdNum] = '" + sExportDefProdNum + "' AND [Sitting] = '" + sExportDefSitting + "'";

                                        TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblGetImageFromDiscOrders);

                                        if (dTblGetImageFromDiscOrders.Rows.Count > 0)
                                        {
                                            sPath = Convert.ToString(dTblGetImageFromDiscOrders.Rows[0]["ImageLocation"]).Trim();
                                            iJobIDsNeeded = Convert.ToInt32(dTblGetImageFromDiscOrders.Rows[0]["JobIDsNeeded"]);

                                            sExportDefPkgTag = Convert.ToString(dRow2["PkgTag"]).Trim();
                                            bMultiRenderGS = Convert.ToBoolean(dRow2["MultiRenderGS"]);
                                            sGSBG = Convert.ToString(dRow2["GSBackground"]).Trim();                                            
                                            sExportDef = Convert.ToString(dRow2["ExportDefFile"]).Trim();
                                            sUniqueID = Convert.ToString(dRow2["UniqueID"]).Trim();
                                            sNameOn = Convert.ToString(dRow2["NameOn"]).Trim();
                                            sYearOn = Convert.ToString(dRow2["YearOn"]).Trim();
                                            string sRefNum = Convert.ToString(dRow2["RefNum"]).Trim();
                                            string sCustNum = string.Empty;
                                            string sDP2Mask = Convert.ToString(dRow2["DP2Mask"]).Trim();

                                            sCommText = "SELECT [CustNum], [DiscType] FROM [DiscOrders] WHERE [ProdNum] = '" + sExportDefProdNum + "'";
                                            DataTable dTbl3 = new DataTable();

                                            TM.SQLQuery(sDiscProcessorConnString, sCommText, dTbl3);

                                            if (dTbl3.Rows.Count > 0)
                                            {
                                                sCustNum = Convert.ToString(dTbl3.Rows[0]["CustNum"]).Trim();

                                                if (sDiscType == "ICD" || sDiscType == "ICDW")
                                                {
                                                    if (bMultiRenderGS == true)
                                                    {
                                                        bool bStylesAndBGs = true;
                                                        this.ICDExportDefModifying(sPath, sExportDefPkgTag, sExportDefRefNum, sExportDef, sExportDefProdNum, sFrameNum, iJobIDsNeeded, sYearOn, sNameOn, sUniqueID, ref dTblJob, ref bCreated, ref iRenderedCount, ref bIDsGathered, ref iJobID, ref iBatchID, ref iLoops, iCount, ref bInitialJobIDAssigned, sCustNum, ref bGoodResults, ref bInitialPass01, sGSBG, bStylesAndBGs, dTbl2, sDiscType, sDP2Mask, sExportDefSitting, bSittingBased);
                                                    }
                                                    else if (bMultiRenderGS != true)
                                                    {
                                                        bool bStylesAndBGs = false;
                                                        this.ICDExportDefModifying(sPath, sExportDefPkgTag, sExportDefRefNum, sExportDef, sExportDefProdNum, sFrameNum, iJobIDsNeeded, sYearOn, sNameOn, sUniqueID, ref dTblJob, ref bCreated, ref iRenderedCount, ref bIDsGathered, ref iJobID, ref iBatchID, ref iLoops, iCount, ref bInitialJobIDAssigned, sCustNum, ref bGoodResults, ref bInitialPass01, sGSBG, bStylesAndBGs, dTbl2, sDiscType, sDP2Mask, sExportDefSitting, bSittingBased);
                                                    }
                                                }
                                                else if (sDiscType == "MEG" || sDiscType == "MEGW")
                                                {
                                                    this.MEGExportDefModifying(sPath, sExportDefPkgTag, sExportDefRefNum, sExportDef, sExportDefProdNum, sFrameNum, iJobIDsNeeded, sYearOn, sNameOn, sUniqueID, ref dTblJob, ref bCreated, ref iRenderedCount, ref bIDsGathered, ref iJobID, ref iBatchID, ref iLoops, iCount, ref bInitialJobIDAssigned, sCustNum, ref bGoodResults, ref bInitialPass01, sGSBG, dTbl2, sDiscType, sDP2Mask, sExportDefSitting, bSittingBased);
                                                }
                                            }
                                            else if (dTbl3.Rows.Count == 0)
                                            {
                                                string sStop = string.Empty;
                                            }
                                        }
                                        else if (dTblGetImageFromDiscOrders.Rows.Count == 0)
                                        {
                                            string sStop = string.Empty;
                                        }
                                    }

                                    if (dTblJob.Rows.Count > 0)
                                    {
                                        this.ExportDefProcessing(sExportDefProdNum, bGoodResults, dTblJob, ref bInitialPass02, ref bInitialPass03, ref bInitialPass04, ref bInitialPass05, sDiscType, sExportDefSitting, bSittingBased);
                                    }
                                    else if (dTblJob.Rows.Count == 0)
                                    {
                                        string sStop = string.Empty;
                                    }                                    
                                }
                            }
                            else if (bSuccess != true)
                            {
                                string sStop = string.Empty;

                                string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ Images not located on server. ]";

                                TM.UpdateDiscOrdersForErrors(sErrorDescription, sExportDefRefNum, sExportDefFrameNum, sDiscType, sExportDefSitting, bSittingBased);

                                string sStatus = "90";

                                TM.UpdateDiscOrdersTableStatusFrameBased(sExportDefRefNum, sExportDefFrameNum, sStatus, sDiscType);
                            }
                        }
                        else if (dTblGatherDiscTypes.Rows.Count == 0)
                        {
                            string sStop = string.Empty;
                        }
                    }
                }
                else if (dTbl.Rows.Count == 0)
                {

                }
            }
            catch(Exception ex)
            {
                TM.SaveExceptionToDB(ex);
            }
        }

        // Gather and process records in the FrameData table that have passed through one of the ExportDefModifying methods. 
        private void ExportDefProcessing(string sExportDefProdNum, bool bGoodResults, DataTable dTblJob, ref bool bInitialPass02, ref bool bInitialPass03, ref bool bInitialPass04, ref bool bInitialPass05, string sDiscType, string sExportDefSitting, bool bSittingBased)
        {
            try
            {
                int iJobsBatchID = 0;
                string sJobsRefNum = string.Empty;
                string sJobsFrameNum = string.Empty;
                string sSitting = string.Empty;
                string sStatus = string.Empty;
                string sFrameNum = string.Empty;
                string sLastSitting = string.Empty;
                bool bGotNWPFileData = false;
                string sLastSitting2 = string.Empty;
                bool bInsertFailedWarned = false;

                if (bGoodResults == true)
                {
                    if (dTblJob.Rows.Count > 0)
                    {
                        if (bSittingBased != true)
                        {
                            sJobsRefNum = Convert.ToString(dTblJob.Rows[0]["RefNum"]).Trim();
                            sJobsFrameNum = Convert.ToString(dTblJob.Rows[0]["FrameNum"]).Trim();

                            // Update the FrameData table for current record that exportdef file has been generated
                            TM.UpdateFrameDataForExportDefGenerated(sJobsRefNum, sJobsFrameNum, sDiscType);
                        }

                        bool bInserted = true;
                        // Foreach through dTblJobs and insert required data from dTblJobs into dp2.jobqueue  and set at 0 (HOLD)
                        foreach (DataRow dRowJobs in dTblJob.Rows)
                        {
                            if (bSittingBased == true)
                            {
                                bInitialPass02 = true;
                            }

                            string sJobsExportDefFile = Convert.ToString(dRowJobs["SavedExportDefPath"]).Trim();
                            sJobsRefNum = Convert.ToString(dRowJobs["RefNum"]).Trim();
                            sJobsFrameNum = Convert.ToString(dRowJobs["FrameNum"]).Trim();
                            int iJobsJobsID = Convert.ToInt32(dRowJobs["JobID"]);
                            iJobsBatchID = Convert.ToInt32(dRowJobs["BatchID"]);

                            if (bSittingBased == true)
                            {
                                sSitting = Convert.ToString(dRowJobs["Sitting"]);
                            }
                            else if (bSittingBased != true)
                            {
                                DataTable dTblGetSittingFromDiscOrders = new DataTable("dTblGetSittingFromDiscOrders");
                                string sCommText = "SELECT [Sitting] FROM [DiscOrders] WHERE [RefNum] = '" + sJobsRefNum + "' AND [FrameNum] = '" + sJobsFrameNum + "'";

                                TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblGetSittingFromDiscOrders);

                                if (dTblGetSittingFromDiscOrders.Rows.Count > 0)
                                {
                                    sSitting = Convert.ToString(dTblGetSittingFromDiscOrders.Rows[0]["Sitting"]);
                                }
                                else if (dTblGetSittingFromDiscOrders.Rows.Count == 0)
                                {
                                    string sStop = string.Empty;
                                }
                            }                            

                            this.JobQueueInsert(sJobsExportDefFile, sJobsRefNum, sJobsFrameNum, iJobsJobsID, iJobsBatchID, ref bInserted, ref bInitialPass02, bSittingBased, sSitting);
                        }

                        if (bInserted == true)
                        {
                            if (bSittingBased != true)
                            {
                                // Update DiscOrders.Status = 30 (Records added to the JobQueue table on HOLD)
                                sStatus = "30";
                                TM.UpdateDiscOrdersTableStatusFrameBased(sJobsRefNum, sJobsFrameNum, sStatus, sDiscType);
                            }
                            else if (bSittingBased == true)
                            {
                                var vSittings = dTblJob.AsEnumerable()
                                    .Select(row => new
                                    {
                                        Sitting = row.Field<string>("Sitting")
                                    })
                                .Distinct();

                                foreach (var v in vSittings)
                                {
                                    sSitting = Convert.ToString(v.Sitting);

                                    // Update DiscOrders.Status = 30 (Records added to the JobQueue table on HOLD)
                                    sStatus = "30";
                                    TM.UpdateDiscOrdersTableStatusSittingBased(sJobsRefNum, sStatus, sDiscType);
                                }
                            }

                            string sCustNum = Convert.ToString(dTblJob.Rows[0]["CustNum"]).Trim();
                            string sMrgJPG = Convert.ToString(dTblJob.Rows[0]["ImageName"]).Trim();
                            string sMrgFilePath = string.Empty;

                            bool bGotMergeFileData = false;

                            if (bSittingBased == true)
                            {
                                var vSittings2 = dTblJob.AsEnumerable()
                                    .Select(row => new
                                    {
                                        Sitting = row.Field<string>("Sitting"),
                                        FrameNum = row.Field<string>("FrameNum")
                                    })
                                .Distinct();

                                foreach (var v in vSittings2)
                                {
                                    bInitialPass03 = true;

                                    sSitting = Convert.ToString(v.Sitting);
                                    sFrameNum = Convert.ToString(v.FrameNum);

                                    if (sSitting != sLastSitting)
                                    {
                                        this.GetMergeFileData(sExportDefProdNum, sCustNum, sFrameNum, sMrgJPG, sJobsRefNum, ref sMrgFilePath, ref bGotMergeFileData, ref bInitialPass03, sDiscType, sSitting, bSittingBased);

                                        sLastSitting = sSitting;
                                    }
                                    else if (sSitting == sLastSitting)
                                    {
                                        // Continue
                                    }
                                }
                            }
                            else if (bSittingBased != true)
                            {
                                this.GetMergeFileData(sExportDefProdNum, sCustNum, sJobsFrameNum, sMrgJPG, sJobsRefNum, ref sMrgFilePath, ref bGotMergeFileData, ref bInitialPass03, sDiscType, sExportDefSitting, bSittingBased);
                            }                            

                            if (bGotMergeFileData == true)
                            {
                                if (bSittingBased != true)
                                {
                                    bGotNWPFileData = false;
                                    this.GetNWPFileData(sExportDefProdNum, sMrgFilePath, sJobsFrameNum, sJobsRefNum, ref bGotNWPFileData, ref bInitialPass04, sDiscType, sCustNum, sExportDefSitting, bSittingBased, sSitting);
                                }
                                else if (bSittingBased == true)
                                {
                                    var vSittings3 = dTblJob.AsEnumerable()
                                        .Select(row => new
                                        {
                                            Sitting = row.Field<string>("Sitting"),
                                            FrameNum = row.Field<string>("FrameNum")
                                        })
                                    .Distinct();

                                    foreach (var v in vSittings3)
                                    {
                                        sSitting = Convert.ToString(v.Sitting);
                                        sFrameNum = Convert.ToString(v.FrameNum);

                                        if (sSitting != sLastSitting2)
                                        {
                                            this.GetNWPFileData(sExportDefProdNum, sMrgFilePath, sFrameNum, sJobsRefNum, ref bGotNWPFileData, ref bInitialPass04, sDiscType, sCustNum, sExportDefSitting, bSittingBased, sSitting);

                                            sLastSitting2 = sSitting;
                                        }
                                        else if (sSitting == sLastSitting2)
                                        {
                                            // Continue
                                        }
                                    }
                                }

                                if (bGotNWPFileData == true)
                                {
                                    if (bSittingBased != true)
                                    {
                                        bool bJobQueueStatusUpdated = false;
                                        // Update all records associated with the BatchID to 1 (READY)                                    
                                        this.JobQueuePrintStatusUpdateFrameBased(sJobsRefNum, sJobsFrameNum, ref iJobsBatchID, ref bJobQueueStatusUpdated, ref bInitialPass05);

                                        // Update DiscOrders.Status = 35 (Records added to the JobQueue table previously now flagged as READY)
                                        sStatus = "35";
                                        TM.UpdateDiscOrdersTableStatusFrameBased(sJobsRefNum, sJobsFrameNum, sStatus, sDiscType);
                                    }
                                    else if (bSittingBased == true)
                                    {
                                        bool bJobQueueStatusUpdated = false;
                                        // Update all records associated with the BatchID to 1 (READY) 
                                        this.JobQueuePrintStatusUpdateSittingBased(sJobsRefNum, ref iJobsBatchID, ref bJobQueueStatusUpdated, ref bInitialPass05);

                                        if (bJobQueueStatusUpdated == true)
                                        {
                                            // Update DiscOrders.Status = 35 (Records added to the JobQueue table previously now flagged as READY)
                                            sStatus = "35";
                                            TM.UpdateDiscOrdersTableStatusSittingBased(sJobsRefNum, sStatus, sDiscType);
                                        }
                                        else if (bJobQueueStatusUpdated != true)
                                        {
                                            string sStop = string.Empty;

                                            string sErrorDescription = "[" + DateTime.Now.ToString() + " ][ Failed to update Jobqueue status.]";

                                            TM.UpdateDiscOrdersForErrors(sErrorDescription, sJobsRefNum, sJobsFrameNum, sDiscType, sExportDefSitting, bSittingBased);

                                            sStatus = "90";

                                            if (bSittingBased != true)
                                            {
                                                TM.UpdateDiscOrdersTableStatusFrameBased(sJobsRefNum, sJobsFrameNum, sStatus, sDiscType);
                                            }
                                            else if (bSittingBased == true)
                                            {
                                                TM.UpdateDiscOrdersTableStatusSittingBased(sJobsRefNum, sStatus, sDiscType);
                                            }
                                        }
                                    }
                                }
                                else if (bGotNWPFileData != true)
                                {
                                    string sStop = string.Empty;

                                    string sErrorDescription = "[" + DateTime.Now.ToString() + " ][ Failed to gather NWP file data.]";

                                    TM.UpdateDiscOrdersForErrors(sErrorDescription, sJobsRefNum, sJobsFrameNum, sDiscType, sExportDefSitting, bSittingBased);

                                    sStatus = "90";

                                    if (bSittingBased != true)
                                    {
                                        TM.UpdateDiscOrdersTableStatusFrameBased(sJobsRefNum, sJobsFrameNum, sStatus, sDiscType);
                                    }
                                    else if (bSittingBased == true)
                                    {
                                        TM.UpdateDiscOrdersTableStatusSittingBased(sJobsRefNum, sStatus, sDiscType);
                                    }

                                    TM.RemoveOrphanedExportDefFiles(dTblJob);

                                    string sText = "[Removed orphaned exportdef files for  " + sJobsRefNum + ".]";
                                    this.LogText(sText);
                                }
                            }
                            else if (bGotMergeFileData != true)
                            {
                                string sStop = string.Empty;

                                string sErrorDescription = "[" + DateTime.Now.ToString() + " ][ Failed to gather merge file data.]";

                                TM.UpdateDiscOrdersForErrors(sErrorDescription, sJobsRefNum, sJobsFrameNum, sDiscType, sExportDefSitting, bSittingBased);

                                sStatus = "90";

                                if (bSittingBased != true)
                                {
                                    TM.UpdateDiscOrdersTableStatusFrameBased(sJobsRefNum, sJobsFrameNum, sStatus, sDiscType);
                                }
                                else if (bSittingBased == true)
                                {
                                    TM.UpdateDiscOrdersTableStatusSittingBased(sJobsRefNum, sStatus, sDiscType);
                                }

                                TM.RemoveOrphanedExportDefFiles(dTblJob);

                                string sText = "[Removed orphaned exportdef files for  " + sJobsRefNum + ".]";
                                this.LogText(sText);
                            }
                        }
                        else if (bInserted != true && bInsertFailedWarned != true)
                        {
                            string sStop = string.Empty;

                            bInsertFailedWarned = true;

                            string sErrorDescription = "[" + DateTime.Now.ToString() + "][Record failed to be inserted in the DP2.JobQueue table.]";

                            TM.UpdateDiscOrdersForErrors(sErrorDescription, sJobsRefNum, sJobsFrameNum, sDiscType, sExportDefSitting, bSittingBased);

                            sStatus = "90";

                            if (bSittingBased != true)
                            {
                                TM.UpdateDiscOrdersTableStatusFrameBased(sJobsRefNum, sJobsFrameNum, sStatus, sDiscType);
                            }
                            else if (bSittingBased == true)
                            {
                                TM.UpdateDiscOrdersTableStatusSittingBased(sJobsRefNum, sStatus, sDiscType);
                            }                            

                            TM.RemoveOrphanedExportDefFiles(dTblJob);

                            string sText = "[Removed orphaned exportdef files for  " + sJobsRefNum + ".]";
                            this.LogText(sText);
                        }
                    }
                    else if (dTblJob.Rows.Count == 0)
                    {
                        string sStop = string.Empty;
                    }
                }
                else if (bGoodResults != true)
                {
                    string sStop = string.Empty;

                    TM.RemoveOrphanedExportDefFiles(dTblJob);
                }
            }
            catch(Exception ex)
            {
                TM.SaveExceptionToDB(ex);
            }
        }

        #region Lock file, JobID and BatchID related.

        private void LockFile(ref int iBatchID, ref int iJobID, ref int iJobIDsNeeded, ref bool bLockedFile, ref bool bGatherIDsSuccess)
        {
            try
            {
                string sText = "[Locking file to gather BatchID and JobID.]";
                this.LogText(sText);

                string sLabel = "LockFile";
                string sValue = "Value";
                string sVariable = string.Empty;
                bool bSuccess = true;

                string sLockFile = string.Empty;

                TM.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                if (bSuccess == true)
                {
                    sLockFile = Convert.ToString(sVariable);

                    fsLock = new FileStream(sLockFile, FileMode.Open, FileAccess.Read, FileShare.None);

                    try
                    {
                        // Lock the file.
                        fsLock.Lock(0, fsLock.Length);
                    }
                    catch(IOException)
                    {
                        try
                        {
                            TimeSpan tSpan = new TimeSpan(0, 0, 5); // Sleep the thread for 5 seconds if the file is currently locked.
                            Thread.Sleep(tSpan);

                            // Lock the file.
                            fsLock.Lock(0, fsLock.Length);
                        }
                        catch(IOException)
                        {
                            try
                            {
                                TimeSpan tSpan = new TimeSpan(0, 0, 10); // Sleep the thread for 10 seconds if the file is currently locked.
                                Thread.Sleep(tSpan);

                                // Lock the file.
                                fsLock.Lock(0, fsLock.Length);
                            }
                            catch(IOException)
                            {
                                bLockedFile = true;
                            }
                        }
                    }

                    if (bLockedFile != true)
                    {
                        bool bGetSuccess = true;

                        this.GetBatchID(ref iBatchID, ref bGetSuccess);

                        if (bGetSuccess == true)
                        {
                            bool bUpdateSuccess = true;
                            this.UpdateBatchID(ref bUpdateSuccess);

                            if (bUpdateSuccess == true)
                            {
                                this.GetJobID(ref iJobID, ref bGetSuccess);

                                if (bGetSuccess == true)
                                {
                                    this.UpdateJobID(iJobIDsNeeded, ref bUpdateSuccess);

                                    if (bUpdateSuccess == true)
                                    {
                                        // Unlock the file.
                                        fsLock.Unlock(0, fsLock.Length);
                                        fsLock.Close();

                                        bGatherIDsSuccess = true;
                                    }
                                    else if (bUpdateSuccess != true)
                                    {
                                        string sStop = string.Empty;
                                        bGatherIDsSuccess = false;
                                    }
                                }
                                else if (bGetSuccess != true)
                                {
                                    string sStop = string.Empty;
                                    bGatherIDsSuccess = false;
                                }
                            }
                            else if (bUpdateSuccess != true)
                            {
                                string sStop = string.Empty;
                                bGatherIDsSuccess = false;
                            }
                        }
                        else if (bGetSuccess != true)
                        {
                            string sStop = string.Empty;
                            bGatherIDsSuccess = false;
                        }
                    }
                }
                else if (bSuccess != true)
                {
                    string sStop = string.Empty;
                    bGatherIDsSuccess = false;
                }
            }
            catch(Exception ex)
            {
                bGatherIDsSuccess = false;

                fsLock.Unlock(0, fsLock.Length);
                fsLock.Close();

                TM.SaveExceptionToDB(ex);
            }
        }

        private void GetBatchID(ref int iBatchID, ref bool bGetSuccess)
        {
            try
            {
                //string sText = "[Gathering BatchID.]";
                //this.LogText(sText);

                DataTable dt = new DataTable();
                string sCommText = "SELECT * FROM [IDs] WHERE [NAME] = 'PrintBatchID'";

                TM.SQLQuery(sDP2ConnString, sCommText, dt);

                if (dt.Rows.Count > 0)
                {
                    iBatchID = Convert.ToInt32(dt.Rows[0]["ID"]) + 2;
                    bGetSuccess = true;
                }
                else if (dt.Rows.Count == 0)
                {
                    bGetSuccess = false;
                }
            }
            catch(Exception ex)
            {
                fsLock.Unlock(0, fsLock.Length);
                fsLock.Close();

                TM.SaveExceptionToDB(ex);
                bGetSuccess = false;
            }
        }

        private void UpdateBatchID(ref bool bUpdateSuccess)
        {
            try
            {
                //string sText = "[Updating BatchID.]";
                //this.LogText(sText);

                string sCommText = "UPDATE [IDs] SET [ID] = ID+4 WHERE [NAME] = 'PrintBatchID'";

                bool bSuccess = true;

                TM.SQLNonQuery(sDP2ConnString, sCommText, ref bSuccess);

                if (bSuccess == true)
                {
                    bUpdateSuccess = true;
                }
                else if (bSuccess != true)
                {
                    string sStop = string.Empty;
                    bUpdateSuccess = false;
                }
            }
            catch(Exception ex)
            {
                fsLock.Unlock(0, fsLock.Length);
                fsLock.Close();

                TM.SaveExceptionToDB(ex);
                bUpdateSuccess = false;
            }
        }

        private void GetJobID(ref int iJobID, ref bool bGetSuccess)
        {
            try
            {
                //string sText = "[Gathering JobID.]";
                //this.LogText(sText);

                DataTable dt = new DataTable();
                string sCommText = "SELECT * FROM [IDs] WHERE [NAME] = 'PrintJobID'";

                TM.SQLQuery(sDP2ConnString, sCommText, dt);

                if (dt.Rows.Count > 0)
                {
                    iJobID = Convert.ToInt32(dt.Rows[0]["ID"]) + 2;
                    bGetSuccess = true;
                }
                else if (dt.Rows.Count == 0)
                {
                    string sStop = string.Empty;
                    bGetSuccess = false;
                }
            }
            catch(Exception ex)
            {
                fsLock.Unlock(0, fsLock.Length);
                fsLock.Close();

                TM.SaveExceptionToDB(ex);
                bGetSuccess = false;
            }
        }

        private void UpdateJobID(int iIDsNeeded, ref bool bUpdateSuccess)
        {
            try
            {
                //string sText = "[Updating JobID.]";
                //this.LogText(sText);

                string sCommText = "UPDATE [IDs] SET [ID] = ID+" + (iIDsNeeded + 3) + " WHERE [NAME] = 'PrintJobID'";

                bool bSuccess = true;

                TM.SQLNonQuery(sDP2ConnString, sCommText, ref bSuccess);

                if (bSuccess == true)
                {
                    bUpdateSuccess = true;
                }
                else if (bSuccess != true)
                {
                    bUpdateSuccess = false;
                }
            }
            catch(Exception ex)
            {
                fsLock.Unlock(0, fsLock.Length);
                fsLock.Close();

                TM.SaveExceptionToDB(ex);
                bUpdateSuccess = false;
            }
        }

        #endregion

        // Generate the required ICD ExportDef files for current order.
        private void ICDExportDefModifying(string sPath, string sPkgTag, string sRefNum, string sExportDef, string sProdNum, string sFrameNum, int iJobIDsNeeded, string sYearOn, string sNameOn, string sUniqueID, ref DataTable dTblJob, ref bool bCreated, ref int iRenderedCount, ref bool bIDsGathered, ref int iJobID, ref int iBatchID, ref int iLoops, int iCount, ref bool bInitialJobIDAssigned, string sCustNum, ref bool bGoodResults, ref bool bInitialPass01, string sGSBkGrnd, bool bStylesAndBGs, DataTable dTbl2, string sDiscType, string sDP2Mask, string sExportDefSitting, bool bSittingBased)
        {
            try
            {
                string sRenderedPath = string.Empty;

                string sLabel = "ICDRenderedPath";
                string sValue = "Value";
                string sVariable = string.Empty;
                bool bSuccess = true;

                TM.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                if (bSuccess == true)
                {
                    sRenderedPath = sVariable;

                    bool bGatherIDsSuccess = true;

                    string sExportDefPath = string.Empty;
                    string sExportDefPathDone = string.Empty;

                    sLabel = "ExportDefPath";
                    sValue = "Value";
                    sVariable = string.Empty;
                    bSuccess = true;

                    TM.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                    if (bSuccess == true)
                    {
                        sExportDefPath = sVariable;

                        sLabel = "ExportDefPathDone";
                        sValue = "Value";
                        sVariable = string.Empty;
                        bSuccess = true;

                        TM.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                        if (bSuccess == true)
                        {
                            sExportDefPathDone = sVariable;

                            if (!Directory.Exists(sExportDefPathDone))
                            {
                                Directory.CreateDirectory(sExportDefPathDone);
                            }

                            string sMrgJPG = Path.GetFileName(sPath);
                            string sExportDefFile = string.Empty;
                            string sAPSDEST = string.Empty;
                            string sExportDefOriginal = sExportDef;
                            bool bLockedFile = false;
                            int iLastJobID = 0;
                            bool bHaveFile = false;

                            if (bInitialPass01 == true)
                            {
                                string sText = "[Generating ExportDef files for reference number " + sRefNum + " and frame number " + sFrameNum + ".]";
                                this.LogText(sText);

                                bInitialPass01 = false;
                            }

                            if (bIDsGathered == false)
                            {
                                this.LockFile(ref iBatchID, ref iJobID, ref iJobIDsNeeded, ref bLockedFile, ref bGatherIDsSuccess);
                            }
                            else if (bIDsGathered != false)
                            {

                            }

                            iLastJobID = iJobID + iJobIDsNeeded;

                            if (bLockedFile != true && bGatherIDsSuccess == true)
                            {
                                bIDsGathered = true;

                                // Create a datatable to store the entire records worth of needed exportdef files which then is looped through and pushed into the dp2.jobqueue table.

                                if (bCreated == false)
                                {
                                    dTblJob.Columns.Add("Count", typeof(int)); // iRenderedCount
                                    dTblJob.Columns.Add("UniqueID", typeof(string)); // sUniqueID
                                    dTblJob.Columns.Add("BatchID", typeof(int)); // iBatchID
                                    dTblJob.Columns.Add("JobID", typeof(int)); // iJobID
                                    dTblJob.Columns.Add("RefNum", typeof(string)); // sRefNum
                                    dTblJob.Columns.Add("FrameNum", typeof(string)); // sFrameNum
                                    dTblJob.Columns.Add("ExportDefFile", typeof(string)); // sExportDef
                                    dTblJob.Columns.Add("SavedExportDefPath", typeof(string)); // sExportDefFile                        
                                    dTblJob.Columns.Add("RenderedImageLocation", typeof(string)); // sAPSDEST
                                    dTblJob.Columns.Add("NameOn", typeof(string)); // sNameOn
                                    dTblJob.Columns.Add("YearOn", typeof(string)); // sYearOn
                                    dTblJob.Columns.Add("ImageName", typeof(string)); // sMrgJPG
                                    dTblJob.Columns.Add("CustNum", typeof(string)); // sCustNum

                                    bCreated = true;
                                }
                                else if (bCreated != false)
                                {

                                }

                                List<string> lLayouts = new List<string>();
                                lLayouts.Add("8x10wName");
                                lLayouts.Add("8x10NoName");
                                lLayouts.Add("5x7");
                                lLayouts.Add("Additional");
                                lLayouts.Add("Copyright");

                                foreach (string s in lLayouts)
                                {
                                    if (s == "8x10wName")
                                    {
                                        if (bInitialJobIDAssigned == true)
                                        {
                                            // This will prevent skipping the initial gathered JobID when assigning to a rendered product.
                                            iJobID += +1;
                                        }
                                        else if (bInitialJobIDAssigned != true)
                                        {
                                            bInitialJobIDAssigned = true;
                                        }

                                        // Some layouts need swapped due to artwork that is present with text but not with no text.
                                        // I added these layouts to a table for easy real time handling.

                                        DataTable dTblExportDefSwaps = new DataTable("dTblExportDefSwaps");
                                        string sCommText = "SELECT * FROM [ExportDef Swaps] WHERE [WithText] = '1'";
                                        bool bEDSwapFound = false;

                                        TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblExportDefSwaps);

                                        if (dTblExportDefSwaps.Rows.Count > 0)
                                        {
                                            foreach (DataRow dRowExportDefSwaps in dTblExportDefSwaps.Rows)
                                            {
                                                string sOriginalED = Convert.ToString(dRowExportDefSwaps["OriginalExportDef"]).Trim();
                                                string sSwappedED = Convert.ToString(dRowExportDefSwaps["SwappedExportDef"]).Trim();

                                                if (sExportDefOriginal == sOriginalED)
                                                {
                                                    sExportDef = sSwappedED;

                                                    bEDSwapFound = true;

                                                    bHaveFile = false;
                                                    TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);                                                    
                                                }
                                            }

                                            if (bEDSwapFound != true)
                                            {
                                                sExportDef = sExportDefOriginal;

                                                bHaveFile = false;
                                                TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);
                                            }
                                        }
                                        else if (dTblExportDefSwaps.Rows.Count == 0)
                                        {
                                            string sStop = string.Empty;
                                        }

                                        #region commented code

                                        //if (sExportDefOriginal == "CKB2.txt")
                                        //{
                                        //    sExportDef = "CKB.txt";

                                        //    bHaveFile = false;
                                        //    TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);
                                        //}
                                        //else if (sExportDefOriginal != "CKB2.txt")
                                        //{
                                        //    sExportDef = sExportDefOriginal;

                                        //    bHaveFile = false;
                                        //    TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);
                                        //}

                                        #endregion

                                        if (bHaveFile == true)
                                        {
                                            iRenderedCount += +1;

                                            sAPSDEST = sRenderedPath + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + "-" + iRenderedCount + "-8x10wname.JPG";
                                            sExportDefFile = sExportDefPathDone + "DiscProcessor" + "_" + sRefNum + "_" + sProdNum + "_" + sFrameNum + "-" + iRenderedCount + "-8x10wname.txt";

                                            string text = File.ReadAllText(sExportDefPath + sExportDef);

                                            text = text.Replace("APSPATH", sPath);
                                            text = text.Replace("APSDEST", sAPSDEST);

                                            if (text.Contains("APSBGID"))
                                            {
                                                text = text.Replace("APSBGID", sGSBkGrnd);
                                            }
                                            if (text.Contains("APSTEXT"))
                                            {
                                                text = text.Replace("APSTEXT", sNameOn);
                                            }
                                            if (text.Contains("APSYEAR"))
                                            {
                                                text = text.Replace("APSYEAR", sYearOn);
                                            }
                                            if (text.Contains("APSCROP"))
                                            {
                                                string sCrop = string.Empty;

                                                TM.GetCrop(ref sCrop, sUniqueID, sProdNum, sFrameNum, sRefNum);

                                                text = text.Replace("APSCROP", sCrop);
                                            }

                                            iLoops += +1;

                                            File.WriteAllText(sExportDefFile, text);

                                            dTblJob.Rows.Add(iRenderedCount, sUniqueID, iBatchID, iJobID, sRefNum, sFrameNum, sExportDef, sExportDefFile, sAPSDEST, sNameOn, sYearOn, sMrgJPG, sCustNum);
                                        }
                                        else if (bHaveFile != true)
                                        {
                                            bGoodResults = false;

                                            string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ No ExportDef file located. ]";

                                            TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sExportDefSitting, bSittingBased);

                                            string sStatus = "90";

                                            TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                        }
                                    }
                                    else if (s == "8x10NoName")
                                    {
                                        // Some layouts need swapped due to artwork that is present with text but not with no text.
                                        // I added these layouts to a table for easy real time handling.

                                        DataTable dTblExportDefSwaps = new DataTable("dTblExportDefSwaps");
                                        string sCommText = "SELECT * FROM [ExportDef Swaps] WHERE [WithText] = '0'";
                                        bool bEDSwapFound = false;

                                        TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblExportDefSwaps);

                                        if (dTblExportDefSwaps.Rows.Count > 0)
                                        {
                                            foreach (DataRow dRowExportDefSwaps in dTblExportDefSwaps.Rows)
                                            {
                                                string sOriginalED = Convert.ToString(dRowExportDefSwaps["OriginalExportDef"]).Trim();
                                                string sSwappedED = Convert.ToString(dRowExportDefSwaps["SwappedExportDef"]).Trim();

                                                if (sExportDefOriginal == sOriginalED)
                                                {
                                                    sExportDef = sSwappedED;

                                                    bEDSwapFound = true;

                                                    bHaveFile = false;
                                                    TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);
                                                }
                                            }

                                            if (bEDSwapFound != true)
                                            {
                                                sExportDef = sExportDefOriginal;

                                                bHaveFile = false;
                                                TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);
                                            }
                                        }
                                        else if (dTblExportDefSwaps.Rows.Count == 0)
                                        {
                                            string sStop = string.Empty;
                                        }

                                        #region commented code

                                        //if (sExportDefOriginal == "CKB.txt")
                                        //{
                                        //    sExportDef = "CKB2.txt";

                                        //    bHaveFile = false;
                                        //    TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);
                                        //}
                                        //else if (sExportDefOriginal != "CKB.txt")
                                        //{
                                        //    sExportDef = sExportDefOriginal;

                                        //    bHaveFile = false;
                                        //    TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);
                                        //}

                                        #endregion

                                        //bHaveFile = false;
                                        //TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);

                                        if (bHaveFile == true)
                                        {
                                            iRenderedCount += +1;
                                            iJobID += +1;

                                            sAPSDEST = sRenderedPath + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + "-" + iRenderedCount + "-8x10noname.JPG";
                                            sExportDefFile = sExportDefPathDone + "DiscProcessor" + "_" + sRefNum + "_" + sProdNum + "_" + sFrameNum + "-" + iRenderedCount + "-8x10noname.txt";

                                            string text = File.ReadAllText(sExportDefPath + sExportDef);

                                            text = text.Replace("APSPATH", sPath);
                                            text = text.Replace("APSDEST", sAPSDEST);

                                            if (text.Contains("APSBGID"))
                                            {
                                                text = text.Replace("APSBGID", sGSBkGrnd);
                                            }
                                            if (text.Contains("APSTEXT"))
                                            {
                                                text = text.Replace("APSTEXT", "");
                                            }
                                            if (text.Contains("APSYEAR"))
                                            {
                                                text = text.Replace("APSYEAR", "");
                                            }
                                            if (text.Contains("APSCROP"))
                                            {
                                                string sCrop = string.Empty;

                                                TM.GetCrop(ref sCrop, sUniqueID, sProdNum, sFrameNum, sRefNum);

                                                text = text.Replace("APSCROP", sCrop);
                                            }

                                            iLoops += +1;

                                            File.WriteAllText(sExportDefFile, text);

                                            dTblJob.Rows.Add(iRenderedCount, sUniqueID, iBatchID, iJobID, sRefNum, sFrameNum, sExportDef, sExportDefFile, sAPSDEST, sNameOn, sYearOn, sMrgJPG, sCustNum);
                                        }
                                        else if (bHaveFile != true)
                                        {
                                            bGoodResults = false;

                                            string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ No ExportDef file located. ]";

                                            TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sExportDefSitting, bSittingBased);

                                            string sStatus = "90";

                                            TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                        }
                                    }
                                    else if (s == "5x7")
                                    {
                                        sExportDef = "04" + sExportDefOriginal;

                                        bHaveFile = false;
                                        TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);

                                        if (bHaveFile == true)
                                        {
                                            iRenderedCount += +1;
                                            iJobID += +1;

                                            sAPSDEST = sRenderedPath + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + "-" + iRenderedCount + "-5x7.JPG";
                                            sExportDefFile = sExportDefPathDone + "DiscProcessor" + "_" + sRefNum + "_" + sProdNum + "_" + sFrameNum + "-" + iRenderedCount + "-5x7.txt";

                                            string text = File.ReadAllText(sExportDefPath + sExportDef);

                                            text = text.Replace("APSPATH", sPath);
                                            text = text.Replace("APSDEST", sAPSDEST);

                                            if (text.Contains("APSBGID"))
                                            {
                                                text = text.Replace("APSBGID", sGSBkGrnd);
                                            }
                                            if (text.Contains("APSTEXT"))
                                            {
                                                text = text.Replace("APSTEXT", sNameOn);
                                            }
                                            if (text.Contains("APSYEAR"))
                                            {
                                                text = text.Replace("APSYEAR", sYearOn);
                                            }
                                            if (text.Contains("APSCROP"))
                                            {
                                                string sCrop = string.Empty;

                                                TM.GetCrop(ref sCrop, sUniqueID, sProdNum, sFrameNum, sRefNum);

                                                text = text.Replace("APSCROP", sCrop);
                                            }

                                            iLoops += +1;

                                            File.WriteAllText(sExportDefFile, text);

                                            dTblJob.Rows.Add(iRenderedCount, sUniqueID, iBatchID, iJobID, sRefNum, sFrameNum, sExportDef, sExportDefFile, sAPSDEST, sNameOn, sYearOn, sMrgJPG, sCustNum);
                                        }
                                        else if (bHaveFile != true)
                                        {
                                            bGoodResults = false;

                                            string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ No ExportDef file located. ]";

                                            TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sExportDefSitting, bSittingBased);

                                            string sStatus = "90";

                                            TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                        }
                                    }
                                    else if (s == "Additional" && bStylesAndBGs == true)
                                    {
                                        if (iLoops == iCount)
                                        {
                                            string sPreviousGSBkGrnd01 = string.Empty;
                                            string sPreviousGSBkGrnd02 = string.Empty;

                                            foreach (DataRow dRow in dTbl2.Rows)
                                            {
                                                sGSBkGrnd = Convert.ToString(dRow["GSBackground"]).Trim();

                                                if (sGSBkGrnd != string.Empty || sGSBkGrnd != "")
                                                {
                                                    if (sGSBkGrnd != sPreviousGSBkGrnd01)
                                                    {
                                                        sPreviousGSBkGrnd01 = sGSBkGrnd;

                                                        sExportDef = "COLOR.txt";

                                                        bHaveFile = false;
                                                        TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);

                                                        if (bHaveFile == true)
                                                        {
                                                            iRenderedCount += +1;
                                                            iJobID += +1;

                                                            sAPSDEST = sRenderedPath + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + "-" + iRenderedCount + "-Color8x10.JPG";
                                                            sExportDefFile = sExportDefPathDone + "DiscProcessor" + "_" + sRefNum + "_" + sProdNum + "_" + sFrameNum + "-" + iRenderedCount + "-Color8x10.txt";

                                                            string text = File.ReadAllText(sExportDefPath + sExportDef);

                                                            text = text.Replace("APSPATH", sPath);
                                                            text = text.Replace("APSDEST", sAPSDEST);

                                                            if (text.Contains("APSBGID"))
                                                            {
                                                                text = text.Replace("APSBGID", sGSBkGrnd);
                                                            }

                                                            File.WriteAllText(sExportDefFile, text);

                                                            dTblJob.Rows.Add(iRenderedCount, sUniqueID, iBatchID, iJobID, sRefNum, sFrameNum, sExportDef, sExportDefFile, sAPSDEST, sNameOn, sYearOn, sMrgJPG, sCustNum);
                                                        }
                                                        else if (bHaveFile != true)
                                                        {
                                                            bGoodResults = false;

                                                            string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ No ExportDef file located. ]";

                                                            TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sExportDefSitting, bSittingBased);

                                                            string sStatus = "90";

                                                            TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                                        }
                                                    }
                                                    else if (sGSBkGrnd == sPreviousGSBkGrnd01)
                                                    {

                                                    }
                                                }
                                                else if (sGSBkGrnd == string.Empty)
                                                {
                                                    string sStop = string.Empty;
                                                }

                                                sGSBkGrnd = Convert.ToString(dRow["GSBackground"]).Trim();

                                                if (sGSBkGrnd != string.Empty || sGSBkGrnd != "")
                                                {
                                                    if (sGSBkGrnd != sPreviousGSBkGrnd02)
                                                    {
                                                        sPreviousGSBkGrnd02 = sGSBkGrnd;

                                                        sExportDef = "BW.TXT";

                                                        bHaveFile = false;
                                                        TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);

                                                        if (bHaveFile == true)
                                                        {
                                                            iRenderedCount += +1;
                                                            iJobID += +1;

                                                            sAPSDEST = sRenderedPath + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + "-" + iRenderedCount + "-BW8x10.JPG";
                                                            sExportDefFile = sExportDefPathDone + "DiscProcessor" + "_" + sRefNum + "_" + sProdNum + "_" + sFrameNum + "-" + iRenderedCount + "-BW8x10.txt";

                                                            string text = File.ReadAllText(sExportDefPath + sExportDef);

                                                            text = text.Replace("APSPATH", sPath);
                                                            text = text.Replace("APSDEST", sAPSDEST);

                                                            if (text.Contains("APSBGID"))
                                                            {
                                                                text = text.Replace("APSBGID", sGSBkGrnd);
                                                            }

                                                            File.WriteAllText(sExportDefFile, text);

                                                            dTblJob.Rows.Add(iRenderedCount, sUniqueID, iBatchID, iJobID, sRefNum, sFrameNum, sExportDef, sExportDefFile, sAPSDEST, sNameOn, sYearOn, sMrgJPG, sCustNum);
                                                        }
                                                        else if (bHaveFile != true)
                                                        {
                                                            bGoodResults = false;

                                                            string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ No ExportDef file located. ]";

                                                            TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sExportDefSitting, bSittingBased);

                                                            string sStatus = "90";

                                                            TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                                        }
                                                    }
                                                    else if (sGSBkGrnd == sPreviousGSBkGrnd02)
                                                    {

                                                    }
                                                }
                                                else if (sGSBkGrnd == string.Empty)
                                                {
                                                    string sStop = string.Empty;
                                                }
                                            }
                                        }
                                        else if (iLoops != iCount)
                                        {

                                        }
                                    }
                                    else if (s == "Additional" && bStylesAndBGs != true)
                                    {
                                        if (iLoops == iCount)
                                        {
                                            sExportDef = "COLOR.txt";

                                            bHaveFile = false;
                                            TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);

                                            if (bHaveFile == true)
                                            {
                                                iRenderedCount += +1;
                                                iJobID += +1;

                                                sAPSDEST = sRenderedPath + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + "-" + iRenderedCount + "-Color8x10.JPG";
                                                sExportDefFile = sExportDefPathDone + "DiscProcessor" + "_" + sRefNum + "_" + sProdNum + "_" + sFrameNum + "-" + iRenderedCount + "-Color8x10.txt";

                                                string text = File.ReadAllText(sExportDefPath + sExportDef);

                                                text = text.Replace("APSPATH", sPath);
                                                text = text.Replace("APSDEST", sAPSDEST);

                                                File.WriteAllText(sExportDefFile, text);

                                                dTblJob.Rows.Add(iRenderedCount, sUniqueID, iBatchID, iJobID, sRefNum, sFrameNum, sExportDef, sExportDefFile, sAPSDEST, sNameOn, sYearOn, sMrgJPG, sCustNum);
                                            }
                                            else if (bHaveFile != true)
                                            {
                                                bGoodResults = false;

                                                string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ No ExportDef file located. ]";

                                                TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sExportDefSitting, bSittingBased);

                                                string sStatus = "90";

                                                TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                            }

                                            sExportDef = "BW.TXT";

                                            bHaveFile = false;
                                            TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);

                                            if (bHaveFile == true)
                                            {
                                                iRenderedCount += +1;
                                                iJobID += +1;

                                                sAPSDEST = sRenderedPath + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + "-" + iRenderedCount + "-BW8x10.JPG";
                                                sExportDefFile = sExportDefPathDone + "DiscProcessor" + "_" + sRefNum + "_" + sProdNum + "_" + sFrameNum + "-" + iRenderedCount + "-BW8x10.txt";

                                                string text = File.ReadAllText(sExportDefPath + sExportDef);

                                                text = text.Replace("APSPATH", sPath);
                                                text = text.Replace("APSDEST", sAPSDEST);

                                                File.WriteAllText(sExportDefFile, text);

                                                dTblJob.Rows.Add(iRenderedCount, sUniqueID, iBatchID, iJobID, sRefNum, sFrameNum, sExportDef, sExportDefFile, sAPSDEST, sNameOn, sYearOn, sMrgJPG, sCustNum);
                                            }
                                            else if (bHaveFile != true)
                                            {
                                                bGoodResults = false;

                                                string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ No ExportDef file located. ]";

                                                TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sExportDefSitting, bSittingBased);

                                                string sStatus = "90";

                                                TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                            }
                                        }
                                        else if (iLoops != iCount)
                                        {

                                        }
                                    }
                                    else if (s == "Copyright" && (iLoops == iCount))
                                    {
                                        if (sCustNum == "58241")
                                        {
                                            TM.GetOriginalCustomerFromIMQ(ref sCustNum, sProdNum);
                                        }

                                        iRenderedCount += +1;
                                        iJobID += +1;

                                        sAPSDEST = sRenderedPath + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + "-" + iRenderedCount + "-Copyright.JPG";
                                        sExportDefFile = sExportDefPathDone + "DiscProcessor" + "_" + sRefNum + "_" + sProdNum + "_" + sFrameNum + "-" + iRenderedCount + "-Copyright.txt";

                                        string[] sFiles = Directory.GetFiles(sExportDefPath);
                                        string sFilePath = string.Empty;
                                        bool bCopyrightFound = false;

                                        foreach (string sFile in sFiles)
                                        {
                                            sFilePath = Path.GetFileName(sFile);

                                            if (sFilePath == sCustNum + " Copyright.txt")
                                            {
                                                sExportDef = sFilePath;
                                                bCopyrightFound = true;
                                            }
                                            else if (sFilePath == sCustNum + " Copyright.TXT")
                                            {
                                                sExportDef = sFilePath;
                                                bCopyrightFound = true;
                                            }
                                        }
                                        if (bCopyrightFound == true)
                                        {
                                            bGoodResults = true;

                                            string text = File.ReadAllText(sExportDefPath + sExportDef);

                                            text = text.Replace("APSPATH", sPath);
                                            text = text.Replace("APSDEST", sAPSDEST);

                                            File.WriteAllText(sExportDefFile, text);

                                            dTblJob.Rows.Add(iRenderedCount, sUniqueID, iBatchID, iJobID, sRefNum, sFrameNum, sExportDef, sExportDefFile, sAPSDEST, sNameOn, sYearOn, sMrgJPG, sCustNum);
                                        }
                                        else if (bCopyrightFound != true)
                                        {
                                            bGoodResults = false;

                                            string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ No copyright located. ]";

                                            TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sExportDefSitting, bSittingBased);

                                            string sStatus = "90";

                                            TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);

                                            TM.RemoveOrphanedExportDefFiles(dTblJob);
                                        }
                                    }
                                }
                            }
                            else if (bLockedFile == true || bGatherIDsSuccess != true)
                            {
                                string sStop = string.Empty;
                            }
                        }
                        else if (bSuccess == false)
                        {
                            string sStop = string.Empty;
                        }
                    }
                    else if (bSuccess != true)
                    {
                        string sStop = string.Empty;
                    }
                }
                else if (bSuccess != true)
                {
                    string sStop = string.Empty;
                }
            }
            catch(Exception ex)
            {
                TM.SaveExceptionToDB(ex);
                bGoodResults = false;
            }
        }

        // Generate the required ICD ExportDef files for current order.
        private void MEGExportDefModifying(string sPath, string sPkgTag, string sRefNum, string sExportDef, string sProdNum, string sFrameNum, int iJobIDsNeeded, string sYearOn, string sNameOn, string sUniqueID, ref DataTable dTblJob, ref bool bCreated, ref int iRenderedCount, ref bool bIDsGathered, ref int iJobID, ref int iBatchID, ref int iLoops, int iCount, ref bool bInitialJobIDAssigned, string sCustNum, ref bool bGoodResults, ref bool bInitialPass01, string sGSBkGrnd, DataTable dTbl2, string sDiscType, string sDP2Mask, string sExportDefSitting, bool bSittingBased)
        {
            try
            {
                string sRenderedPath = string.Empty;

                string sLabel = "MEGRenderedPath";
                string sValue = "Value";
                string sVariable = string.Empty;
                bool bSuccess = true;

                TM.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                if (bSuccess == true)
                {
                    sRenderedPath = sVariable;

                    bool bGatherIDsSuccess = true;

                    string sExportDefPath = string.Empty;
                    string sExportDefPathDone = string.Empty;

                    sLabel = "ExportDefPath";
                    sValue = "Value";
                    sVariable = string.Empty;
                    bSuccess = true;

                    TM.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                    if (bSuccess == true)
                    {
                        sExportDefPath = sVariable;

                        sLabel = "ExportDefPathDone";
                        sValue = "Value";
                        sVariable = string.Empty;
                        bSuccess = true;

                        TM.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                        if (bSuccess == true)
                        {
                            sExportDefPathDone = sVariable;

                            if (!Directory.Exists(sExportDefPathDone))
                            {
                                Directory.CreateDirectory(sExportDefPathDone);
                            }

                            string sMrgJPG = Path.GetFileName(sPath);
                            string sExportDefFile = string.Empty;
                            string sAPSDEST = string.Empty;
                            string sExportDefOriginal = sExportDef;
                            bool bLockedFile = false;
                            int iLastJobID = 0;
                            bool bHaveFile = false;

                            if (bInitialPass01 == true)
                            {
                                string sText = "[Generating ExportDef files for reference number " + sRefNum + " and frame number " + sFrameNum + ".]";
                                this.LogText(sText);

                                bInitialPass01 = false;
                            }

                            if (bIDsGathered == false)
                            {
                                this.LockFile(ref iBatchID, ref iJobID, ref iJobIDsNeeded, ref bLockedFile, ref bGatherIDsSuccess);
                            }
                            else if (bIDsGathered != false)
                            {

                            }

                            iLastJobID = iJobID + iJobIDsNeeded;

                            if (bLockedFile != true && bGatherIDsSuccess == true)
                            {
                                bIDsGathered = true;

                                // Create a datatable to store the entire records worth of needed exportdef files which then is looped through and pushed into the dp2.jobqueue table.

                                if (bCreated == false)
                                {
                                    dTblJob.Columns.Add("Count", typeof(int)); // iRenderedCount
                                    dTblJob.Columns.Add("UniqueID", typeof(string)); // sUniqueID
                                    dTblJob.Columns.Add("BatchID", typeof(int)); // iBatchID
                                    dTblJob.Columns.Add("JobID", typeof(int)); // iJobID
                                    dTblJob.Columns.Add("RefNum", typeof(string)); // sRefNum
                                    dTblJob.Columns.Add("FrameNum", typeof(string)); // sFrameNum
                                    dTblJob.Columns.Add("ExportDefFile", typeof(string)); // sExportDef
                                    dTblJob.Columns.Add("SavedExportDefPath", typeof(string)); // sExportDefFile                        
                                    dTblJob.Columns.Add("RenderedImageLocation", typeof(string)); // sAPSDEST
                                    dTblJob.Columns.Add("NameOn", typeof(string)); // sNameOn
                                    dTblJob.Columns.Add("YearOn", typeof(string)); // sYearOn
                                    dTblJob.Columns.Add("ImageName", typeof(string)); // sMrgJPG
                                    dTblJob.Columns.Add("CustNum", typeof(string)); // sCustNum

                                    bCreated = true;
                                }
                                else if (bCreated != false)
                                {

                                }

                                List<string> lLayouts = new List<string>();
                                lLayouts.Add("8x10wName");
                                lLayouts.Add("8x10NoName");
                                lLayouts.Add("5x7");
                                lLayouts.Add("Additional");
                                lLayouts.Add("Calendar");
                                lLayouts.Add("Copyright");

                                foreach (string s in lLayouts)
                                {
                                    if (s == "8x10wName")
                                    {
                                        if (bInitialJobIDAssigned == true)
                                        {
                                            // This will prevent skipping the initial gathered JobID when assigning to a rendered product.
                                            iJobID += +1;
                                        }
                                        else if (bInitialJobIDAssigned != true)
                                        {
                                            bInitialJobIDAssigned = true;
                                        }

                                        // Some layouts need swapped due to artwork that is present with text but not with no text.
                                        // I added these layouts to a table for easy real time handling.

                                        DataTable dTblExportDefSwaps = new DataTable("dTblExportDefSwaps");
                                        string sCommText = "SELECT * FROM [ExportDef Swaps] WHERE [WithText] = '1'";
                                        bool bEDSwapFound = false;

                                        TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblExportDefSwaps);

                                        if (dTblExportDefSwaps.Rows.Count > 0)
                                        {
                                            foreach (DataRow dRowExportDefSwaps in dTblExportDefSwaps.Rows)
                                            {
                                                string sOriginalED = Convert.ToString(dRowExportDefSwaps["OriginalExportDef"]).Trim();
                                                string sSwappedED = Convert.ToString(dRowExportDefSwaps["SwappedExportDef"]).Trim();

                                                if (sExportDefOriginal == sOriginalED)
                                                {
                                                    sExportDef = sSwappedED;

                                                    bEDSwapFound = true;

                                                    bHaveFile = false;
                                                    TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);
                                                }
                                            }

                                            if (bEDSwapFound != true)
                                            {
                                                sExportDef = sExportDefOriginal;

                                                bHaveFile = false;
                                                TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);
                                            }
                                        }
                                        else if (dTblExportDefSwaps.Rows.Count == 0)
                                        {
                                            string sStop = string.Empty;
                                        }

                                        #region commented code

                                        //if (sExportDefOriginal == "CKB2.txt")
                                        //{
                                        //    sExportDef = "CKB.txt";

                                        //    bHaveFile = false;
                                        //    TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);
                                        //}
                                        //else if (sExportDefOriginal != "CKB2.txt")
                                        //{
                                        //    sExportDef = sExportDefOriginal;

                                        //    bHaveFile = false;
                                        //    TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);
                                        //}

                                        #endregion

                                        if (bHaveFile == true)
                                        {
                                            iRenderedCount += +1;

                                            sAPSDEST = sRenderedPath + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + "-" + iRenderedCount + "-8x10wname.JPG";
                                            sExportDefFile = sExportDefPathDone + "DiscProcessor" + "_" + sRefNum + "_" + sProdNum + "_" + sFrameNum + "-" + iRenderedCount + "-8x10wname.txt";

                                            string text = File.ReadAllText(sExportDefPath + sExportDef);

                                            text = text.Replace("APSPATH", sPath);
                                            text = text.Replace("APSDEST", sAPSDEST);

                                            if (text.Contains("APSBGID"))
                                            {
                                                text = text.Replace("APSBGID", sGSBkGrnd);
                                            }
                                            if (text.Contains("APSTEXT"))
                                            {
                                                text = text.Replace("APSTEXT", sNameOn);
                                            }
                                            if (text.Contains("APSYEAR"))
                                            {
                                                text = text.Replace("APSYEAR", sYearOn);
                                            }
                                            if (text.Contains("APSCROP"))
                                            {
                                                string sCrop = string.Empty;

                                                TM.GetCrop(ref sCrop, sUniqueID, sProdNum, sFrameNum, sRefNum);

                                                text = text.Replace("APSCROP", sCrop);
                                            }

                                            iLoops += +1;

                                            File.WriteAllText(sExportDefFile, text);

                                            dTblJob.Rows.Add(iRenderedCount, sUniqueID, iBatchID, iJobID, sRefNum, sFrameNum, sExportDef, sExportDefFile, sAPSDEST, sNameOn, sYearOn, sMrgJPG, sCustNum);
                                        }
                                        else if (bHaveFile != true)
                                        {
                                            bGoodResults = false;

                                            string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ No ExportDef file located. ]";

                                            TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sExportDefSitting, bSittingBased);

                                            string sStatus = "90";

                                            TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                        }
                                    }
                                    else if (s == "8x10NoName")
                                    {
                                        // Some layouts need swapped due to artwork that is present with text but not with no text.
                                        // I added these layouts to a table for easy real time handling.

                                        DataTable dTblExportDefSwaps = new DataTable("dTblExportDefSwaps");
                                        string sCommText = "SELECT * FROM [ExportDef Swaps] WHERE [WithText] = '0'";
                                        bool bEDSwapFound = false;

                                        TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblExportDefSwaps);

                                        if (dTblExportDefSwaps.Rows.Count > 0)
                                        {
                                            foreach (DataRow dRowExportDefSwaps in dTblExportDefSwaps.Rows)
                                            {
                                                string sOriginalED = Convert.ToString(dRowExportDefSwaps["OriginalExportDef"]).Trim();
                                                string sSwappedED = Convert.ToString(dRowExportDefSwaps["SwappedExportDef"]).Trim();

                                                if (sExportDefOriginal == sOriginalED)
                                                {
                                                    sExportDef = sSwappedED;

                                                    bEDSwapFound = true;

                                                    bHaveFile = false;
                                                    TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);
                                                }
                                            }

                                            if (bEDSwapFound != true)
                                            {
                                                sExportDef = sExportDefOriginal;

                                                bHaveFile = false;
                                                TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);
                                            }
                                        }
                                        else if (dTblExportDefSwaps.Rows.Count == 0)
                                        {
                                            string sStop = string.Empty;
                                        }

                                        #region commented code

                                        //if (sExportDefOriginal == "CKB.txt")
                                        //{
                                        //    sExportDef = "CKB2.txt";

                                        //    bHaveFile = false;
                                        //    TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);
                                        //}
                                        //else if (sExportDefOriginal != "CKB.txt")
                                        //{
                                        //    sExportDef = sExportDefOriginal;

                                        //    bHaveFile = false;
                                        //    TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);
                                        //}

                                        #endregion

                                        //bHaveFile = false;
                                        //TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);

                                        if (bHaveFile == true)
                                        {
                                            iRenderedCount += +1;
                                            iJobID += +1;

                                            sAPSDEST = sRenderedPath + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + "-" + iRenderedCount + "-8x10noname.JPG";
                                            sExportDefFile = sExportDefPathDone + "DiscProcessor" + "_" + sRefNum + "_" + sProdNum + "_" + sFrameNum + "-" + iRenderedCount + "-8x10noname.txt";

                                            string text = File.ReadAllText(sExportDefPath + sExportDef);

                                            text = text.Replace("APSPATH", sPath);
                                            text = text.Replace("APSDEST", sAPSDEST);

                                            if (text.Contains("APSBGID"))
                                            {
                                                text = text.Replace("APSBGID", sGSBkGrnd);
                                            }
                                            if (text.Contains("APSTEXT"))
                                            {
                                                text = text.Replace("APSTEXT", "");
                                            }
                                            if (text.Contains("APSYEAR"))
                                            {
                                                text = text.Replace("APSYEAR", "");
                                            }
                                            if (text.Contains("APSCROP"))
                                            {
                                                string sCrop = string.Empty;

                                                TM.GetCrop(ref sCrop, sUniqueID, sProdNum, sFrameNum, sRefNum);

                                                text = text.Replace("APSCROP", sCrop);
                                            }

                                            iLoops += +1;

                                            File.WriteAllText(sExportDefFile, text);

                                            dTblJob.Rows.Add(iRenderedCount, sUniqueID, iBatchID, iJobID, sRefNum, sFrameNum, sExportDef, sExportDefFile, sAPSDEST, sNameOn, sYearOn, sMrgJPG, sCustNum);
                                        }
                                        else if (bHaveFile != true)
                                        {
                                            bGoodResults = false;

                                            string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ No ExportDef file located. ]";

                                            TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sExportDefSitting, bSittingBased);

                                            string sStatus = "90";

                                            TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                        }
                                    }
                                    else if (s == "5x7")
                                    {
                                        sExportDef = "04" + sExportDefOriginal;

                                        bHaveFile = false;
                                        TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);

                                        if (bHaveFile == true)
                                        {
                                            iRenderedCount += +1;
                                            iJobID += +1;

                                            sAPSDEST = sRenderedPath + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + "-" + iRenderedCount + "-5x7.JPG";
                                            sExportDefFile = sExportDefPathDone + "DiscProcessor" + "_" + sRefNum + "_" + sProdNum + "_" + sFrameNum + "-" + iRenderedCount + "-5x7.txt";

                                            string text = File.ReadAllText(sExportDefPath + sExportDef);

                                            text = text.Replace("APSPATH", sPath);
                                            text = text.Replace("APSDEST", sAPSDEST);

                                            if (text.Contains("APSBGID"))
                                            {
                                                text = text.Replace("APSBGID", sGSBkGrnd);
                                            }
                                            if (text.Contains("APSTEXT"))
                                            {
                                                text = text.Replace("APSTEXT", sNameOn);
                                            }
                                            if (text.Contains("APSYEAR"))
                                            {
                                                text = text.Replace("APSYEAR", sYearOn);
                                            }
                                            if (text.Contains("APSCROP"))
                                            {
                                                string sCrop = string.Empty;

                                                TM.GetCrop(ref sCrop, sUniqueID, sProdNum, sFrameNum, sRefNum);

                                                text = text.Replace("APSCROP", sCrop);
                                            }

                                            iLoops += +1;

                                            File.WriteAllText(sExportDefFile, text);

                                            dTblJob.Rows.Add(iRenderedCount, sUniqueID, iBatchID, iJobID, sRefNum, sFrameNum, sExportDef, sExportDefFile, sAPSDEST, sNameOn, sYearOn, sMrgJPG, sCustNum);
                                        }
                                        else if (bHaveFile != true)
                                        {
                                            bGoodResults = false;

                                            string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ No ExportDef file located. ]";

                                            TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sExportDefSitting, bSittingBased);

                                            string sStatus = "90";

                                            TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                        }
                                    }
                                    else if (s == "Additional")
                                    {
                                        if (iLoops == iCount)
                                        {
                                            DataTable dTblMEGAdds = new DataTable("dTblMedAdds");
                                            string sCommText = "SELECT * FROM [MEG Additional Renders]";

                                            TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblMEGAdds);

                                            if (dTblMEGAdds.Rows.Count > 0)
                                            {
                                                foreach (DataRow dRowMEGAdds in dTblMEGAdds.Rows)
                                                {
                                                    sExportDef = Convert.ToString(dRowMEGAdds["Render Layout"]).Trim();

                                                    bHaveFile = false;
                                                    TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);

                                                    if (bHaveFile == true)
                                                    {
                                                        iRenderedCount += +1;
                                                        iJobID += +1;

                                                        sAPSDEST = sRenderedPath + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + "-"
                                                            + iRenderedCount + "-Additional.JPG";
                                                        sExportDefFile = sExportDefPathDone + "DiscProcessor" + "_" + sRefNum + "_" + sProdNum + "_" + sFrameNum + "-" 
                                                            + iRenderedCount + "-Additional.txt";

                                                        string text = File.ReadAllText(sExportDefPath + sExportDef);

                                                        text = text.Replace("APSPATH", sPath);
                                                        text = text.Replace("APSDEST", sAPSDEST);

                                                        if (text.Contains("APSBGID"))
                                                        {
                                                            text = text.Replace("APSBGID", sGSBkGrnd);
                                                        }
                                                        if (text.Contains("APSTEXT"))
                                                        {
                                                            text = text.Replace("APSTEXT", sNameOn);
                                                        }
                                                        if (text.Contains("APSYEAR"))
                                                        {
                                                            text = text.Replace("APSYEAR", sYearOn);
                                                        }

                                                        File.WriteAllText(sExportDefFile, text);

                                                        dTblJob.Rows.Add(iRenderedCount, sUniqueID, iBatchID, iJobID, sRefNum, sFrameNum, sExportDef, sExportDefFile, sAPSDEST, sNameOn, sYearOn, sMrgJPG, sCustNum);
                                                    }
                                                    else if (bHaveFile != true)
                                                    {
                                                        bGoodResults = false;

                                                        string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ No ExportDef file located. ]";

                                                        TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sExportDefSitting, bSittingBased);

                                                        string sStatus = "90";

                                                        TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                                    }
                                                }
                                            }
                                            else if (dTblMEGAdds.Rows.Count == 0)
                                            {
                                                string sStop = string.Empty;
                                            }
                                        }
                                        else if (iLoops != iCount)
                                        {

                                        }
                                    }
                                    else if (s == "Calendar")
                                    {
                                        if (iLoops == iCount)
                                        {
                                            if (sGSBkGrnd != string.Empty || sGSBkGrnd != "")
                                            {
                                                DataTable dTblGSCalRenders = new DataTable("dTblGSCalRenders");
                                                string sCommText = "SELECT * FROM [MEG GS Calendar Renders]";

                                                TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblGSCalRenders);

                                                if (dTblGSCalRenders.Rows.Count > 0)
                                                {
                                                    foreach (DataRow dRowGSCalRenders in dTblGSCalRenders.Rows)
                                                    {
                                                        sExportDef = Convert.ToString(dRowGSCalRenders["Render Layout"]).Trim();

                                                        string sMonth = Path.GetFileNameWithoutExtension(sExportDef);
                                                        sMonth = sMonth.Substring(sExportDef.IndexOf("_")).Trim();
                                                        sMonth = sMonth.Replace("_", "").Trim();

                                                        bHaveFile = false;
                                                        TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);

                                                        if (bHaveFile == true)
                                                        {
                                                            iRenderedCount += +1;
                                                            iJobID += +1;

                                                            sAPSDEST = sRenderedPath + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + "-"
                                                                + iRenderedCount + "-" + sMonth + "Calendar.JPG";
                                                            sExportDefFile = sExportDefPathDone + "DiscProcessor" + "_" + sRefNum + "_" + sProdNum + "_" + sFrameNum + "-"
                                                                + iRenderedCount + "- " + sMonth + "Calendar.txt";

                                                            string text = File.ReadAllText(sExportDefPath + sExportDef);

                                                            text = text.Replace("APSPATH", sPath);
                                                            text = text.Replace("APSDEST", sAPSDEST);

                                                            if (text.Contains("APSBGID"))
                                                            {
                                                                text = text.Replace("APSBGID", sGSBkGrnd);
                                                            }
                                                            if (text.Contains("APSTEXT"))
                                                            {
                                                                text = text.Replace("APSTEXT", sNameOn);
                                                            }
                                                            if (text.Contains("APSYEAR"))
                                                            {
                                                                text = text.Replace("APSYEAR", sYearOn);
                                                            }

                                                            File.WriteAllText(sExportDefFile, text);

                                                            dTblJob.Rows.Add(iRenderedCount, sUniqueID, iBatchID, iJobID, sRefNum, sFrameNum, sExportDef, sExportDefFile, sAPSDEST, sNameOn, sYearOn, sMrgJPG, sCustNum);
                                                        }
                                                        else if (bHaveFile != true)
                                                        {
                                                            bGoodResults = false;

                                                            string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ No ExportDef file located. ]";

                                                            TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sExportDefSitting, bSittingBased);

                                                            string sStatus = "90";

                                                            TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                                        }
                                                    }
                                                }
                                                else if (dTblGSCalRenders.Rows.Count == 0)
                                                {
                                                    string sStop = string.Empty;
                                                }
                                            }
                                            else if (sGSBkGrnd == string.Empty)
                                            {
                                                DataTable dTblNonGSCalRenders = new DataTable("dTblNonGSCalRenders");
                                                string sCommText = "SELECT * FROM [MEG Non GS Calendar Renders]";

                                                TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblNonGSCalRenders);

                                                if (dTblNonGSCalRenders.Rows.Count > 0)
                                                {
                                                    foreach (DataRow dRowNonGSCalRenders in dTblNonGSCalRenders.Rows)
                                                    {
                                                        sExportDef = Convert.ToString(dRowNonGSCalRenders["Render Layout"]).Trim();

                                                        string sMonth = Path.GetFileNameWithoutExtension(sExportDef);
                                                        sMonth = sMonth.Substring(sExportDef.IndexOf("_")).Trim();
                                                        sMonth = sMonth.Replace("_", "").Trim();

                                                        bHaveFile = false;
                                                        TM.CheckForExportFileExistence(ref sExportDef, ref bHaveFile);

                                                        if (bHaveFile == true)
                                                        {
                                                            iRenderedCount += +1;
                                                            iJobID += +1;

                                                            sAPSDEST = sRenderedPath + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + "-"
                                                                + iRenderedCount + "-" + sMonth + "Calendar.JPG";
                                                            sExportDefFile = sExportDefPathDone + "DiscProcessor" + "_" + sRefNum + "_" + sProdNum + "_" + sFrameNum + "-"
                                                                + iRenderedCount + "- " + sMonth + "Calendar.txt";

                                                            string text = File.ReadAllText(sExportDefPath + sExportDef);

                                                            text = text.Replace("APSPATH", sPath);
                                                            text = text.Replace("APSDEST", sAPSDEST);

                                                            if (text.Contains("APSTEXT"))
                                                            {
                                                                text = text.Replace("APSTEXT", sNameOn);
                                                            }
                                                            if (text.Contains("APSYEAR"))
                                                            {
                                                                text = text.Replace("APSYEAR", sYearOn);
                                                            }

                                                            File.WriteAllText(sExportDefFile, text);

                                                            dTblJob.Rows.Add(iRenderedCount, sUniqueID, iBatchID, iJobID, sRefNum, sFrameNum, sExportDef, sExportDefFile, sAPSDEST, sNameOn, sYearOn, sMrgJPG, sCustNum);
                                                        }
                                                        else if (bHaveFile != true)
                                                        {
                                                            bGoodResults = false;

                                                            string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ No ExportDef file located. ]";

                                                            TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sExportDefSitting, bSittingBased);

                                                            string sStatus = "90";

                                                            TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                                        }
                                                    }
                                                }
                                                else if (dTblNonGSCalRenders.Rows.Count == 0)
                                                {
                                                    string sStop = string.Empty;
                                                }
                                            }
                                        }
                                    }
                                    else if (s == "Copyright" && (iLoops == iCount))
                                    {
                                        if (sCustNum == "58241")
                                        {
                                            TM.GetOriginalCustomerFromIMQ(ref sCustNum, sProdNum);
                                        }

                                        iRenderedCount += +1;
                                        iJobID += +1;

                                        sAPSDEST = sRenderedPath + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + "-" + iRenderedCount + "-Copyright.JPG";
                                        sExportDefFile = sExportDefPathDone + "DiscProcessor" + "_" + sRefNum + "_" + sProdNum + "_" + sFrameNum + "-" + iRenderedCount + "-Copyright.txt";

                                        string[] sFiles = Directory.GetFiles(sExportDefPath);
                                        string sFilePath = string.Empty;
                                        bool bCopyrightFound = false;

                                        foreach (string sFile in sFiles)
                                        {
                                            sFilePath = Path.GetFileName(sFile);

                                            if (sFilePath == sCustNum + " Copyright.txt")
                                            {
                                                sExportDef = sFilePath;
                                                bCopyrightFound = true;
                                            }
                                            else if (sFilePath == sCustNum + " Copyright.TXT")
                                            {
                                                sExportDef = sFilePath;
                                                bCopyrightFound = true;
                                            }
                                        }
                                        if (bCopyrightFound == true)
                                        {
                                            bGoodResults = true;

                                            string text = File.ReadAllText(sExportDefPath + sExportDef);

                                            text = text.Replace("APSPATH", sPath);
                                            text = text.Replace("APSDEST", sAPSDEST);

                                            File.WriteAllText(sExportDefFile, text);

                                            dTblJob.Rows.Add(iRenderedCount, sUniqueID, iBatchID, iJobID, sRefNum, sFrameNum, sExportDef, sExportDefFile, sAPSDEST, sNameOn, sYearOn, sMrgJPG, sCustNum);
                                        }
                                        else if (bCopyrightFound != true)
                                        {
                                            bGoodResults = false;

                                            string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ No copyright located. ]";

                                            TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sExportDefSitting, bSittingBased);

                                            string sStatus = "90";

                                            TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);

                                            TM.RemoveOrphanedExportDefFiles(dTblJob);

                                            string sText = "[Removed orphaned exportdef files for  " + sRefNum + ".]";
                                            this.LogText(sText);
                                        }
                                    }
                                }
                            }
                            else if (bLockedFile == true || bGatherIDsSuccess != true)
                            {
                                string sStop = string.Empty;
                            }
                        }
                        else if (bSuccess == false)
                        {
                            string sStop = string.Empty;
                        }
                    }
                    else if (bSuccess != true)
                    {
                        string sStop = string.Empty;
                    }
                }
                else if (bSuccess != true)
                {
                    string sStop = string.Empty;
                }
            }
            catch (Exception ex)
            {
                TM.SaveExceptionToDB(ex);
                bGoodResults = false;
            }
        }

        // Add each JobID and a single BatchID for entire disc to be produced.
        private void JobQueueInsert(string sExportDefFile, string sRefNum, string sFrameNum, int iJobID, int iBatchID, ref bool bInserted, ref bool bInitialPass02, bool bSittingBased, string sSitting)
        {
            // jobqueue print status
            //  0 - HOLD........-
            //  1 - READY.......-
            //  2 - RESERVED....-
            //  3 - PRINTING....-
            //  4 - COMPLETED...-
            //  5 - SAVED.......-
            //  6 - ERROR.......-
            //  7 - CANCELLED...-
            //  8 - PENDING.....-
            //  9 - LOADED......-
            // 10 - PARSED......-

            try
            {
                if (bSittingBased != true)
                {
                    if (bInitialPass02 == true)
                    {
                        string sText = "[Queueing reference number " + sRefNum + " and frame number " + sFrameNum + " for rendering.]";
                        this.LogText(sText);

                        bInitialPass02 = false;
                    }
                }
                else if (bSittingBased == true)
                {
                    if (bInitialPass02 == true)
                    {
                        string sText = "[Queueing reference number " + sRefNum + " and sitting number " + sSitting.Trim() + " and frame number " + sFrameNum + " for rendering.]";
                        this.LogText(sText);

                        bInitialPass02 = false;
                    }
                }

                string sCommText = "INSERT INTO JOBQUEUE (QUEUENAME, BATCHID, ORDERID, ORDERSEQUENCE, ORDERITEMID, ORDERITEMQTY," +
                        " ORDERITEMSEQUENCE, PRIORITY, SUBMITDATE, JOBID, PRINTSTATUS, OWNER, JOBPATH) " +
                        "VALUES ('AUTOGEN', '" + iBatchID + "', '" + sRefNum + "', " + "1, 1, 1, 1, 50, '" +
                        Convert.ToString(DateTime.Now.Year).Trim().PadLeft(4, '0') + Convert.ToString(DateTime.Now.Month).Trim().PadLeft(2, '0') + Convert.ToString(DateTime.Now.Day).Trim().PadLeft(2, '0') +
                        Convert.ToString(DateTime.Now.Hour).Trim().PadLeft(2, '0') + Convert.ToString(DateTime.Now.Minute).Trim().PadLeft(2, '0') + Convert.ToString(DateTime.Now.Second).Trim().PadLeft(5, '0') +
                        "', '" + iJobID + "', 0, '" + SystemInformation.ComputerName + "', '" + sExportDefFile + "')";

                bool bSuccess = true;

                TM.SQLNonQuery(sDP2ConnString, sCommText, ref bSuccess);

                if (bSuccess == true)
                {

                }
                else if (bSuccess != true)
                {
                    bInserted = false;
                }
            }
            catch(Exception ex)
            {
                TM.SaveExceptionToDB(ex);
                bInserted = false;
            }
        }

        private void GetMergeFileData(string sProdNum, string sCustNum, string sFrameNum, string sMrgJPG, string sRefNum, ref string sMrgFilePath, ref bool bGotMergeFileData, ref bool bInitialPass03, string sDiscType, string sExportDefSitting, bool bSittingBased)
        {
            try
            {
                string sRenderedPath = string.Empty;
                string sLabel = string.Empty;
                string sMergeText = string.Empty;

                if (sDiscType == "ICD" || sDiscType == "ICDW")
                {
                    sLabel = "ICDRenderedPath";
                }
                else if (sDiscType == "MEG" || sDiscType == "MEGW")
                {
                    sLabel = "MEGRenderedPath";
                }
                else if (sDiscType == "PEC" || sDiscType == "PECW")
                {
                    sLabel = "PECRenderedPath";
                }

                string sValue = "Value";
                string sVariable = string.Empty;
                bool bSuccess = true;

                TM.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                if (bSuccess == true)
                {
                    sRenderedPath = sVariable;

                    if (bSittingBased != true)
                    {
                        if (bInitialPass03 == true)
                        {
                            string sText = "[Gathering merge file data for reference number " + sRefNum + " and frame number " + sFrameNum + ".]";
                            this.LogText(sText);

                            bInitialPass03 = false;
                        }
                    }
                    else if (bSittingBased == true)
                    {
                        if (bInitialPass03 == true)
                        {
                            string sText = "[Gathering merge file data for reference number " + sRefNum + " and sitting number " + sExportDefSitting.Trim() + ".]";
                            this.LogText(sText);

                            bInitialPass03 = false;
                        }
                    }

                    string sCustName = string.Empty;
                    string sCommText = "SELECT Name FROM Customer WHERE Customer = " + "'" + sCustNum + "'";
                    DataTable dt = new DataTable();

                    TM.CDSQuery(sCDSConnString, sCommText, dt);

                    if (dt.Rows.Count > 0)
                    {
                        sCustName = Convert.ToString(dt.Rows[0]["Name"]).Trim();

                        string sSequence = sFrameNum.TrimStart('0');

                        sCommText = "SELECT Teacher, First_name, Last_name, Schoolname FROM Endcust WHERE Lookupnum = " + "'" + sProdNum + "'" +
                            " AND Sequence = " + sSequence;
                        DataTable dt2 = new DataTable();

                        TM.CDSQuery(sCDSConnString, sCommText, dt2);

                        string sMrgFullName = string.Empty;
                        string sMrgSchoolName = string.Empty;
                        string sMrgTeacher = string.Empty;

                        if (dt2.Rows.Count > 0)
                        {
                            string sMrgFName = Convert.ToString(dt2.Rows[0]["First_name"]).Trim();
                            string sMrgLName = Convert.ToString(dt2.Rows[0]["Last_name"]).Trim();
                            sMrgFullName = sMrgFName + " " + sMrgLName;
                            sMrgSchoolName = Convert.ToString(dt2.Rows[0]["Schoolname"]).Trim();
                            sMrgTeacher = Convert.ToString(dt2.Rows[0]["Teacher"]).Trim();

                            sMrgFullName = sMrgFullName.Replace("'", "''");

                            if (bSittingBased != true)
                            {
                                sMrgFilePath = sRenderedPath + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + @"\Merge.txt";
                            }
                            else if (bSittingBased == true)
                            {
                                sMrgFilePath = sRenderedPath + sRefNum + @"\" + sProdNum + @"\" + sProdNum + "_" + sExportDefSitting.Trim() + "_" + "PECMerge.txt";
                            }                            

                            string sPath = Path.GetDirectoryName(sMrgFilePath);

                            if (!Directory.Exists(sPath))
                            {
                                Directory.CreateDirectory(sPath);
                            }

                            if (bSittingBased != true)
                            {
                                // Example merge file data.
                                // "<Student_Name>","<Prod+Frame>","<JPG>","<SchoolName>","<CustomerName>","<Order>","<Teacher>"

                                sMergeText = "\"" + sMrgFullName + "\",\"" + sProdNum + sFrameNum + "\",\"" + sMrgJPG + "\",\"" + sMrgSchoolName + "\",\"" + sCustName + "\",\"" + sRefNum + "\",\"" + sMrgTeacher + "\"";
                            }
                            else if (bSittingBased == true)
                            {
                                // Example merge file data.
                                // "<Customer>","<PEC_+Prod_+Ref_+Sitting.Trim()>","<?>","<Sitting.Trim()>","<RefNum>","<CustID>"

                                DataTable dTblItems = new DataTable("dTblItems");
                                sCommText = "SELECT Custid FROM Items WHERE Lookupnum = '" + sProdNum + "'";

                                TM.CDSQuery(sCDSConnString, sCommText, dTblItems);

                                if (dTblItems.Rows.Count > 0)
                                {
                                    string sCustID = Convert.ToString(dTblItems.Rows[0]["Custid"]).Trim();

                                    sMergeText = "\"" + sCustName + "\",\"PEC_" + sProdNum + "_" + sRefNum + "_" + sExportDefSitting.Trim() + "\",\"" + "\",\"" + sExportDefSitting.Trim() + "\",\"" + sRefNum + "\",\"" + sCustID + "\"";
                                }
                                else if (dTblItems.Rows.Count == 0)
                                {
                                    string sStop = string.Empty;
                                }
                            }

                            File.WriteAllText(sMrgFilePath, sMergeText);

                            sMrgFName = sMrgFName.Replace("'", "''");
                            sMrgSchoolName = sMrgSchoolName.Replace("'", "''");
                            sMrgTeacher = sMrgTeacher.Replace("'", "''");
                            sCustName = sCustName.Replace("'", "''");
                            sMergeText = sMergeText.Replace("'", "''");
                            
                            if (bSittingBased != true)
                            {
                                sCommText = "UPDATE [DiscOrders] SET [MergeFileData] = '" + sMergeText + "' WHERE [ProdNum] = '" + sProdNum + "' AND [FrameNum] = '" + sFrameNum + "' AND [DiscType] = '" + sDiscType + "'";
                            }
                            else if (bSittingBased == true)
                            {
                                sCommText = "UPDATE [DiscOrders] SET [MergeFileData] = '" + sMergeText + "' WHERE [ProdNum] = '" + sProdNum + "' AND [Sitting] = '" + sExportDefSitting + "' AND [DiscType] = '" + sDiscType + "'";
                            }                            

                            bSuccess = true;

                            TM.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                            if (bSuccess == true)
                            {
                                bGotMergeFileData = true;
                            }
                            else if (bSuccess != true)
                            {
                                MessageBox.Show("SQL INSERT/UPDATE Failed : " + Environment.NewLine + sCommText);
                                bGotMergeFileData = false;
                            }
                        }
                        else if (dt2.Rows.Count == 0)
                        {
                            string sStop = string.Empty;

                            sSequence = sFrameNum.TrimStart('0');

                            DataTable dTblSport = new DataTable("Sport");
                            sCommText = "SELECT First_name, Last_name FROM Sport WHERE Lookupnum = '" + sProdNum + "' AND Sequence = " + sSequence;

                            TM.CDSQuery(sCDSConnString, sCommText, dTblSport);

                            if (dTblSport.Rows.Count > 0)
                            {
                                string sMrgFName = Convert.ToString(dTblSport.Rows[0]["First_name"]).Trim();
                                string sMrgLName = Convert.ToString(dTblSport.Rows[0]["Last_name"]).Trim();
                                sMrgFullName = sMrgFName + " " + sMrgLName;
                                sMrgSchoolName = " ";
                                sMrgTeacher = " ";

                                sMrgFullName = sMrgFullName.Replace("'", "''");

                                if (bSittingBased != true)
                                {
                                    sMrgFilePath = sRenderedPath + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + @"\Merge.txt";
                                }
                                else if (bSittingBased == true)
                                {
                                    sMrgFilePath = sRenderedPath + sRefNum + @"\" + sProdNum + @"\" + sProdNum + "_" + sExportDefSitting.Trim() + "_" + "PECMerge.txt";
                                }  

                                string sPath = Path.GetDirectoryName(sMrgFilePath);

                                if (!Directory.Exists(sPath))
                                {
                                    Directory.CreateDirectory(sPath);
                                }

                                if (bSittingBased != true)
                                {
                                    // Example merge file data.
                                    // "<Student_Name>","<Prod+Frame>","<JPG>","<SchoolName>","<CustomerName>","<Order>","<Teacher>"

                                    sMergeText = "\"" + sMrgFullName + "\",\"" + sProdNum + sFrameNum + "\",\"" + sMrgJPG + "\",\"" + sMrgSchoolName + "\",\"" + sCustName + "\",\"" + sRefNum + "\",\"" + sMrgTeacher + "\"";
                                }
                                else if (bSittingBased == true)
                                {
                                    // Example merge file data.
                                    // "<Customer>","<PEC_+Prod_+Ref_+Sitting.Trim()>","<?>","<Sitting.Trim()>","<RefNum>","<CustID>"

                                    DataTable dTblItems = new DataTable("dTblItems");
                                    sCommText = "SELECT Custid FROM Items WHERE Lookupnum = '" + sProdNum + "'";

                                    TM.CDSQuery(sCDSConnString, sCommText, dTblItems);

                                    if (dTblItems.Rows.Count > 0)
                                    {
                                        string sCustID = Convert.ToString(dTblItems.Rows[0]["Custid"]).Trim();

                                        sMergeText = "\"" + sCustName + "\",\"PEC_" + sProdNum + "_" + sRefNum + "_" + sExportDefSitting.Trim() + "\",\"" + "\",\"" + sExportDefSitting.Trim() + "\",\"" + sRefNum + "\",\"" + sCustID + "\"";
                                    }
                                    else if (dTblItems.Rows.Count == 0)
                                    {
                                        sStop = string.Empty;
                                    }
                                }

                                File.WriteAllText(sMrgFilePath, sMergeText);

                                sMrgFName = sMrgFName.Replace("'", "''");
                                sMrgSchoolName = sMrgSchoolName.Replace("'", "''");
                                sMrgTeacher = sMrgTeacher.Replace("'", "''");
                                sCustName = sCustName.Replace("'", "''");
                                sMergeText = sMergeText.Replace("'", "''");
                                
                                if (bSittingBased != true)
                                {
                                    sCommText = "UPDATE [DiscOrders] SET [MergeFileData] = '" + sMergeText + "' WHERE [ProdNum] = '" + sProdNum + "' AND [FrameNum] = '" + sFrameNum + "' AND [DiscType] = '" + sDiscType + "'";
                                }
                                else if (bSittingBased == true)
                                {
                                    sCommText = "UPDATE [DiscOrders] SET [MergeFileData] = '" + sMergeText + "' WHERE [ProdNum] = '" + sProdNum + "' AND [Sitting] = '" + sExportDefSitting + "' AND [DiscType] = '" + sDiscType + "'";
                                }

                                bSuccess = true;

                                TM.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                                if (bSuccess == true)
                                {
                                    bGotMergeFileData = true;
                                }
                                else if (bSuccess != true)
                                {
                                    MessageBox.Show("SQL INSERT/UPDATE Failed : " + Environment.NewLine + sCommText);
                                    bGotMergeFileData = false;
                                }
                            }
                            else if (dTblSport.Rows.Count == 0)
                            {
                                sMrgSchoolName = " ";
                                sMrgTeacher = " ";
                                sMrgFullName = " ";
                                sCustName = sCustName.Replace("'", "''");   

                                if (bSittingBased != true)
                                {
                                    sMrgFilePath = sRenderedPath + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + @"\Merge.txt";
                                }
                                else if (bSittingBased == true)
                                {
                                    sMrgFilePath = sRenderedPath + sRefNum + @"\" + sProdNum + @"\" + sProdNum + "_" + sExportDefSitting.Trim() + "_" + "PECMerge.txt";
                                }  

                                string sPath = Path.GetDirectoryName(sMrgFilePath);

                                if (!Directory.Exists(sPath))
                                {
                                    Directory.CreateDirectory(sPath);
                                }

                                if (bSittingBased != true)
                                {
                                    // Example merge file data.
                                    // "<Student_Name>","<Prod+Frame>","<JPG>","<SchoolName>","<CustomerName>","<Order>","<Teacher>"

                                    sMergeText = "\"" + sMrgFullName + "\",\"" + sProdNum + sFrameNum + "\",\"" + sMrgJPG + "\",\"" + sMrgSchoolName + "\",\"" + sCustName + "\",\"" + sRefNum + "\",\"" + sMrgTeacher + "\"";
                                }
                                else if (bSittingBased == true)
                                {
                                    // Example merge file data.
                                    // "<Customer>","<PEC_+Prod_+Ref_+Sitting.Trim()>","<?>","<Sitting.Trim()>","<RefNum>","<CustID>"

                                    DataTable dTblItems = new DataTable("dTblItems");
                                    sCommText = "SELECT Custid FROM Items WHERE Lookupnum = '" + sProdNum + "'";

                                    TM.CDSQuery(sCDSConnString, sCommText, dTblItems);

                                    if (dTblItems.Rows.Count > 0)
                                    {
                                        string sCustID = Convert.ToString(dTblItems.Rows[0]["Custid"]).Trim();

                                        sMergeText = "\"" + sCustName + "\",\"PEC_" + sProdNum + "_" + sRefNum + "_" + sExportDefSitting.Trim() + "\",\"" + "\",\"" + sExportDefSitting.Trim() + "\",\"" + sRefNum + "\",\"" + sCustID + "\"";
                                    }
                                    else if (dTblItems.Rows.Count == 0)
                                    {
                                        sStop = string.Empty;
                                    }
                                }

                                File.WriteAllText(sMrgFilePath, sMergeText);

                                sCustName = sCustName.Replace("'", "''");
                                sMergeText = sMergeText.Replace("'", "''");

                                if (bSittingBased != true)
                                {
                                    sCommText = "UPDATE [DiscOrders] SET [MergeFileData] = '" + sMergeText + "' WHERE [ProdNum] = '" + sProdNum + "' AND [FrameNum] = '" + sFrameNum + "' AND [DiscType] = '" + sDiscType + "'";
                                }
                                else if (bSittingBased == true)
                                {
                                    sCommText = "UPDATE [DiscOrders] SET [MergeFileData] = '" + sMergeText + "' WHERE [ProdNum] = '" + sProdNum + "' AND [Sitting] = '" + sExportDefSitting + "' AND [DiscType] = '" + sDiscType + "'";
                                }

                                bSuccess = true;

                                TM.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                                if (bSuccess == true)
                                {
                                    bGotMergeFileData = true;
                                }
                                else if (bSuccess != true)
                                {
                                    MessageBox.Show("SQL INSERT/UPDATE Failed : " + Environment.NewLine + sCommText);
                                    bGotMergeFileData = false;
                                }
                            }
                        }
                    }
                }
                else if (bSuccess != true)
                {
                    string sStop = string.Empty;
                }
            }
            catch(Exception ex)
            {
                TM.SaveExceptionToDB(ex);
                bGotMergeFileData = false;
            }
        }

        // This method should be called once at the finish of image rendering foreach refnum + framenum currently being processed. File for Rimage disc production.
        private void GetNWPFileData(string sProdNum, string sMrgFilePath, string sFrameNum, string sRefNum, ref bool bGotNWPFileData, ref bool bInitialPass04, string sDiscType, string sCustNum, string sExportDefSitting, bool bSittingBased, string sSitting)
        {
            //file = f:\OHS-SRS\IBC
            //FileType = Parent
            //copies = 2
            //Label = \\nb\users\bross\Burner-Temps\APS-cdw-4.btw
            //Priority = 1
            //media = CDR
            //Merge = \\nb\jobs\cdsburn\mergefile\_22222_333333_.txt
            //OrderID = Test_22222_333333
            //volume = just-a-test

            try
            {
                string sDiscQuantity = string.Empty;
                string sLabelFile = string.Empty;
                string sLogoFile = string.Empty;
                bool bGotLabelFile = true;
                string sCommText = string.Empty;
                string sRenderedPath = string.Empty;
                string sRenderedPath2 = string.Empty;

                if (bSittingBased != true)
                {
                    if (bInitialPass04 == true)
                    {
                        string sText = "[Gathering NWP file data for reference number " + sRefNum + " and frame number " + sFrameNum + ".]";
                        this.LogText(sText);

                        bInitialPass04 = false;

                        sCommText = "SELECT [Quantity] FROM [DiscOrders] WHERE [UniqueID] = '" + sProdNum + sFrameNum + sDiscType + "'";
                    }
                }
                else if (bSittingBased == true)
                {
                    string sText = "[Gathering NWP file data for reference number " + sRefNum + " and sitting number " + sSitting.Trim() + " and frame number " + sFrameNum + ".]";
                    this.LogText(sText);

                    bInitialPass04 = false;

                    sCommText = "SELECT [Quantity] FROM [DiscOrders] WHERE [UniqueID] = '" + sProdNum + sSitting.Trim() + sDiscType + "'";
                }
                
                DataTable dTbl = new DataTable();

                TM.SQLQuery(sDiscProcessorConnString, sCommText, dTbl);

                if (dTbl.Rows.Count > 0)
                {
                    sDiscQuantity = Convert.ToString(dTbl.Rows[0]["Quantity"]).Trim();

                    if (sDiscType == "ICD" || sDiscType == "ICDW")
                    {
                        string sLabel = "ICDLabelFile";
                        string sValue = "Value";
                        string sVariable = string.Empty;
                        bool bSuccess = true;

                        TM.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                        if (bSuccess == true)
                        {
                            sLabelFile = sVariable;
                            bGotLabelFile = true;
                        }
                        else if (bSuccess != true)
                        {
                            bGotLabelFile = false;
                        }

                        DataTable dTblRenderedPath = new DataTable("dTblRenderedPath");
                        sCommText = "SELECT [Value] FROM [Variables] WHERE [Label] = 'ICDRenderedPath'";

                        TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblRenderedPath);
 
                        if (dTblRenderedPath.Rows.Count > 0)
                        {
                            sRenderedPath = Convert.ToString(dTblRenderedPath.Rows[0]["Value"]).Trim();
                        }
                    }
                    else if (sDiscType == "MEG" || sDiscType == "MEGW")
                    {
                        string sLabel = "MEGLabelFile";
                        string sValue = "Value";
                        string sVariable = string.Empty;
                        bool bSuccess = true;

                        TM.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                        if (bSuccess == true)
                        {
                            sLabelFile = sVariable;
                            bGotLabelFile = true;
                        }
                        else if (bSuccess != true)
                        {
                            bGotLabelFile = false;
                        }

                        DataTable dTblRenderedPath = new DataTable("dTblRenderedPath");
                        sCommText = "SELECT [Value] FROM [Variables] WHERE [Label] = 'MEGRenderedPath'";

                        TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblRenderedPath);

                        if (dTblRenderedPath.Rows.Count > 0)
                        {
                            sRenderedPath = Convert.ToString(dTblRenderedPath.Rows[0]["Value"]).Trim();
                        }
                    }
                    else if (sDiscType == "PEC" || sDiscType == "PECW")
                    {
                        string sLabel = "PECLabelFile";
                        string sValue = "Value";
                        string sVariable = string.Empty;
                        bool bSuccess = true;

                        TM.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                        if (bSuccess == true)
                        {
                            sLabelFile = sVariable;
                            bGotLabelFile = true;
                        }
                        else if (bSuccess != true)
                        {
                            bGotLabelFile = false;
                        }

                        DataTable dTblRenderedPath = new DataTable("dTblRenderedPath");
                        sCommText = "SELECT [Value] FROM [Variables] WHERE [Label] = 'PECRenderedPath'";

                        TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblRenderedPath);

                        if (dTblRenderedPath.Rows.Count > 0)
                        {
                            sRenderedPath = Convert.ToString(dTblRenderedPath.Rows[0]["Value"]).Trim();
                        }
                    }
                    
                    DataTable dTblCustomerLabels = new DataTable("dTblCustomerLabels");
                    sCommText = "SELECT * FROM [CustomerLabels]";

                    TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblCustomerLabels);

                    if (dTblCustomerLabels.Rows.Count > 0)
                    {
                        foreach (DataRow dRowCustomerLabels in dTblCustomerLabels.Rows)
                        {
                            string sCustomerLabelsCustNum = Convert.ToString(dRowCustomerLabels["CustNum"]).Trim();

                            if (sCustNum == sCustomerLabelsCustNum)
                            {
                                string sCustomerLabelsLabel = Convert.ToString(dRowCustomerLabels["LabelLocation"]).Trim();
                                string sCustomerLabelsLogo = Convert.ToString(dRowCustomerLabels["LogoLocation"]).Trim();

                                sLabelFile = sCustomerLabelsLabel;

                                bGotLabelFile = true;

                                if (sCustomerLabelsLogo != "" || sCustomerLabelsLogo != string.Empty)
                                {
                                    sLogoFile = sCustomerLabelsLogo;
                                }
                            }
                            else if (sCustNum != sCustomerLabelsCustNum)
                            {
                                // Do nothing.
                            }
                        }
                    }
                    else if (dTblCustomerLabels.Rows.Count == 0)
                    {
                        string sStop = string.Empty;
                    }

                    if (bGotLabelFile == true)
                    {
                        string sUniqueID = string.Empty;

                        if (bSittingBased != true)
                        {
                            sUniqueID = sRefNum + sFrameNum;
                        }
                        else if (bSittingBased == true)
                        {
                            sUniqueID = sRefNum + sSitting.Trim() + sFrameNum;
                        }

                        string sLabel = "RenderedPath";
                        string sValue = "Value";
                        string sVariable = string.Empty;
                        bool bSuccess = true;

                        TM.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                        if (bSuccess == true)
                        {
                            if (bSittingBased != true)
                            {
                                sRenderedPath += sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum;
                            }
                            else if (bSittingBased == true)
                            {
                                sRenderedPath2 = sRenderedPath + sRefNum + @"\" + sProdNum + @"\";
                                sRenderedPath += sRefNum + @"\" + sProdNum + @"\" + sSitting.Trim() + @"\";
                            }                            

                            string sFile = Path.GetDirectoryName(sMrgFilePath);
                            StringBuilder sb = new StringBuilder();

                            if (bSittingBased != true)
                            {
                                sb.AppendFormat("file = {0}", sFile);
                            }
                            else if (bSittingBased == true)
                            {
                                sb.AppendFormat("file = {0}", sRenderedPath + Environment.NewLine);
                            }
                            
                            sb.Append(Environment.NewLine);
                            sb.AppendFormat("FileType = Parent");
                            sb.Append(Environment.NewLine);
                            sb.AppendFormat("copies = {0}", sDiscQuantity);
                            sb.Append(Environment.NewLine);

                            if (sLabel != string.Empty)
                            {
                                sb.AppendFormat("Label = {0}", sLabelFile);
                                sb.Append(Environment.NewLine);
                            }                            

                            if (sLogoFile != "" || sLogoFile != string.Empty)
                            {
                                sb.AppendFormat("Logo = {0}", sLogoFile);
                                sb.Append(Environment.NewLine);
                            }

                            sb.AppendFormat("Priority = 1");
                            sb.Append(Environment.NewLine);
                            sb.AppendFormat("media = CDR");
                            sb.Append(Environment.NewLine);

                            if (bSittingBased != true)
                            {
                                sb.AppendFormat("Merge = {0}", sMrgFilePath);
                            }
                            else if (bSittingBased == true)
                            {
                                sMrgFilePath = sRenderedPath2 + sProdNum + "_" + sSitting.Trim() + "_" + "PECMerge.txt";
                                sb.AppendFormat("Merge = {0}", sMrgFilePath);
                            }
                            
                            sb.Append(Environment.NewLine);
                            sb.AppendFormat("OrderID = {0}", sUniqueID);
                            sb.Append(Environment.NewLine);
                            sb.AppendFormat("volume = {0}", sUniqueID);
                            sb.Append(Environment.NewLine);

                            if (bSittingBased != true)
                            {
                                sCommText = "UPDATE [DiscOrders] SET [NWPFileData] = '" + sb.ToString() + "' WHERE [ProdNum] = '" + sProdNum + "' AND [FrameNum] = '" + sFrameNum + "'";
                            }
                            else if (bSittingBased == true)
                            {
                                sCommText = "UPDATE [DiscOrders] SET [NWPFileData] = '" + sb.ToString() + "' WHERE [ProdNum] = '" + sProdNum + "' AND [Sitting] = '" + sSitting + "'";
                            }                            

                            bool bSuccess2 = true;

                            TM.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess2);

                            if (bSuccess2 == true)
                            {
                                bGotNWPFileData = true;
                            }
                            else if (bSuccess2 != true)
                            {
                                MessageBox.Show("SQL INSERT/UPDATE Failed : " + Environment.NewLine + sCommText);
                                bGotNWPFileData = false;
                            }
                        }
                        else if (bSuccess != true)
                        {
                            string sStop = string.Empty;
                        }
                    }
                    else if (bGotLabelFile != true)
                    {
                        string sStop = string.Empty;
                    }
                }
                else if (dTbl.Rows.Count == 0)
                {
                    string sStop = string.Empty;
                    bGotNWPFileData = false;
                }
            }
            catch(Exception ex)
            {
                TM.SaveExceptionToDB(ex);
                bGotNWPFileData = false;
            }
        }

        // Update DP2.JobQueue where BatchID = all rendered JobIDs (1 for each rendered image) at once aka the entire disc.
        private void JobQueuePrintStatusUpdateFrameBased(string sRefNum, string sFrameNum, ref int iBatchID, ref bool bJobQueueStatusUpdated, ref bool bInitialPass05)
        {
            try
            {
                if (bInitialPass05 == true)
                {
                    string sText = "[Flagging reference number " + sRefNum + " and frame number " + sFrameNum + " to be rendered.]";
                    this.LogText(sText);

                    bInitialPass05 = false;
                }

                string sCommText = "UPDATE [JobQueue] SET [PrintStatus] = '1' WHERE [OrderID] = '" + sRefNum + "' AND [BatchID] = '" + iBatchID + "'";

                bool bSuccess = true;

                TM.SQLNonQuery(sDP2ConnString, sCommText, ref bSuccess);

                if (bSuccess == true)
                {
                    bJobQueueStatusUpdated = true;
                }
                else if (bSuccess != true)
                {
                    MessageBox.Show("SQL INSERT/UPDATE Failed : " + Environment.NewLine + sCommText);
                    bJobQueueStatusUpdated = false;
                }
            }
            catch(Exception ex)
            {
                TM.SaveExceptionToDB(ex);
                bJobQueueStatusUpdated = false;
            }
        }

        // Update DP2.JobQueue where BatchID = all rendered JobIDs (1 for each rendered image) at once aka the entire disc.
        private void JobQueuePrintStatusUpdateSittingBased(string sRefNum, ref int iBatchID, ref bool bJobQueueStatusUpdated, ref bool bInitialPass05)
        {
            try
            {
                if (bInitialPass05 == true)
                {
                    string sText = "[Flagging reference number " + sRefNum + " to be rendered.]";
                    this.LogText(sText);

                    bInitialPass05 = false;
                }

                string sCommText = "UPDATE [JobQueue] SET [PrintStatus] = '1' WHERE [OrderID] = '" + sRefNum + "'";

                bool bSuccess = true;

                TM.SQLNonQuery(sDP2ConnString, sCommText, ref bSuccess);

                if (bSuccess == true)
                {
                    bJobQueueStatusUpdated = true;
                }
                else if (bSuccess != true)
                {
                    MessageBox.Show("SQL INSERT/UPDATE Failed : " + Environment.NewLine + sCommText);
                    bJobQueueStatusUpdated = false;
                }
            }
            catch (Exception ex)
            {
                TM.SaveExceptionToDB(ex);
                bJobQueueStatusUpdated = false;
            }
        }

        #endregion

        #region Check for rendered images and move NWP file to disc publisher when needed image rendered count is achieved.

        // Gather records that are flagged as having been queued for rendering and check for rendered images.
        private void CheckForFrameBasedRenderedImages()
        {
            try
            {
                string sUniqueID = string.Empty;
                bool bContinue = true;
                string sRenderedPath = string.Empty;
                string sDiscType = string.Empty;
                bool bSittingBased = false;
                string sAction = "DPRecProcd";

                DataTable dTblGatherDiscTypes = new DataTable("dTblGatherDiscTypes");
                string sCommText = "SELECT * FROM [GatherDiscTypes] WHERE [SittingBased] = '0'";

                TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblGatherDiscTypes);

                if (dTblGatherDiscTypes.Rows.Count > 0)
                {
                    foreach (DataRow dRowGatherDiscTypes in dTblGatherDiscTypes.Rows)
                    {
                        sDiscType = Convert.ToString(dRowGatherDiscTypes["GatherDiscType"]).Trim();

                        DataTable dTbl = new DataTable();
                        sCommText = "SELECT * FROM [DiscOrders] WHERE [Status] = '35' AND [DiscType] = '" + sDiscType + "'";

                        TM.SQLQuery(sDiscProcessorConnString, sCommText, dTbl);

                        if (dTbl.Rows.Count > 0)
                        {
                            foreach (DataRow dRow in dTbl.Rows)
                            {
                                string sRefNum = Convert.ToString(dRow["RefNum"]).Trim();
                                string sFrameNum = Convert.ToString(dRow["FrameNum"]).Trim();
                                string sProdNum = Convert.ToString(dRow["ProdNum"]).Trim();
                                string sSitting = Convert.ToString(dRow["Sitting"]);
                                int iNeededRenderCount = Convert.ToInt32(dRow["JobIDsNeeded"]);

                                DataTable dTbl1 = new DataTable();
                                string sText = "[Checking for rendered images for reference number " + sRefNum + " and frame number " + sFrameNum + ".]";
                                this.LogText(sText);

                                string sLabel = string.Empty;

                                if (sDiscType == "ICD" || sDiscType == "ICDW")
                                {
                                    sLabel = "ICDRenderedPath";
                                }
                                else if (sDiscType == "MEG" || sDiscType == "MEGW")
                                {
                                    sLabel = "MEGRenderedPath";
                                }

                                string sValue = "Value";
                                string sVariable = string.Empty;
                                bool bSuccess = true;

                                TM.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                                if (bSuccess == true)
                                {
                                    sRenderedPath = sVariable + sProdNum + sFrameNum + @"\" + sProdNum + sFrameNum + @"\";

                                    if (Directory.Exists(sRenderedPath))
                                    {
                                        int iDirRenderedCount = Directory.GetFiles(sRenderedPath, "*.jpg", SearchOption.TopDirectoryOnly).Length;
                                        int iMergeFileCount = Directory.GetFiles(sRenderedPath, "Merge.txt", SearchOption.TopDirectoryOnly).Length;

                                        if (iMergeFileCount == 1)
                                        {
                                            if (iDirRenderedCount == iNeededRenderCount)
                                            {
                                                string sEventPass = string.Empty;
                                                string sEventCode = string.Empty;
                                                bool bHaveCodeAndPass = false;

                                                //TM.GetEventInfo(sPath, sRefNum, ref sEventCode, ref sEventPass, ref bHaveCodeAndPass);

                                                bHaveCodeAndPass = true;

                                                if (bHaveCodeAndPass == true)
                                                {
                                                    bool bMadeDirs = true;
                                                    //TM.MakeWebDirectories(sRenderedPath, sEventCode, sEventPass, ref bMadeDirs, sDiscType);

                                                    if (bMadeDirs == true)
                                                    {
                                                        bContinue = true;
                                                    }
                                                    //else if (bMadeDirs != true)
                                                    //{
                                                    //    string sStop = string.Empty;
                                                    //    bContinue = false;

                                                    //    string sErrorDescription = "[" + DateTime.Now.ToString() + " ][ Failed to create web directories.]";

                                                    //    TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sSitting);

                                                    //    string sStatus = "90";

                                                    //    TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                                    //}
                                                }
                                                else if (bHaveCodeAndPass != true)
                                                {
                                                    // Continue.
                                                }

                                                bContinue = true;

                                                if (bContinue == true)
                                                {
                                                    // Update DiscOrders.Status = 40 (Images have been rendered.)
                                                    string sStatus = "40";
                                                    TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);

                                                    // Generate the Copyright Release.txt
                                                    bool bCRGenerated = false;
                                                    TM.GenerateCopyrightRelease(sRenderedPath, ref bCRGenerated);

                                                    if (bCRGenerated == true)
                                                    {
                                                        // Update DiscOrders.Status = 50 (Copyright and Merge text files are with the rendered images.)
                                                        sStatus = "50";
                                                        TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);

                                                        // Moves NWP file to the rimage for disc production.
                                                        bool bNWPMoved = false;
                                                        this.MoveNWPtoReady(sRefNum, sFrameNum, sRenderedPath, ref bNWPMoved, sDiscType, sSitting, bSittingBased);

                                                        if (bNWPMoved == true)
                                                        {
                                                            // Update DiscOrders.Status = 60 (NWP files moved to initiate processing of the disc. Completed.)
                                                            sStatus = "60";
                                                            TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);

                                                            bool bStamped = false;

                                                            TM.StampIt(sProdNum, sAction, ref bStamped);

                                                            if (bStamped == true)
                                                            {

                                                            }
                                                            else if (bStamped != true)
                                                            {

                                                            }
                                                        }
                                                        else if (bNWPMoved != true)
                                                        {
                                                            string sStop = string.Empty;

                                                            string sErrorDescription = "[" + DateTime.Now.ToString() + " ][ Failed to move NWP file.]";

                                                            TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sSitting, bSittingBased);

                                                            sStatus = "90";

                                                            TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                                        }
                                                    }
                                                    else if (bCRGenerated != true)
                                                    {
                                                        string sStop = string.Empty;

                                                        string sErrorDescription = "[" + DateTime.Now.ToString() + " ][ Failed to generate Copyright.]";

                                                        TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sSitting, bSittingBased);

                                                        sStatus = "90";

                                                        TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                                    }
                                                }
                                                else if (bContinue != true)
                                                {
                                                    string sStop = string.Empty;
                                                }
                                            }
                                            else if (iDirRenderedCount != iNeededRenderCount)
                                            {
                                                string sStop = string.Empty;
                                            }
                                        }
                                        else if (iMergeFileCount == 0)
                                        {
                                            string sStop = string.Empty;

                                            string sErrorDescription = "[" + DateTime.Now.ToString() + " ][ No merge file in rendered directory.]";

                                            TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sSitting, bSittingBased);

                                            string sStatus = "90";

                                            TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                        }
                                    }
                                    else if (!Directory.Exists(sRenderedPath))
                                    {
                                        string sStop = string.Empty;

                                        string sErrorDescription = "[" + DateTime.Now.ToString() + " ][ Rendered image path does not exist.]";

                                        TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sSitting, bSittingBased);

                                        string sStatus = "90";

                                        TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                    }
                                }
                                else if (bSuccess != true)
                                {
                                    string sStop = string.Empty;
                                }
                            }
                        }
                        else if (dTbl.Rows.Count == 0)
                        {
                            string sStop = string.Empty;
                        }
                    }
                }
                else if (dTblGatherDiscTypes.Rows.Count == 0)
                {
                    string sStop = string.Empty;
                }
            }
            catch(Exception ex)
            {
                TM.SaveExceptionToDB(ex);
            }
        }        

        private void CheckForSittingBasedRenderedImages()
        {
            try
            {
                string sDiscType = string.Empty;
                string sRenderedPath = string.Empty;
                string sRenderedPath2 = string.Empty;
                string sOriginalRenderedPath = string.Empty;
                bool bContinue = false;
                bool bAllGood = false;
                bool bSittingBased = true;
                string sRefNum = string.Empty;
                string sFrameNum = string.Empty;
                string sProdNum = string.Empty;
                string sSitting = string.Empty;

                DataTable dTblCheckedForRenderedImages = new DataTable("dTblCheckedForRenderedImages");
                dTblCheckedForRenderedImages.Columns.Add("RefNum", typeof(string));


                DataTable dTblGatherDiscTypes = new DataTable("dTblGatherDiscTypes");
                string sCommText = "SELECT * FROM [GatherDiscTypes] WHERE [SittingBased] = '1'";

                TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblGatherDiscTypes);

                if (dTblGatherDiscTypes.Rows.Count > 0)
                {
                    foreach (DataRow dRowGatherDiscTypes in dTblGatherDiscTypes.Rows)
                    {
                        sDiscType = Convert.ToString(dRowGatherDiscTypes["GatherDiscType"]).Trim();

                        DataTable dTblRenderedPath = new DataTable("dTblRenderedPath");                        

                        if (sDiscType == "PEC" || sDiscType == "PECW")
                        {
                            sCommText = "SELECT [Value] FROM [Variables] WHERE [Label] = 'PECRenderedPath'";

                            TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblRenderedPath);

                            if (dTblRenderedPath.Rows.Count > 0)
                            {
                                sRenderedPath = Convert.ToString(dTblRenderedPath.Rows[0]["Value"]).Trim();
                            }
                            else if (dTblRenderedPath.Rows.Count == 0)
                            {
                                string sStop = string.Empty;
                            }
                        }
                        else if (sDiscType != "PEC" || sDiscType != "PECW")
                        {
                            string sStop = string.Empty;
                        }

                        sOriginalRenderedPath = sRenderedPath;
                        sRenderedPath2 = sRenderedPath;

                        DataTable dTblDiscOrders = new DataTable("dTblDiscOrders");
                        sCommText = "SELECT * FROM [DiscOrders] WHERE [Status] = '35' AND [DiscType] = '" + sDiscType + "'";

                        TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblDiscOrders);

                        if (dTblDiscOrders.Rows.Count > 0)
                        {
                            foreach (DataRow dRowDiscOrders in dTblDiscOrders.Rows)
                            {
                                sRenderedPath = sOriginalRenderedPath;
                                sRenderedPath2 = sOriginalRenderedPath;

                                sRefNum = Convert.ToString(dRowDiscOrders["RefNum"]).Trim();
                                sFrameNum = Convert.ToString(dRowDiscOrders["FrameNum"]).Trim();
                                sProdNum = Convert.ToString(dRowDiscOrders["ProdNum"]).Trim();
                                sSitting = Convert.ToString(dRowDiscOrders["Sitting"]);

                                string sText = "[Checking for rendered images for reference number " + sRefNum + " and sittng number " + sSitting.Trim() + ".]";
                                this.LogText(sText);

                                DataTable dTblFrames = new DataTable("dTblFrames");
                                sCommText = "SELECT * FROM Frames WHERE Lookupnum = '" + sProdNum + "' AND Sitting = '" + sSitting + "'";

                                TM.CDSQuery(sCDSConnString, sCommText, dTblFrames);

                                if (dTblFrames.Rows.Count > 0)
                                {                                    
                                    int iNeededRenderedImageCount = 0;
                                    int idTblFramesRowCount = dTblFrames.Rows.Count;

                                    iNeededRenderedImageCount = idTblFramesRowCount * 4;

                                    sRenderedPath += sRefNum + @"\" + sProdNum + @"\" + sSitting.Trim() + @"\";

                                    if (Directory.Exists(sRenderedPath))
                                    {
                                        string sMergeFile = sRenderedPath2 + sRefNum + @"\" + sProdNum + @"\" + sProdNum + "_" + sSitting.Trim() + "_" + "PECMerge.txt";

                                        if (File.Exists(sMergeFile))
                                        {
                                            int iDirRenderedCount = Directory.GetFiles(sRenderedPath, "*.jpg", SearchOption.TopDirectoryOnly).Length;

                                            if (iDirRenderedCount == iNeededRenderedImageCount || iDirRenderedCount == iNeededRenderedImageCount + 1)
                                            {
                                                string sEventPass = string.Empty;
                                                string sEventCode = string.Empty;
                                                bool bHaveCodeAndPass = false;

                                                //TM.GetEventInfo(sPath, sRefNum, ref sEventCode, ref sEventPass, ref bHaveCodeAndPass);

                                                if (bHaveCodeAndPass == true)
                                                {
                                                    bool bMadeDirs = true;
                                                    //TM.MakeWebDirectories(sRenderedPath, sEventCode, sEventPass, ref bMadeDirs, sDiscType);

                                                    if (bMadeDirs == true)
                                                    {
                                                        bContinue = true;
                                                    }
                                                    //else if (bMadeDirs != true)
                                                    //{
                                                    //    string sStop = string.Empty;
                                                    //    bContinue = false;

                                                    //    string sErrorDescription = "[" + DateTime.Now.ToString() + " ][ Failed to create web directories.]";

                                                    //    TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sSitting);

                                                    //    string sStatus = "90";

                                                    //    TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                                    //}
                                                }
                                                else if (bHaveCodeAndPass != true)
                                                {
                                                    // Continue.
                                                }

                                                bContinue = true;

                                                if (bContinue == true)
                                                {
                                                    // Update DiscOrders.Status = 40 (Images have been rendered.)
                                                    string sStatus = "40";

                                                    TM.UpdateDiscOrdersTableStatusSittingBased(sRefNum, sStatus, sDiscType);

                                                    DataTable dTblGenericCopyrightRelease = new DataTable("dTblGenericCopyrightRelease");
                                                    sCommText = "SELECT [Value] FROM [Variables] WHERE [Label] = 'GenericCopyrightRelease'";

                                                    TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblGenericCopyrightRelease);

                                                    if (dTblGenericCopyrightRelease.Rows.Count > 0)
                                                    {
                                                        string sGenericCopyrightRelease = Convert.ToString(dTblGenericCopyrightRelease.Rows[0]["Value"]).Trim();
                                                        string sDestFilePath = sRenderedPath + Path.GetFileName(sGenericCopyrightRelease).Trim();

                                                        if (!File.Exists(sDestFilePath))
                                                        {
                                                            File.Copy(sGenericCopyrightRelease, sDestFilePath);
                                                        }                                                        

                                                        DataTable dTblRightsRelease = new DataTable("dTblRightsRelease");
                                                        sCommText = "SELECT [Value] FROM [Variables] WHERE [Label] = 'CopyrightRelease'";

                                                        TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblRightsRelease);

                                                        if (dTblRightsRelease.Rows.Count > 0)
                                                        {
                                                            string sRightsReleaseText = Convert.ToString(dTblRightsRelease.Rows[0]["Value"]);
                                                            string sRightsReleasePath = sRenderedPath + "rights-release.txt";

                                                            if (!File.Exists(sRightsReleasePath))
                                                            {
                                                                File.WriteAllText(sRightsReleasePath, sRightsReleaseText);
                                                            }                                                            

                                                            bAllGood = true;
                                                        }
                                                        else if (dTblRightsRelease.Rows.Count == 0)
                                                        {
                                                            string sStop = string.Empty;
                                                            bAllGood = false;

                                                            string sErrorDescription = "[" + DateTime.Now.ToString() + " ][ Rights release data not collected from table.]";

                                                            TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sSitting, bSittingBased);

                                                            sStatus = "90";

                                                            TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                                        }
                                                    }
                                                    else if (dTblGenericCopyrightRelease.Rows.Count == 0)
                                                    {
                                                        string sStop = string.Empty;
                                                        bAllGood = false;

                                                        string sErrorDescription = "[" + DateTime.Now.ToString() + " ][ Generic copyright release not located in table.]";

                                                        TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sSitting, bSittingBased);

                                                        sStatus = "90";

                                                        TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                                    }
                                                }
                                                else if (bContinue != true)
                                                {
                                                    string sStop = string.Empty;
                                                    bAllGood = false;
                                                }
                                            }
                                            else if (iDirRenderedCount != iNeededRenderedImageCount)
                                            {
                                                // Continue in event of renders not being complete at this point.
                                            }
                                        }
                                        else if (!File.Exists(sMergeFile))
                                        {
                                            string sStop = string.Empty;
                                            bAllGood = false;

                                            string sErrorDescription = "[" + DateTime.Now.ToString() + " ][ Merge file could not be located.]";

                                            TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sSitting, bSittingBased);

                                            string sStatus = "90";

                                            TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                        }
                                    }
                                    else if (!Directory.Exists(sRenderedPath))
                                    {
                                        string sStop = string.Empty;
                                        bAllGood = false;

                                        Directory.CreateDirectory(sRenderedPath);
                                    }
                                }
                                else if (dTblFrames.Rows.Count == 0)
                                {
                                    string sStop = string.Empty;
                                    bAllGood = false;

                                    string sErrorDescription = "[" + DateTime.Now.ToString() + " ][ No frame data.]";

                                    TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sSitting, bSittingBased);

                                    string sStatus = "90";

                                    TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                }

                                if (bAllGood == true)
                                {
                                    bool bNWPMoved = false;

                                    this.MoveNWPtoReady(sRefNum, sFrameNum, sRenderedPath, ref bNWPMoved, sDiscType, sSitting, bSittingBased);

                                    if (bNWPMoved == true)
                                    {
                                        sText = "[Moved NWP file to initiate disc production for reference # " + sRefNum + ", frame # " + sFrameNum + " and sitting " + sSitting.Trim() + ".]";
                                        this.LogText(sText);

                                        dTblCheckedForRenderedImages.Rows.Add(sRefNum);
                                    }
                                    else if (bNWPMoved != true)
                                    {
                                        string sStop = string.Empty;

                                        string sErrorDescription = "[" + DateTime.Now.ToString() + " ][ Failed to move NWP file to initiate disc production.]";

                                        TM.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sSitting, bSittingBased);

                                        string sStatus = "90";

                                        TM.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                    }
                                }
                                else if (bAllGood != true)
                                {
                                    string sStop = string.Empty;
                                }
                            }

                            // End of foreach through DiscOrders for status = 35

                            if (bAllGood == true)
                            {
                                string sCheckedRefNum = string.Empty;

                                var vUniqueRefNum = dTblCheckedForRenderedImages.AsEnumerable()
                                    .Select(row => new
                                    {
                                        CheckedRefNum = row.Field<string>("RefNum")
                                    })
                                    .Distinct();

                                foreach (var v in vUniqueRefNum)
                                {
                                    sCheckedRefNum = Convert.ToString(v.CheckedRefNum).Trim();

                                    // Update DiscOrders.Status = 60 (NWP files moved to initiate processing of the disc. Completed.)
                                    string sStatus2 = "60";
                                    TM.UpdateDiscOrdersTableStatusSittingBased(sCheckedRefNum, sStatus2, sDiscType);
                                }
                            }
                            else if (bAllGood != true)
                            {
                                string sStop = string.Empty;
                            }
                        }
                        else if (dTblDiscOrders.Rows.Count == 0)
                        {
                            // Continue.
                        }
                    }
                }
                else if (dTblGatherDiscTypes.Rows.Count == 0)
                {
                    string sStop = string.Empty;
                }
            }
            catch (Exception ex)
            {
                TM.SaveExceptionToDB(ex);
            }
        }

        // Move the NWP files to the burnders hot folder for disc production.
        private void MoveNWPtoReady(string sRefNum, string sFrameNum, string sRenderedPath, ref bool bNWPMoved, string sDiscType, string sSitting, bool bSittingBased)
        {
            try
            {
                string sFile = string.Empty;
                string sCommText = string.Empty;                

                string sDTime1 = DateTime.Now.ToString("MM-dd-yy").Trim();
                string sDTime2 = DateTime.Now.ToString("HH:mm:ss").Trim();
                string sDTime3 = "[" + sDTime1 + "][" + sDTime2 + "]";

                if (bSittingBased != true)
                {
                    sLogText = sDTime3 + "[Moving NWP file for reference number " + sRefNum + " and frame number " + sFrameNum + " to disc publisher.]";
                }
                else if (bSittingBased == true)
                {
                    sLogText = sDTime3 + "[Moving NWP file for reference number " + sRefNum + " and sitting number " + sSitting.Trim() + " to disc publisher.]";
                }
                
                this.SetLogText(sLogText, sDTime3);
                sbLog.AppendFormat(sLogText);
                sbLog.Append(Environment.NewLine);

                string sReadyDirFilePath = string.Empty;


                string sReadyDir1 = string.Empty;
                string sReadyDir2 = string.Empty;
                string sReadyDir3 = string.Empty;
                string sReadyDir4 = string.Empty;
                string sSendHereFile = string.Empty;
                bool bSuccess = true;

                TM.GatherNWPFileVariables(ref sReadyDir1, ref sReadyDir2, ref sReadyDir3, ref sReadyDir4, ref sSendHereFile, ref bSuccess);

                if (bSuccess == true)
                {
                    bool bDirFound = false;

                    if (bSittingBased != true)
                    {
                        sFile = sDiscType + "_" + sRefNum + "_" + sFrameNum + ".NWP";
                    }
                    else if (bSittingBased == true)
                    {
                        sFile = sDiscType + "_" + sRefNum + "_" + sSitting.Trim() + ".NWP";
                    }
                    
                    sRenderedPath += sFile;

                    DataTable dTbl = new DataTable();

                    if (bSittingBased != true)
                    {
                        sCommText = "SELECT [NWPFileData] FROM [DiscOrders] WHERE [RefNum] = '" + sRefNum + "' AND [FrameNum] = '" + sFrameNum + "' AND [DiscType] = '" + sDiscType + "'";
                    }
                    else if (bSittingBased == true)
                    {
                        sCommText = "SELECT [NWPFileData] FROM [DiscOrders] WHERE [RefNum] = '" + sRefNum + "' AND [Sitting] = '" + sSitting.Trim() + "' AND [DiscType] = '" + sDiscType + "'";
                    }

                    TM.SQLQuery(sDiscProcessorConnString, sCommText, dTbl);

                    if (dTbl.Rows.Count > 0)
                    {
                        string sNWPFile = Convert.ToString(dTbl.Rows[0]["NWPFileData"]).Trim();

                        File.WriteAllText(sRenderedPath, sNWPFile);

                        if (File.Exists(sRenderedPath))
                        {
                            if (bDirFound == false && (File.Exists(sReadyDir1 + sSendHereFile)))
                            {
                                sReadyDirFilePath = sReadyDir1 += sFile;

                                if (File.Exists(sReadyDirFilePath))
                                {
                                    File.Delete(sReadyDirFilePath);
                                    File.Copy(sRenderedPath, sReadyDirFilePath);
                                    File.Delete(sRenderedPath);

                                    bDirFound = true;
                                    bNWPMoved = true;
                                }
                                else if (!File.Exists(sReadyDirFilePath))
                                {
                                    File.Copy(sRenderedPath, sReadyDirFilePath);
                                    File.Delete(sRenderedPath);

                                    bDirFound = true;
                                    bNWPMoved = true;
                                }
                            }
                            if (bDirFound == false && File.Exists(sReadyDir2 + sSendHereFile))
                            {
                                sReadyDirFilePath = sReadyDir2 += sFile;

                                if (File.Exists(sReadyDirFilePath))
                                {
                                    File.Delete(sReadyDirFilePath);
                                    File.Copy(sRenderedPath, sReadyDirFilePath);
                                    File.Delete(sRenderedPath);

                                    bDirFound = true;
                                    bNWPMoved = true;
                                }
                                else if (!File.Exists(sReadyDirFilePath))
                                {
                                    File.Copy(sRenderedPath, sReadyDirFilePath);
                                    File.Delete(sRenderedPath);

                                    bDirFound = true;
                                    bNWPMoved = true;
                                }
                            }
                            if (bDirFound == false && File.Exists(sReadyDir3 + sSendHereFile))
                            {
                                sReadyDirFilePath = sReadyDir3 += sFile;

                                if (File.Exists(sReadyDirFilePath))
                                {
                                    File.Delete(sReadyDirFilePath);
                                    File.Copy(sRenderedPath, sReadyDirFilePath);
                                    File.Delete(sRenderedPath);

                                    bDirFound = true;
                                    bNWPMoved = true;
                                }
                                else if (!File.Exists(sReadyDirFilePath))
                                {
                                    File.Copy(sRenderedPath, sReadyDirFilePath);
                                    File.Delete(sRenderedPath);

                                    bDirFound = true;
                                    bNWPMoved = true;
                                }
                            }
                            if (bDirFound == false && File.Exists(sReadyDir4 + sSendHereFile))
                            {
                                sReadyDirFilePath = sReadyDir4 += sFile;

                                if (File.Exists(sReadyDirFilePath))
                                {
                                    File.Delete(sReadyDirFilePath);
                                    File.Copy(sRenderedPath, sReadyDirFilePath);
                                    File.Delete(sRenderedPath);

                                    bDirFound = true;
                                    bNWPMoved = true;
                                }
                                else if (!File.Exists(sReadyDirFilePath))
                                {
                                    File.Copy(sRenderedPath, sReadyDirFilePath);
                                    File.Delete(sRenderedPath);

                                    bDirFound = true;
                                    bNWPMoved = true;
                                }
                            }
                            else if (bDirFound == false)
                            {
                                DataTable dTblDiscTypeVariables = new DataTable("dTblDiscTypeVariables");
                                sCommText = "SELECT * FROM [DiscTypes] WHERE [DiscType] = '" + sDiscType + "'";

                                TM.SQLQuery(sDiscProcessorConnString, sCommText, dTblDiscTypeVariables);

                                if (dTblDiscTypeVariables.Rows.Count > 0)
                                {
                                    string sDefaultDir = Convert.ToString(dTblDiscTypeVariables.Rows[0]["DefaultReadyDir"]).Trim();

                                    sReadyDirFilePath = sDefaultDir += sFile;

                                    if (File.Exists(sReadyDirFilePath))
                                    {
                                        File.Delete(sReadyDirFilePath);
                                        File.Copy(sRenderedPath, sReadyDirFilePath);
                                        File.Delete(sRenderedPath);

                                        bDirFound = true;
                                        bNWPMoved = true;
                                    }
                                    else if (!File.Exists(sReadyDirFilePath))
                                    {
                                        File.Copy(sRenderedPath, sReadyDirFilePath);
                                        File.Delete(sRenderedPath);

                                        bDirFound = true;
                                        bNWPMoved = true;
                                    }
                                }
                                else if (dTblDiscTypeVariables.Rows.Count == 0)
                                {
                                    string sStop = string.Empty;
                                    bNWPMoved = false;
                                }
                            }
                        }
                        else if (!File.Exists(sRenderedPath))
                        {
                            string sStop = string.Empty;
                            bNWPMoved = false;
                        }
                    }
                    else if (dTbl.Rows.Count == 0)
                    {
                        string sStop = string.Empty;
                        bNWPMoved = false;
                    }
                }
                else if (bSuccess != true)
                {
                    string sStop = string.Empty;
                    bNWPMoved = false;
                }
            }
            catch(Exception ex)
            {
                TM.SaveExceptionToDB(ex);
                bNWPMoved = false;
            }
        }

        #endregion

        #region Form based task methods.

        // Clear global variables.
        private void Clear()  
        {
            try
            {
                sLogText = string.Empty;
                sbLog.Clear();
                rtxtboxLog.Clear();
                rtxtboxLog.Refresh();
                sbLog.Clear();
            }
            catch (Exception ex)
            {
                TM.SaveExceptionToDB(ex);
            }
        }

        // Set text for the forms rich text box.
        private void SetLogText(string sLogText, string sDTime3)
        {
            try
            {
                rtxtboxLog.AppendText(sLogText + Environment.NewLine);
            }
            catch(Exception ex)
            {
                TM.SaveExceptionToDB(ex);
            }
        }

        // Calls to this method will trigger another method which will export the rich text box text to a log file.
        private void SetLogTextFinished(string sLogText)
        {
            try
            {
                rtxtboxLog.AppendText(sLogText + Environment.NewLine);
                sbLog.AppendFormat(sLogText);
                sbLog.Append(Environment.NewLine);
                this.ExportLogTextToFile();
            }
            catch(Exception ex)
            {
                TM.SaveExceptionToDB(ex);
            }
        }

        // Exports the rich text box text to a log file.
        private void ExportLogTextToFile()
        {
            try
            {
                string sDTime1 = DateTime.Now.ToString("MM-dd-yy").Trim();
                string sDTime2 = DateTime.Now.ToString("HHmmss").Trim();
                string sDTime3 = "[" + sDTime1 + "][" + sDTime2 + "]";

                string sLabel = "DPLogFilePath";
                string sValue = "Value";
                string sVariable = string.Empty;
                bool bSuccess = true;

                TM.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                if (bSuccess == true)
                {
                    string sLogPath = sVariable;
                    string sLogFile = sLogPath + sDTime3 + "-DiscProcessorLog.txt";
                    int iMaxLogFileSize = 10485760; // 10MB
                    int iFileSize = 0;

                    string sCurrentLogFile = string.Empty;

                    sLabel = "CurrentDPLogFile";
                    sValue = "Value";
                    sVariable = string.Empty;
                    bSuccess = true;

                    TM.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                    if (bSuccess == true)
                    {
                        sCurrentLogFile = sVariable;

                        if (!Directory.Exists(sLogPath))
                        {
                            Directory.CreateDirectory(sLogPath);
                        }

                        if (sCurrentLogFile != string.Empty && (!File.Exists(sCurrentLogFile))) // Log file previously created and recorded into the table is no longer on server.
                        {
                            File.WriteAllText(sLogFile, sbLog.ToString());

                            sCurrentLogFile = sLogFile;

                            string sCommText = "UPDATE [Variables] SET [Value] = '" + sCurrentLogFile + "' WHERE [Label] = 'CurrentDPLogFile'";
                            bSuccess = true;

                            TM.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                            if (bSuccess == true)
                            {

                            }
                            else if (bSuccess != true)
                            {
                                MessageBox.Show("SQL INSERT/UPDATE Failed : " + Environment.NewLine + sCommText);
                            }
                        }
                        if (sCurrentLogFile != string.Empty && File.Exists(sCurrentLogFile)) // Log file recorded in table and exists on server.
                        {
                            long lCurrentLogFile = new FileInfo(sCurrentLogFile).Length;
                            if (lCurrentLogFile >= iMaxLogFileSize)
                            {
                                string sAppendingText = Environment.NewLine + "Log file has reached maximum size. Creating a new log file.";
                                File.AppendAllText(sCurrentLogFile, sAppendingText);

                                File.WriteAllText(sLogFile, sbLog.ToString());
                                sCurrentLogFile = sLogFile;

                                string sCommText = "UPDATE [Variables] SET [Value] = '" + sCurrentLogFile + "' WHERE [Label] = 'CurrentDPLogFile'";
                                bSuccess = true;

                                TM.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                                if (bSuccess == true)
                                {

                                }
                                else if (bSuccess != true)
                                {
                                    MessageBox.Show("SQL INSERT/UPDATE Failed : " + Environment.NewLine + sCommText);
                                }
                            }
                            else if (iFileSize <= iMaxLogFileSize) // Log file recorded has reached 10MB size limit.
                            {
                                File.AppendAllText(sCurrentLogFile, sbLog.ToString());

                                File.Copy(sCurrentLogFile, sLogFile);
                                File.Delete(sCurrentLogFile);
                                sCurrentLogFile = sLogFile;

                                string sCommText = "UPDATE [Variables] SET [Value] = '" + sCurrentLogFile + "' WHERE [Label] = 'CurrentDPLogFile'";
                                bSuccess = true;

                                TM.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                                if (bSuccess == true)
                                {

                                }
                                else if (bSuccess != true)
                                {
                                    MessageBox.Show("SQL INSERT/UPDATE Failed : " + Environment.NewLine + sCommText);
                                }
                            }
                        }
                        else if (sCurrentLogFile == string.Empty) // No log file recorded in table.
                        {
                            File.WriteAllText(sLogFile, sbLog.ToString());

                            sCurrentLogFile = sLogFile;

                            string sCommText = "UPDATE [Variables] SET [Value] = '" + sCurrentLogFile + "' WHERE [Label] = 'CurrentDPLogFile'";
                            bSuccess = true;

                            TM.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                            if (bSuccess == true)
                            {

                            }
                            else if (bSuccess != true)
                            {
                                MessageBox.Show("SQL INSERT/UPDATE Failed : " + Environment.NewLine + sCommText);
                            }
                        }
                    }
                    else if (bSuccess != true)
                    {
                        string sStop = string.Empty;
                    }
                }
                else if (bSuccess != true)
                {
                    string sStop = string.Empty;
                }
            }
            catch(Exception ex)
            {
                TM.SaveExceptionToDB(ex);
            }
        }

        // Sets the forms richtextbox text.
        private void LogText(string sText)
        {
            try
            {
                string sDTime1 = DateTime.Now.ToString("MM-dd-yy").Trim();
                string sDTime2 = DateTime.Now.ToString("HH:mm:ss").Trim();
                string sDTime3 = "[" + sDTime1 + "][" + sDTime2 + "]";
                sLogText = sDTime3 + sText;
                this.SetLogText(sLogText, sDTime3);
                sbLog.AppendFormat(sLogText);
                sbLog.Append(Environment.NewLine);

                Application.DoEvents();
            }
            catch(Exception ex)
            {
                TM.SaveExceptionToDB(ex);
            }
        }

        #endregion
    }
}
