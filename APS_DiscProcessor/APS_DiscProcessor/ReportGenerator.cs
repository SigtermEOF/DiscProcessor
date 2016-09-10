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
    public partial class ReportGenerator : Form
    {
        string sDiscProcessorConnString = APS_DiscProcessor.Properties.Settings.Default.DiscProcessorConnString.ToString();
        string sCDSConnString = APS_DiscProcessor.Properties.Settings.Default.CDSConnString.ToString();
        TaskMethods TM04 = null;
        bool bDiscOrders = false;
        bool bFrameData = false;
        string sQueryString = string.Empty;

        public ReportGenerator()
        {
            InitializeComponent();

            TM04 = new TaskMethods();
        }

        #region Form events. 

        private void ReportGenerator_Load(object sender, EventArgs e)
        {
            this.cmboBoxTables.SelectedIndex = 0;

            if (bDiscOrders == true)
            {

            }
            else if (bFrameData == true)
            {

            }
        }

        private void cmboBoxTables_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmboBoxTables.SelectedIndex == 0)
            {
                bFrameData = false;
                bDiscOrders = true;

                this.txtBoxQueryString.Text = "SELECT ProdNum, RefNum, FrameNum, Sitting, DiscType, CustNum, Packagetag, Received, OrderType," +
                    " ServiceType, ImageLocation, Quantity, Status, LastCheck, RecordCollectedDate, ResubmitCount FROM DiscOrders";

                sQueryString = "SELECT ProdNum, RefNum, FrameNum, Sitting, DiscType, UniqueID, CustNum, Packagetag, Received, OrderType," +
                    " ServiceType, ImageLocation, Quantity, Status, LastCheck, RecordCollectedDate, ResubmitCount FROM DiscOrders";
            }
            else if (cmboBoxTables.SelectedIndex == 1)
            {
                this.txtBoxQueryString.Text = "SELECT ProdNum, RefNum, FrameNum, Sitting, UniqueID, DiscType, ImageName, PkgTag, MultiRenderGS, DP2Bord," + 
                    " DP2BkGrnd, DP2Color, DP2Text, DP2Mask, GSBackground, NameOn, YearOn, ExportDefFile, JobIDsNeeded, Processed, ProcessDate, ExportDefGenerated," + 
                    " ExportDefGeneratedDate, MergeFileData, NWPFileData, Error, ErrorDate, ErrorChecked, ErrorDescription FROM FrameData";

                sQueryString = "SELECT ProdNum, RefNum, FrameNum, Sitting, UniqueID, DiscType, ImageName, PkgTag, MultiRenderGS, DP2Bord," +
                    " DP2BkGrnd, DP2Color, DP2Text, DP2Mask, GSBackground, NameOn, YearOn, ExportDefFile, JobIDsNeeded, Processed, ProcessDate, ExportDefGenerated," +
                    " ExportDefGeneratedDate, MergeFileData, NWPFileData, Error, ErrorDate, ErrorChecked, ErrorDescription FROM FrameData";

                bDiscOrders = false;
                bFrameData = true;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                string sText = this.txtBoxQueryString.Text.ToString().Trim();

                if (sText != string.Empty)
                {
                    if (bDiscOrders == true)
                    {
                        DataTable dTblDiscOrders = new DataTable("dTblDiscOrders");
                        string sCommText = sText;

                        TM04.SQLQuery(sDiscProcessorConnString, sCommText, dTblDiscOrders);

                        if (dTblDiscOrders.Rows.Count > 0)
                        {
                            this.lblRecsReturned.Text = "Records returned: " + Convert.ToString(dTblDiscOrders.Rows.Count).Trim();

                            string sReportPath = @"R:\jl projects\APS_DiscProcessor\APS_DiscProcessor\APS_DiscProcessor\Reports\DiscOrders.rdlc";
                            string sReportDS = "DiscOrdersReportingDS";

                            this.SetUpRePViewer(sReportPath, sReportDS, dTblDiscOrders, sText);
                        }
                        else if (dTblDiscOrders.Rows.Count == 0)
                        {

                        }
                    }
                    else if (bFrameData == true)
                    {
                        DataTable dTblFrameData = new DataTable("dTblFrameData");
                        string sCommText = sText;

                        TM04.SQLQuery(sDiscProcessorConnString, sCommText, dTblFrameData);

                        if (dTblFrameData.Rows.Count > 0)
                        {
                            this.lblRecsReturned.Text = "Records returned: " + Convert.ToString(dTblFrameData.Rows.Count).Trim();

                            string sReportPath = @"R:\jl projects\APS_DiscProcessor\APS_DiscProcessor\APS_DiscProcessor\Reports\FrameData.rdlc";
                            string sReportDS = "FrameDataDS";

                            this.SetUpRePViewer(sReportPath, sReportDS, dTblFrameData, sText);
                        }
                        else if (dTblFrameData.Rows.Count == 0)
                        {

                        }
                    }
                }
                else if (sText == string.Empty)
                {

                }
            }
            catch (Exception ex)
            {
                TM04.SaveExceptionToDB(ex);
            }
        }

        #endregion

        private void SetUpRePViewer(string sReportPath, string sReportDS, DataTable dTbl, string sText)
        {
            try
            {
                this.repViewerDiscOrders.LocalReport.ReportPath = @sReportPath;

                this.repViewerDiscOrders.LocalReport.DataSources.Clear();
                this.repViewerDiscOrders.LocalReport.DataSources.Add(new Microsoft.Reporting.WinForms.ReportDataSource(sReportDS, dTbl));

                this.repViewerDiscOrders.RefreshReport();

                this.txtBoxPrevQuery.Text = sText;
                this.txtBoxQueryString.Text = sQueryString;
                this.txtBoxQueryString.Focus();

                Application.DoEvents();
            }
            catch (Exception ex)
            {
                TM04.SaveExceptionToDB(ex);
                MessageBox.Show("An error has occurred populating the report with data. Check the Errors table for more information.");
            }
        }
    }
}
