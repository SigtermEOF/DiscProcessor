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
    public partial class ICDorMEGResubmit : Form
    {
        TaskMethods TMethods = null;
        string sDiscProcessorConnString = APS_DiscProcessor.Properties.Settings.Default.DiscProcessorConnString.ToString();

        // Global variables.
        DataTable dTblDiscOrders = new DataTable("dTblDiscOrders");
        string sText = string.Empty;

        public ICDorMEGResubmit()
        {
            InitializeComponent();

            TMethods = new TaskMethods();
        }

        #region Form events.

        private void Resubmit_Load(object sender, EventArgs e)
        {
            this.btnResubmit.Enabled = false;
            this.cmboBoxFrame.Enabled = false;
            this.chkBoxAllFrames.Enabled = false;
            this.chkBoxAllFrames.Visible = false;

            btnClear.Text = "C" + Environment.NewLine + "L" + Environment.NewLine + "E" + Environment.NewLine + "A" + Environment.NewLine + "R";
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            this.Clear();
        }

        private void btnQuery_Click(object sender, EventArgs e)
        {
            if (txtResubmit.Text != string.Empty || txtResubmit.Text != "")
            {
                dTblDiscOrders.Clear();
                sText = txtResubmit.Text;
                bool bDigitOnly = false;

                TMethods.IsDigitsOnly(sText, ref bDigitOnly);

                if (bDigitOnly == true)
                {                    
                    string sCommText = "SELECT [FrameNum], [ResubmitCount], [DiscType] FROM [DiscOrders] WHERE ([ProdNum] = '" + sText + "' OR [RefNum] = '" + sText + "')";

                    TMethods.SQLQuery(sDiscProcessorConnString, sCommText, dTblDiscOrders);

                    if (dTblDiscOrders.Rows.Count > 0)
                    {
                        cmboBoxFrame.DataSource = dTblDiscOrders;
                        cmboBoxFrame.DisplayMember = "Frame #";
                        cmboBoxFrame.ValueMember = "FrameNum";

                        this.txtResubmit.Enabled = false;
                        this.btnQuery.Enabled = false;
                        this.cmboBoxFrame.Enabled = true;
                        this.btnResubmit.Enabled = true;

                        int iRowCount = dTblDiscOrders.Rows.Count;

                        if (iRowCount == 1)
                        {
                            // Do nothing.
                        }
                        else if (iRowCount > 1)
                        {
                            this.chkBoxAllFrames.Enabled = true;
                            this.chkBoxAllFrames.Visible = true;
                        }                        
                    }
                    else if (dTblDiscOrders.Rows.Count == 0)
                    {
                        MessageBox.Show("No record for " + sText + ".");
                        this.Clear();
                    }
                }
                else if (bDigitOnly != true)
                {
                    MessageBox.Show("Numeric characters only.");
                    this.Clear();
                }
            }
            else if (txtResubmit.Text == string.Empty)
            {
                MessageBox.Show("Please enter a prod # or ref # to continue.");
                this.Clear();
            }
        }

        private void btnResubmit_Click(object sender, EventArgs e)
        {
            if (chkBoxAllFrames.CheckState == CheckState.Checked)
            {
                string sResubmitNumber = sText;

                foreach (DataRow dRowDiscOrders in dTblDiscOrders.Rows)
                {
                    int iResubmitCount = Convert.ToInt32(dRowDiscOrders["ResubmitCount"]);
                    iResubmitCount += 1;

                    string sResubmitFrame = Convert.ToString(dRowDiscOrders["FrameNum"]).Trim();
                    string sDiscType = Convert.ToString(dRowDiscOrders["DiscType"]).Trim();

                    this.DoTheStuff(sResubmitNumber, sResubmitFrame, iResubmitCount, sDiscType);
                }
            }
            else if (chkBoxAllFrames.CheckState == CheckState.Unchecked)
            {
                string sResubmitNumber = sText;
                string sResubmitFrame = cmboBoxFrame.SelectedValue.ToString();

                int iResubmitCount = Convert.ToInt32(dTblDiscOrders.Rows[0]["ResubmitCount"]);
                iResubmitCount += 1;
                string sDiscType = Convert.ToString(dTblDiscOrders.Rows[0]["DiscType"]).Trim();

                this.DoTheStuff(sResubmitNumber, sResubmitFrame, iResubmitCount, sDiscType);
            }

            this.Clear();
        }

        private void DoTheStuff(string sResubmitNumber, string sResubmitFrame, int iResubmitCount, string sDiscType)
        {
            try
            {
                DataTable dTblFrameData = new DataTable("dTblFrameData");
                string sCommText = "SELECT * FROM [FrameData] WHERE ([ProdNum] = '" + sResubmitNumber + "' OR [RefNum] = '" + sResubmitNumber + "') AND [FrameNum] = '" + sResubmitFrame + "'";

                TMethods.SQLQuery(sDiscProcessorConnString, sCommText, dTblFrameData);

                if (dTblFrameData.Rows.Count > 0)
                {
                    sCommText = "DELETE FROM [FrameData] WHERE ([ProdNum] = '" + sResubmitNumber + "' OR [RefNum] = '" + sResubmitNumber + "') AND [FrameNum] = '" + sResubmitFrame + "'";
                    bool bSuccess1 = false;

                    TMethods.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess1);

                    if (bSuccess1 == true)
                    {
                        bool bDeleted = false;
                        TMethods.DeleteRenderedDirectoryAndFiles(sResubmitNumber, sResubmitFrame, ref bDeleted, sDiscType);

                        if (bDeleted == true)
                        {
                            sCommText = "UPDATE [DiscOrders] SET [Status] = '10', [LastCheck] = '" + DateTime.Now.ToString("MM/dd/yy H:mm:ss").Trim() + "', [ResubmitCount] = '" + iResubmitCount + "' WHERE ([ProdNum] = '" + sResubmitNumber + "' OR [RefNum] = '" + sResubmitNumber + "') AND FrameNum = '" + sResubmitFrame + "'";
                            bool bSuccess2 = false;

                            TMethods.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess2);

                            if (bSuccess2 == true)
                            {
                                MessageBox.Show("Successfully resubmitted ProdNum (or RefNum): " + sResubmitNumber + " and FrameNum: " + sResubmitFrame + ".");
                            }
                            else if (bSuccess2 != true)
                            {
                                string sBody = "A resubmit was submitted but the updating of the DiscOrders table failed for ProdNum (or RefNum): " + sResubmitNumber + " FrameNum: " + sResubmitFrame + ".";

                                TMethods.EmailAndStatusUpdatesForResubmits(sResubmitNumber, sResubmitFrame, sBody);
                            }
                        }
                        else if (bDeleted != true)
                        {
                            string sBody = "A resubmit was submitted but the rendered directory could not be deleted for ProdNum (or RefNum): " + sResubmitNumber + " FrameNum: " + sResubmitFrame + ".";

                            TMethods.EmailAndStatusUpdatesForResubmits(sResubmitNumber, sResubmitFrame, sBody);
                        }
                    }
                    else if (bSuccess1 != true)
                    {
                        string sBody = "A resubmit was submitted but the deleting of the FrameData records failed for ProdNum (or RefNum): " + sResubmitNumber + " FrameNum: " + sResubmitFrame + ".";

                        TMethods.EmailAndStatusUpdatesForResubmits(sResubmitNumber, sResubmitFrame, sBody);
                    }
                }
                else if (dTblFrameData.Rows.Count == 0)
                {
                    MessageBox.Show("Records for job #: " + sResubmitNumber + " and frame #: " + sResubmitFrame + " have not been processed yet.");
                }
            }
            catch (Exception ex)
            {
                TMethods.SaveExceptionToDB(ex);
            }
        }

        private void chkBoxAllFrames_CheckedChanged(object sender, EventArgs e)
        {
            if (chkBoxAllFrames.CheckState == CheckState.Checked)
            {
                this.cmboBoxFrame.Enabled = false;
            }
            else if (chkBoxAllFrames.CheckState == CheckState.Unchecked)
            {
                this.cmboBoxFrame.Enabled = true;
            }
        }

        #endregion

        private void Clear()
        {
            try
            {
                this.txtResubmit.Text = string.Empty;                
                this.chkBoxAllFrames.CheckState = CheckState.Unchecked;
                this.cmboBoxFrame.SelectedIndex = -1;
                this.cmboBoxFrame.Enabled = true;
                sText = string.Empty;
                this.btnQuery.Enabled = true;
                this.txtResubmit.Enabled = true;
                this.txtResubmit.Focus();
                this.cmboBoxFrame.Enabled = false;
                this.btnResubmit.Enabled = false;
                this.chkBoxAllFrames.Enabled = false;
                this.chkBoxAllFrames.Visible = false;
            }
            catch (Exception ex)
            {
                TMethods.SaveExceptionToDB(ex);
            }
        }
    }
}
