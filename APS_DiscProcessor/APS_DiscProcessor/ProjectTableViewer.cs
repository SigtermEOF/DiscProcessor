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
    public partial class ProjectTableViewer : Form
    {
        TaskMethods TM03 = null;
        string sCDSConnString = APS_DiscProcessor.Properties.Settings.Default.CDSConnString.ToString();
        string sDiscProcessorConnString = APS_DiscProcessor.Properties.Settings.Default.DiscProcessorConnString.ToString();
        DataTable dTblVariables = new DataTable("dTblVariables");
        DataTable dTblChangeLog = new DataTable("dTblChangeLog");
        DataTable dTblDiscTypes = new DataTable("dTblDiscTypes");
        DataTable dTblCustomerLabels = new DataTable("dTblCustomerLabels");
        DataTable dTblStatus = new DataTable("dTblStatus");
        DataTable dTblErrors = new DataTable("dTblErrors");
        List<string> lTables = new List<string>();
        bool bVariables = false;
        bool bChangeLog = false;
        bool bDiscTypes = false;
        bool bCustomerLabels = false;
        bool bStatus = false;
        bool bErrors = false;
        StringBuilder sBuilder = new StringBuilder();
        string sSBLogText = string.Empty;

        public ProjectTableViewer()
        {
            InitializeComponent();

            TM03 = new TaskMethods();
        }

        #region Form events.

        private void ChangeVariables_Load(object sender, EventArgs e)
        {
            // TODO: This line of code loads data into the 'discProcessorDataSet.Errors' table. You can move, or remove it, as needed.
            this.errorsTableAdapter.Fill(this.discProcessorDataSet.Errors);
            dTblErrors = this.discProcessorDataSet.Errors.Copy();
            // TODO: This line of code loads data into the 'discProcessorDataSet.Status' table. You can move, or remove it, as needed.
            this.statusTableAdapter.Fill(this.discProcessorDataSet.Status);
            dTblStatus = this.discProcessorDataSet.Status.Copy();
            // TODO: This line of code loads data into the 'discProcessorDataSet.CustomerLabels' table. You can move, or remove it, as needed.
            this.customerLabelsTableAdapter.Fill(this.discProcessorDataSet.CustomerLabels);
            dTblCustomerLabels = this.discProcessorDataSet.CustomerLabels.Copy();
            // TODO: This line of code loads data into the 'discProcessorDataSet.DiscTypes' table. You can move, or remove it, as needed.
            this.discTypesTableAdapter.Fill(this.discProcessorDataSet.DiscTypes);
            dTblDiscTypes = this.discProcessorDataSet.DiscTypes.Copy();
            // TODO: This line of code loads data into the 'discProcessorDataSet.Variables' table. You can move, or remove it, as needed.
            this.variablesTableAdapter.Fill(this.discProcessorDataSet.Variables);
            dTblVariables = this.discProcessorDataSet.Variables.Copy();
            // TODO: This line of code loads data into the 'discProcessorDataSet1.ChangeLog' table. You can move, or remove it, as needed.
            this.changeLogTableAdapter.Fill(this.discProcessorDataSet.ChangeLog);
            dTblChangeLog = this.discProcessorDataSet.ChangeLog.Copy();

            lTables.Add("Variables");
            lTables.Add("ChangeLog");
            lTables.Add("DiscTypes");
            lTables.Add("CustomerLabels");
            lTables.Add("Status");
            lTables.Add("Errors");

            this.cBoxTables.DataSource = lTables;
            this.cBoxTables.SelectedIndex = 0;

            this.btnSave.Enabled = false;
            this.btnSave.Visible = false;
            Color colorMyBG = Color.FromArgb(30, 30, 30);
            this.dGV01.DefaultCellStyle.ForeColor = Color.AntiqueWhite;
            this.dGV01.DefaultCellStyle.BackColor = colorMyBG;
            this.dGV01.DefaultCellStyle.Font = new Font("Arial Bold", 10F, FontStyle.Regular);
            this.dGV01.RowHeadersDefaultCellStyle.Font = new Font("Arial Bold", 10F, FontStyle.Regular);
            this.dGV01.ColumnHeadersDefaultCellStyle.Font = new Font("Arial Bold", 10F, FontStyle.Regular);
            this.dGV01.EditMode = DataGridViewEditMode.EditOnEnter;
            this.dGV01.ReadOnly = true;
            this.dGV01.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            this.dGV01.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            this.dGV01.Refresh();
            Application.DoEvents();
        }

        private void chkBoxEditMode_CheckedChanged(object sender, EventArgs e)
        {
            if (chkBoxEditMode.CheckState == CheckState.Checked)
            {
                DialogResult dResult = new DialogResult();
                dResult = MessageBox.Show("This will allow editing and saving of the datatable to the database. " + Environment.NewLine + Environment.NewLine + "Are you sure?", "Enter Edit Mode?", MessageBoxButtons.YesNo);

                if (dResult == DialogResult.Yes)
                {
                    this.btnSave.Enabled = true;
                    this.btnSave.Visible = true;
                    this.dGV01.ReadOnly = false;
                    
                    this.dGV01.Refresh();
                    Application.DoEvents();
                }
                else if (dResult == DialogResult.No)
                {
                    this.chkBoxEditMode.CheckState = CheckState.Unchecked;
                    return;
                }
            }
            else if (chkBoxEditMode.CheckState != CheckState.Checked)
            {
                this.btnSave.Enabled = false;
                this.btnSave.Visible = false;
                this.dGV01.ReadOnly = true;
                
                this.dGV01.Refresh();
                Application.DoEvents();
            }
        }

        private void cBoxTables_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cBoxTables.SelectedIndex == 0)
            {
                bErrors = false;
                bStatus = false;
                bCustomerLabels = false;
                bDiscTypes = false;
                bChangeLog = false;
                bVariables = true;
                this.dGV01.DataSource = this.discProcessorDataSet.Variables;
                this.dGV01.AllowUserToAddRows = true;
                this.ResizeDGV();
                this.dGV01.Refresh();
                chkBoxEditMode.Enabled = true;
                chkBoxEditMode.Visible = true;
                Application.DoEvents();
               
            }
            else if (cBoxTables.SelectedIndex == 1)
            {
                bErrors = false;
                bStatus = false;
                bCustomerLabels = false;
                bDiscTypes = false;
                bVariables = false;
                bChangeLog = true;
                this.dGV01.DataSource = this.discProcessorDataSet.ChangeLog;
                this.dGV01.AllowUserToAddRows = true;
                this.ResizeDGV();
                this.dGV01.Refresh();
                chkBoxEditMode.Enabled = true;
                chkBoxEditMode.Visible = true;
                Application.DoEvents();                
            }
            else if (cBoxTables.SelectedIndex == 2)
            {
                bErrors = false;
                bStatus = false;
                bCustomerLabels = false;
                bChangeLog = false;
                bVariables = false;
                bDiscTypes = true;
                this.dGV01.DataSource = this.discProcessorDataSet.DiscTypes;
                this.dGV01.AllowUserToAddRows = false;
                this.ResizeDGV();
                this.dGV01.Refresh();
                chkBoxEditMode.Enabled = false;
                chkBoxEditMode.Visible = false;
                Application.DoEvents();
            }
            else if (cBoxTables.SelectedIndex == 3)
            {
                bErrors = false;
                bStatus = false;
                bChangeLog = false;
                bVariables = false;
                bDiscTypes = false;
                bCustomerLabels = true;
                this.dGV01.DataSource = this.discProcessorDataSet.CustomerLabels;
                this.dGV01.AllowUserToAddRows = false;
                this.ResizeDGV();
                this.dGV01.Refresh();
                chkBoxEditMode.Enabled = false;
                chkBoxEditMode.Visible = false;
                Application.DoEvents();

            }
            else if (cBoxTables.SelectedIndex == 4)
            {
                bErrors = false;
                bChangeLog = false;
                bVariables = false;
                bDiscTypes = false;
                bCustomerLabels = false;
                bStatus = true;
                this.dGV01.DataSource = this.discProcessorDataSet.Status;
                this.dGV01.AllowUserToAddRows = false;
                this.ResizeDGV();
                this.dGV01.Refresh();
                chkBoxEditMode.Enabled = false;
                chkBoxEditMode.Visible = false;
                Application.DoEvents();
            }
            else if (cBoxTables.SelectedIndex == 5)
            {
                bChangeLog = false;
                bVariables = false;
                bDiscTypes = false;
                bCustomerLabels = false;
                bStatus = false;
                bErrors = true;
                this.dGV01.DataSource = this.discProcessorDataSet.Errors;
                this.dGV01.AllowUserToAddRows = false;
                this.ResizeDGV();
                this.dGV01.Refresh();
                chkBoxEditMode.Enabled = false;
                chkBoxEditMode.Visible = false;
                Application.DoEvents();
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                #region Variables table. 

                if (bVariables == true)
                {
                    if (this.discProcessorDataSet.Variables.Rows.Count > 0)
                    {
                        this.discProcessorDataSet.Variables.AcceptChanges();
                        this.dGV01.Refresh();
                        Application.DoEvents();

                        foreach (DataRow dRowVariables in this.discProcessorDataSet.Variables.Rows)
                        {
                            string sLabel = Convert.ToString(dRowVariables["Label"]).Trim();
                            string sValue = Convert.ToString(dRowVariables["Value"]).Trim();

                            DataTable dTblVariablesValue = new DataTable("dTblVariablesValue");
                            string sCommText = "SELECT * FROM [Variables] WHERE [Label] = '" + sLabel + "'";

                            TM03.SQLQuery(sDiscProcessorConnString, sCommText, dTblVariablesValue);

                            if (dTblVariablesValue.Rows.Count > 0)
                            {
                                string sCurrentValue = Convert.ToString(dTblVariablesValue.Rows[0]["Value"]).Trim();

                                if (sCurrentValue != sValue)
                                {
                                    string sText = "Current value: " + sCurrentValue + Environment.NewLine + "Entered value: " + sValue + ".";

                                    DialogResult dResult = MessageBox.Show(sText + Environment.NewLine + Environment.NewLine + "Are you sure?", "Apply changes?", MessageBoxButtons.YesNo);

                                    if (dResult == DialogResult.Yes)
                                    {
                                        sCommText = "UPDATE [Variables] SET [Value] = '" + sValue + "' WHERE [Label] = '" + sLabel + "'";
                                        bool bSuccess = false;

                                        TM03.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                                        if (bSuccess == true)
                                        {
                                            string sSBText = "[Updating of the Variables table succeeded.]" + Environment.NewLine + Environment.NewLine +
                                                "DialogResult.sText = (" + Environment.NewLine + Environment.NewLine + sText + ")" + Environment.NewLine + Environment.NewLine +
                                                "sCommText = (" + sCommText + ")" + Environment.NewLine + Environment.NewLine;
                                            this.AppendToStringBuilder(sSBText);

                                            MessageBox.Show("Successfully updated record.");
                                            this.dGV01.Refresh();
                                            Application.DoEvents();
                                        }
                                        else if (bSuccess != true)
                                        {
                                            string sSBText = "[Attempt to update the Variables table failed:]" + Environment.NewLine + Environment.NewLine +
                                                "DialogResult.sText = (" + Environment.NewLine + Environment.NewLine + sText + ")" + Environment.NewLine + Environment.NewLine +
                                                "sCommText = (" + sCommText + ")" + Environment.NewLine + Environment.NewLine;
                                            this.AppendToStringBuilder(sSBText);

                                            MessageBox.Show("Failed to update the record.");
                                            this.discProcessorDataSet.Variables.Clear();
                                            this.variablesTableAdapter.Fill(this.discProcessorDataSet.Variables);
                                            this.dGV01.DataSource = this.discProcessorDataSet.Variables;
                                            this.dGV01.Refresh();
                                            Application.DoEvents();
                                        }
                                    }
                                    else if (dResult == DialogResult.No)
                                    {
                                        return;
                                    }
                                }
                                else if (sCurrentValue == sValue)
                                {
                                    // Ignore.
                                }
                            }
                            else if (dTblVariablesValue.Rows.Count == 0)
                            {
                                string sStop = string.Empty;

                                sCommText = "INSERT INTO [Variables] ([Label], [Value]) VALUES ( '" + sLabel + "', '" + sValue + "')";
                                bool bSuccess = false;

                                string sText = "New row values: " + Environment.NewLine + Environment.NewLine + "Label: " + sLabel + Environment.NewLine + "Value: " + sValue +
                                    Environment.NewLine + Environment.NewLine + "Do you wish to run the following INSERT command? " + Environment.NewLine + Environment.NewLine +
                                    sCommText;

                                DialogResult dResult = new DialogResult();
                                dResult = MessageBox.Show(sText + Environment.NewLine + Environment.NewLine + "Are you sure?", "Run INSERT command?", MessageBoxButtons.YesNo);

                                if (dResult == DialogResult.Yes)
                                {
                                    TM03.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                                    if (bSuccess == true)
                                    {
                                        string sSBText = "[Updating of the Variables table succeeded.]" + Environment.NewLine + Environment.NewLine +
                                            "DialogResult.sText = (" + Environment.NewLine + Environment.NewLine + sText + ")" + Environment.NewLine + Environment.NewLine +
                                            "sCommText = (" + sCommText + ")" + Environment.NewLine + Environment.NewLine;
                                        this.AppendToStringBuilder(sSBText);

                                        MessageBox.Show("The following INSERT command succeeded: " + Environment.NewLine + Environment.NewLine + sCommText);
                                        this.dGV01.Refresh();
                                        Application.DoEvents();
                                    }
                                    else if (bSuccess != true)
                                    {
                                        string sSBText = "[Attempt to update the Variables table failed:]" + Environment.NewLine + Environment.NewLine +
                                            "DialogResult.sText = (" + Environment.NewLine + Environment.NewLine + sText + ")" + Environment.NewLine + Environment.NewLine +
                                            "sCommText = " + sCommText + ")" + Environment.NewLine + Environment.NewLine;
                                        this.AppendToStringBuilder(sSBText);

                                        MessageBox.Show("The following INSERT command failed: " + Environment.NewLine + Environment.NewLine + sCommText);
                                        this.discProcessorDataSet.Variables.Clear();
                                        this.variablesTableAdapter.Fill(this.discProcessorDataSet.Variables);
                                        this.dGV01.DataSource = this.discProcessorDataSet.Variables;
                                        this.dGV01.Refresh();
                                        Application.DoEvents();
                                    }
                                }
                                else if (dResult == DialogResult.No)
                                {
                                    return;
                                }
                            }
                        }
                    }
                    else if (dTblVariables.Rows.Count == 0)
                    {
                        string sStop = string.Empty;
                    }
                }

                #endregion

                #region ChangeLog table. 

                else if (bChangeLog == true)
                {
                    if (this.discProcessorDataSet.ChangeLog.Rows.Count > 0)
                    {
                        this.discProcessorDataSet.ChangeLog.AcceptChanges();
                        this.dGV01.Refresh();
                        Application.DoEvents();

                        foreach (DataRow dRowChangeLog in this.discProcessorDataSet.ChangeLog.Rows)
                        {
                            string sApp = Convert.ToString(dRowChangeLog["App"]).Trim();
                            string sVersion = Convert.ToString(dRowChangeLog["Version"]).Trim();
                            DateTime dTimeDate = Convert.ToDateTime(dRowChangeLog["Date"]).Date;
                            string sDTimeDate = dTimeDate.ToString("yyyy-MM-dd").Trim();
                            string sDev = Convert.ToString(dRowChangeLog["Dev"]).Trim();
                            string sChanges = Convert.ToString(dRowChangeLog["Changes"]).Trim();

                            DataTable dTblVariablesValue = new DataTable("dTblVariablesValue");
                            string sCommText = "SELECT * FROM [ChangeLog] WHERE [App] = '" + sApp + "' AND [Version] = '" + sVersion + "'";

                            TM03.SQLQuery(sDiscProcessorConnString, sCommText, dTblVariablesValue);

                            if (dTblVariablesValue.Rows.Count > 0)
                            {
                                string sCurrentApp = Convert.ToString(dTblVariablesValue.Rows[0]["App"]).Trim();
                                string sCurrentVersion = Convert.ToString(dTblVariablesValue.Rows[0]["Version"]).Trim();
                                DateTime dTimeCurrentDate = Convert.ToDateTime(dTblVariablesValue.Rows[0]["Date"]).Date;
                                string sDTimeCurrentDate = dTimeCurrentDate.ToString("yyyy-MM-dd").Trim();
                                string sCurrentDev = Convert.ToString(dTblVariablesValue.Rows[0]["Dev"]).Trim();
                                string sCurrentChanges = Convert.ToString(dTblVariablesValue.Rows[0]["Changes"]).Trim();

                                if (sDTimeCurrentDate != sDTimeDate || sCurrentDev != sDev || sCurrentChanges != sChanges)
                                {
                                    string sText = "Stored [Date]: " + dTimeCurrentDate.ToString("yyyy-MM-dd").Trim() + Environment.NewLine + "[Date] to save: " + dTimeCurrentDate.ToString("yyyy-MM-dd").Trim() + Environment.NewLine +
                                        "Stored [Dev]: " + sCurrentDev + Environment.NewLine + "[Dev] to Save: " + sDev + Environment.NewLine +
                                        "Stored [Changes]: " + sCurrentChanges + Environment.NewLine + "[Changes] to save: " + sChanges;

                                    DialogResult dResult = MessageBox.Show(sText + Environment.NewLine + Environment.NewLine + "Are you sure?", "Apply changes?", MessageBoxButtons.YesNo);

                                    if (dResult == DialogResult.Yes)
                                    {
                                        sCommText = "UPDATE [ChangeLog] SET [Version] = '" + sCurrentVersion + "', [Date] = '" + dTimeCurrentDate.ToString("yyyy-MM-dd").Trim() + "', [Dev] = '" +
                                            sCurrentDev + "', [Changes] = '" + sCurrentChanges + "' WHERE [App] = '" + sApp + "' AND [Version] = '" + sVersion + "'";
                                        bool bSuccess = false;

                                        TM03.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                                        if (bSuccess == true)
                                        {
                                            string sSBText = "[Updating of the ChangeLog table succeeded.]" + Environment.NewLine + Environment.NewLine +
                                                "DialogResult.sText = (" + Environment.NewLine + Environment.NewLine + sText + ")" + Environment.NewLine + Environment.NewLine +
                                                "sCommText = (" + sCommText + ")" + Environment.NewLine + Environment.NewLine;
                                            this.AppendToStringBuilder(sSBText);

                                            MessageBox.Show("The following INSERT command succeeded: " + Environment.NewLine + Environment.NewLine + sCommText);
                                            this.discProcessorDataSet.ChangeLog.Clear();
                                            this.changeLogTableAdapter.Fill(this.discProcessorDataSet.ChangeLog);
                                            this.dGV01.DataSource = this.discProcessorDataSet.ChangeLog;
                                            this.dGV01.Refresh();
                                            Application.DoEvents();
                                        }
                                        else if (bSuccess != true)
                                        {
                                            string sSBText = "[Attempt to update the ChangeLog table failed:]" + Environment.NewLine + Environment.NewLine +
                                                "DialogResult.sText = (" + Environment.NewLine + Environment.NewLine + sText + ")" + Environment.NewLine + Environment.NewLine +
                                                "sCommText = " + sCommText + ")" + Environment.NewLine + Environment.NewLine;
                                            this.AppendToStringBuilder(sSBText);

                                            MessageBox.Show("The following INSERT command failed: " + Environment.NewLine + Environment.NewLine + sCommText);
                                            this.discProcessorDataSet.ChangeLog.Clear();
                                            this.changeLogTableAdapter.Fill(this.discProcessorDataSet.ChangeLog);
                                            this.dGV01.DataSource = this.discProcessorDataSet.ChangeLog;
                                            this.dGV01.Refresh();
                                            Application.DoEvents();
                                        }
                                    }
                                    else if (dResult == DialogResult.No)
                                    {
                                        return;
                                    }
                                }
                                else if (dTimeCurrentDate == dTimeDate && sCurrentDev == sDev && sCurrentChanges == sChanges)
                                {
                                    // Ignore.
                                }
                            }
                            else if (dTblVariablesValue.Rows.Count == 0)
                            {
                                string sStop = string.Empty;

                                sCommText = "INSERT INTO [ChangeLog] ([App], [Version], [Date], [Dev], [Changes]) VALUES ('" + sApp + "', '" + sVersion + "', '" + dTimeDate.ToString("yyyy-MM-dd").Trim() +
                                    "', '" + sDev + "', '" + sChanges + "')";
                                bool bSuccess = false;

                                string sText = "New row values: " + Environment.NewLine + Environment.NewLine + "App: " + sApp + Environment.NewLine + "Version: " + sVersion +
                                    Environment.NewLine + "Date: " + dTimeDate.ToString("yyyy-MM-dd").Trim() + Environment.NewLine + "Dev: " + sDev + Environment.NewLine +
                                    "Changes: " + sChanges + Environment.NewLine + Environment.NewLine + "Do you wish to run the following INSERT command? " +
                                    Environment.NewLine + Environment.NewLine + sCommText;

                                DialogResult dResult = new DialogResult();
                                dResult = MessageBox.Show(sText + Environment.NewLine + Environment.NewLine + "Are you sure?", "Run INSERT command?", MessageBoxButtons.YesNo);

                                if (dResult == DialogResult.Yes)
                                {
                                    TM03.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                                    if (bSuccess == true)
                                    {
                                        string sSBText = "[Updating of the ChangeLog table succeeded.]" + Environment.NewLine + Environment.NewLine +
                                            "DialogResult.sText = (" + Environment.NewLine + Environment.NewLine + sText + ")" + Environment.NewLine + Environment.NewLine +
                                            "sCommText = (" + sCommText + ")" + Environment.NewLine + Environment.NewLine;
                                        this.AppendToStringBuilder(sSBText);

                                        MessageBox.Show("The following INSERT command succeeded: " + Environment.NewLine + Environment.NewLine + sCommText);
                                        this.dGV01.Refresh();
                                        Application.DoEvents();
                                    }
                                    else if (bSuccess != true)
                                    {
                                        string sSBText = "[Attempt to update the ChangeLog table failed:]" + Environment.NewLine + Environment.NewLine +
                                            "DialogResult.sText = (" + Environment.NewLine + Environment.NewLine + sText + ")" + Environment.NewLine + Environment.NewLine +
                                            "sCommText = " + sCommText + ")" + Environment.NewLine + Environment.NewLine;
                                        this.AppendToStringBuilder(sSBText);

                                        MessageBox.Show("The following INSERT command failed: " + Environment.NewLine + Environment.NewLine + sCommText);
                                        this.discProcessorDataSet.ChangeLog.Clear();
                                        this.changeLogTableAdapter.Fill(this.discProcessorDataSet.ChangeLog);
                                        this.dGV01.DataSource = this.discProcessorDataSet.ChangeLog;
                                        this.dGV01.Refresh();
                                        Application.DoEvents();
                                    }
                                }
                                else if (dResult == DialogResult.No)
                                {
                                    return;
                                }
                            }
                        }
                    }
                    else if (this.discProcessorDataSet.ChangeLog.Rows.Count == 0)
                    {
                        string sStop = string.Empty;
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                TM03.SaveExceptionToDB(ex);
            }
        }

        private void dGV01_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            string sStop = string.Empty;
        }

        #endregion

        private void AppendToStringBuilder(string sSBText)
        {
            try
            {
                string sDTime1 = DateTime.Now.ToString("MM-dd-yy").Trim();
                string sDTime2 = DateTime.Now.ToString("HH:mm:ss").Trim();
                string sDTime3 = "[" + sDTime1 + "][" + sDTime2 + "]";
                sSBLogText = sDTime3 + sSBText;
                sBuilder.Append(Environment.NewLine + Environment.NewLine);
                sBuilder.AppendFormat(sSBLogText);
                sBuilder.Append(Environment.NewLine + Environment.NewLine);

                string sCurrentLogFile = string.Empty;
                bool bLogFileExists = false;

                TM03.GetCurrentLogFile(ref sCurrentLogFile, ref bLogFileExists);

                if (bLogFileExists == true)
                {
                    File.AppendAllText(sCurrentLogFile, sBuilder.ToString().Trim());
                    sBuilder.Clear();
                    sSBLogText = string.Empty;
                }
                else if (bLogFileExists != true)
                {
                    string sStop = string.Empty;
                    sBuilder.Clear();
                    sSBLogText = string.Empty;
                }
            }
            catch (Exception ex)
            {
                TM03.SaveExceptionToDB(ex);
            }
        }

        private void Clear()
        {
            try
            {

            }
            catch (Exception ex)
            {
                TM03.SaveExceptionToDB(ex);
            }
        }

        public void ResizeDGV()
        {
            try
            {
                for (int i = 0; i < dGV01.Columns.Count - 1; i++)
                {
                    dGV01.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                }

                dGV01.Columns[dGV01.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

                for (int i = 0; i < dGV01.Columns.Count; i++)
                {
                    int iColWidth = dGV01.Columns[i].Width;
                    dGV01.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    dGV01.Columns[i].Width = iColWidth;
                }

                if (bVariables == true)
                {
                    foreach (DataGridViewRow dGVRow in dGV01.Rows)
                    {
                        string sValue = Convert.ToString(dGVRow.Cells["Label"].Value).Trim();

                        if (sValue == "Admin" || sValue == "AdminMachines")
                        {
                            dGVRow.Visible = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TM03.SaveExceptionToDB(ex);
            }
        }
    }
}
