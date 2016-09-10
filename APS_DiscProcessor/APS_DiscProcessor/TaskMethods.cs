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
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text.RegularExpressions;

namespace APS_DiscProcessor
{
    class TaskMethods
    {
        string sDiscProcessorConnString = APS_DiscProcessor.Properties.Settings.Default.DiscProcessorConnString.ToString();
        string sCDSConnString = APS_DiscProcessor.Properties.Settings.Default.CDSConnString.ToString();
        string sKrsolConnString = APS_DiscProcessor.Properties.Settings.Default.ksrsolConnString.ToString();

        public bool SQLNonQuery(string sConnString, string sCommText, ref bool bSuccess)
        {
            try
            {
                SqlConnection sqlConn = new SqlConnection(sConnString);

                SqlCommand sqlComm = sqlConn.CreateCommand();

                sqlComm.CommandText = sCommText;

                sqlConn.Open();

                sqlComm.ExecuteNonQuery();

                sqlComm.Dispose();

                sqlConn.Close();
                sqlConn.Dispose();

                bSuccess = true;
            }
            catch(Exception ex)
            {
                bSuccess = false;
                this.SaveExceptionToDBwithCommText(ex, sCommText);
            }
            return bSuccess;
        }

        public void SQLQuery(string sConnString, string sCommText, DataTable dTbl)
        {
            try
            {
                SqlConnection sqlConn = new SqlConnection(sConnString);

                SqlCommand sqlComm = sqlConn.CreateCommand();

                sqlComm.CommandText = sCommText;

                sqlConn.Open();

                SqlDataReader sqlDReader = sqlComm.ExecuteReader();

                if (sqlDReader.HasRows)
                {
                    dTbl.Clear();
                    dTbl.Load(sqlDReader);
                }

                sqlDReader.Close();
                sqlDReader.Dispose();

                sqlComm.Dispose();

                sqlConn.Close();
                sqlConn.Dispose();
            }
            catch(Exception ex)
            {
                this.SaveExceptionToDBwithCommText(ex, sCommText);
            }
        }

        public void CDSQuery(string sConnString, string sCommText, DataTable dTbl)
        {
            try
            {
                OleDbConnection olDBConn = new OleDbConnection(sConnString);

                OleDbCommand oleDBComm = olDBConn.CreateCommand();

                oleDBComm.CommandText = sCommText;

                olDBConn.Open();

                oleDBComm.CommandTimeout = 0;

                OleDbDataReader oleDBDReader = oleDBComm.ExecuteReader();

                if (oleDBDReader.HasRows)
                {
                    dTbl.Clear();
                    dTbl.Load(oleDBDReader);
                }

                oleDBComm.Dispose();

                oleDBDReader.Close();
                oleDBDReader.Dispose();

                olDBConn.Close();
                olDBConn.Dispose();
            }
            catch(Exception ex)
            {
                this.SaveExceptionToDBwithCommText(ex, sCommText);
            }
        }

        public bool CDSNonQuery(string sConnString, string sCommText, ref bool bSuccess)
        {
            try
            {
                OleDbConnection oleDBConn = new OleDbConnection(sConnString);

                OleDbCommand oleDBComm = oleDBConn.CreateCommand();

                oleDBComm.CommandText = sCommText;

                oleDBConn.Open();

                oleDBComm.CommandTimeout = 0;

                oleDBComm.ExecuteNonQuery();

                oleDBComm.Dispose();

                oleDBConn.Close();
                oleDBConn.Dispose();

                bSuccess = true;
            }
            catch (Exception ex)
            {
                bSuccess = false;
                this.SaveExceptionToDBwithCommText(ex, sCommText);
            }
            return bSuccess;
        }

        public void SaveExceptionToDB(Exception ex)
        {
            string sException = ex.ToString().Trim();
            sException = sException.Replace(@"'", "");
            string sCommText = string.Empty;

            try
            {
                sCommText = "INSERT INTO [Errors] VALUES ('" + sException + "', '" + DateTime.Now.ToString() + "', '0')";
                bool bSuccess = false;

                this.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                if (bSuccess == true)
                {

                }
                else if (bSuccess != true)
                {
                    MessageBox.Show("Insertion of data into the Errors table failed." + Environment.NewLine + Environment.NewLine + sCommText);
                }
            }
            catch(Exception exception)
            {
                MessageBox.Show(exception.Message + Environment.NewLine + Environment.NewLine + sCommText);
            }
        }

        public void SaveExceptionToDBwithCommText(Exception ex, string sText)
        {
            string sException = ex.ToString().Trim();
            sException = sException.Replace(@"'", "");
            sText = sText.Replace(@"'", "");
            string sCommText = string.Empty;

            try
            {
                sCommText = "INSERT INTO [Errors] VALUES ('" + sException + Environment.NewLine + sText + "', '" + DateTime.Now.ToString() + "', '0')";
                bool bSuccess = false;

                this.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                if (bSuccess == true)
                {

                }
                else if (bSuccess != true)
                {
                    MessageBox.Show("Insertion of data into the Errors table failed." + Environment.NewLine + Environment.NewLine + sCommText);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message + Environment.NewLine + Environment.NewLine + sCommText);
            }
        }

        public void CheckForExportFileExistence(ref string sExportDef, ref bool bHaveFile)
        {
            try
            {
                string sExportDefWithoutExtension = Path.GetFileNameWithoutExtension(sExportDef);

                string sLabel = "ExportDefPath";
                string sValue = "Value";
                string sVariable = string.Empty;
                bool bSuccess = true;

                this.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                if (bSuccess == true)
                {
                    string[] sFiles = Directory.GetFiles(sVariable);

                    foreach (string s in sFiles)
                    {
                        string sFile = Path.GetFileNameWithoutExtension(s);
                        string sExtension = Path.GetExtension(s);

                        if (sExportDefWithoutExtension == sFile)
                        {
                            bHaveFile = true;
                            sExportDef = sExportDefWithoutExtension += sExtension;
                            return;
                        }
                    }
                }
                else if (bSuccess != true)
                {

                }
            }
            catch(Exception ex)
            {
                this.SaveExceptionToDB(ex);
                bHaveFile = false;
            }
        }

        public void StylesAndBackgroundCount(ref int iIDsNeeded, bool bMultiRenderGS, DataTable dTbl01, DataTable dTbl02, string sDiscType)
        {
            // if bMultiRenderGS = true
            // 3 renders per style (8x10 w/ name, 8x10 w/out name and a 5x7)
            // 14 gs bgs
            // 2 renders per gs bg (color 8x10 and a b&w 8x10)
            // 1 rendered copyright
            // (14 styles * 3) + (14 gs bgs * 2) + 1 = 71

            // if bMultiRenderGS = false
            // 14 styles
            // 3 renders per style (8x10 w/ name, 8x10 w/out name and a 5x7)
            // 3 renders per disc (color 8x10, b&w 8x10 and a copyright)
            // (14 styles * 3) + 3 = 45

            try
            {
                int iRowsCount01 = dTbl01.Rows.Count;
                int iRowsCount02 = dTbl02.Rows.Count;

                if (sDiscType == "ICD" || sDiscType == "ICDW")
                {
                    if (bMultiRenderGS == false)
                    {
                        if (iRowsCount01 != 0 && iRowsCount02 != 0)
                        {
                            iIDsNeeded = ((iRowsCount01 * iRowsCount02) * 3) + 3;
                        }
                        else if (iRowsCount01 == 0 && iRowsCount02 != 0)
                        {
                            iIDsNeeded = (iRowsCount02 * 3) + 3;
                        }
                        else if (iRowsCount01 != 0 && iRowsCount02 == 0)
                        {
                            iIDsNeeded = (iRowsCount01 * 3) + 3;
                        }

                    }
                    else if (bMultiRenderGS != false)
                    {
                        DataTable dTblAltData = new DataTable("dTblAltData");
                        dTblAltData.Columns.Add("Alt_Data", typeof(string));

                        string sPreviousAltData = string.Empty;

                        foreach (DataRow dRow01 in dTbl01.Rows)
                        {
                            string sAltData = Convert.ToString(dRow01["Alt_data"]).Trim();

                            if (sAltData != string.Empty || sAltData != "")
                            {
                                if (sAltData != sPreviousAltData) // This is to prevent the same green screen background getting a color/b&w 8x10 render.
                                {
                                    sPreviousAltData = sAltData;

                                    dTblAltData.Rows.Add(sAltData);
                                }
                            }
                        }

                        int iRowsCount03 = dTblAltData.Rows.Count;

                        iIDsNeeded = (iRowsCount01 * 3) + (iRowsCount03 * 2) + 1;
                    }
                }
                else if (sDiscType == "MEG" || sDiscType == "MEGW")
                {
                    //Meg discs need:
                        //(Styles * 3 renders) + (20 additional renders + 12 renders for calendar + 1 copyright rendered).
                    if (iRowsCount01 != 0 && iRowsCount02 != 0)
                    {
                        iIDsNeeded = ((iRowsCount01 * iRowsCount02) * 3) + (20 + 12 + 1);
                    }
                    else if (iRowsCount01 != 0 && iRowsCount02 == 0)
                    {
                        iIDsNeeded = (iRowsCount01 * 3) + (20 + 12 + 1);
                    }
                    else if (iRowsCount01 == 0 && iRowsCount02 != 0)
                    {
                        iIDsNeeded = (iRowsCount02 * 3) + (20 + 12 + 1);
                    }
                }
            }
            catch (Exception ex)
            {
                this.SaveExceptionToDB(ex);
            }
        }

        public void GetCrop(ref string sCrop, string sUniqueID, string sProdNum, string sFrameNum, string sRefNum)
        {
            try
            {
                string sDP2Bord = string.Empty;
                string sZone = string.Empty;
                sFrameNum = sFrameNum.TrimStart('0');

                DataTable dTbl = new DataTable();
                string sCommText = "SELECT [DP2Bord] FROM [FrameData] WHERE [UniqueID] = '" + sUniqueID + "'";

                this.SQLQuery(sDiscProcessorConnString, sCommText, dTbl);

                if (dTbl.Rows.Count > 0)
                {
                    sDP2Bord = Convert.ToString(dTbl.Rows[0]["DP2Bord"]).Trim();

                    // SELECT * FROM cds.dp2crop WHERE DP2Bord = sDP2Bord
                    // if no returned results then sCrop = "50 50 50 50 100 100"

                    // if sCrop = string.empty
                    // select * from cds.dp2image where lookupnum = sProdNum and frame = sFrameNum
                    // if results then sZone = dp2image.zone
                    // sCrop = "50 50 50 50 100 100"

                    // if sZone = string.empty
                    // select * from cds.items where lookupnum = sProdNum
                    // if results then sZone = items.special

                    // if sZone != string.empty
                    // select * from cds.dp2crop where dp2crop.DP2Bord = sDP2Bord and dp2crop.zone = sZone
                    // if results then sCrop = dp2crop.cropovr

                    DataTable dTbl1 = new DataTable();
                    sCommText = "SELECT * FROM DP2Crop WHERE DP2Bord = '" + sDP2Bord + "'";

                    this.CDSQuery(sCDSConnString, sCommText, dTbl1);

                    if (dTbl1.Rows.Count > 0)
                    {
                        if (sZone == string.Empty)
                        {
                            DataTable dTbl2 = new DataTable();
                            sCommText = "SELECT * FROM DP2Image WHERE Lookupnum = '" + sProdNum + "' AND Frame = " + sFrameNum + "";

                            this.CDSQuery(sCDSConnString, sCommText, dTbl2);

                            if (dTbl2.Rows.Count > 0)
                            {
                                sZone = Convert.ToString(dTbl2.Rows[0]["Zone"]).Trim();

                                if (sZone == string.Empty)
                                {
                                    DataTable dTbl3 = new DataTable();
                                    sCommText = "SELECT * FROM Items WHERE Lookupnum = '" + sProdNum + "'";

                                    this.CDSQuery(sCDSConnString, sCommText, dTbl3);

                                    if (dTbl3.Rows.Count > 0)
                                    {
                                        sZone = Convert.ToString(dTbl3.Rows[0]["Special"]).Trim();
                                    }
                                    else if (dTbl3.Rows.Count == 0)
                                    {
                                        string sStop = string.Empty;
                                    }
                                }
                                else if (sZone != string.Empty || sZone != "")
                                {

                                }
                            }
                            else if (dTbl2.Rows.Count == 0)
                            {
                                sCrop = "50 50 50 50 100 100";
                            }
                        }
                        else if (sZone != string.Empty || sZone != "")
                        {

                        }
                    }
                    else if (dTbl1.Rows.Count == 0)
                    {
                        sCrop = "50 50 50 50 100 100";
                    }
                }
                else if (dTbl.Rows.Count == 0)
                {
                    string sStop = string.Empty;
                }

                if (sZone != string.Empty || sZone != "")
                {
                    DataTable dTbl4 = new DataTable();
                    sCommText = "SELECT Cropovr FROM DP2Crop WHERE DP2Bord = '" + sDP2Bord + "' AND Zone = '" + sZone + "'";

                    this.CDSQuery(sCDSConnString, sCommText, dTbl4);

                    if (dTbl4.Rows.Count > 0)
                    {
                        sCrop = Convert.ToString(dTbl4.Rows[0]["Cropovr"]).Trim();
                    }
                    else if (dTbl4.Rows.Count == 0)
                    {

                    }
                }
                else if (sZone == string.Empty)
                {
                    sCrop = "50 50 50 50 100 100";
                }

            }
            catch(Exception ex)
            {
                this.SaveExceptionToDB(ex);
            }
        }

        public void RemoveOrphanedExportDefFiles(DataTable dTblJob)
        {
            try
            {
                if (dTblJob.Rows.Count > 0)
                {
                    foreach (DataRow dRow in dTblJob.Rows)
                    {
                        string sFileToRemove = Convert.ToString(dRow["SavedExportDefPath"]).Trim();

                        if (File.Exists(sFileToRemove))
                        {
                            File.Delete(sFileToRemove);
                        }
                    }
                }
                else if (dTblJob.Rows.Count == 0)
                {
                    string sStop = string.Empty;
                }
            }
            catch(Exception ex)
            {
                this.SaveExceptionToDB(ex);
            }
        }

        public void UpdateDiscOrdersTableStatusFrameBased(string sRefNum, string sFrameNum, string sStatus, string sDiscType)
        {
            try
            {
                DataTable dTblGetProdNum = new DataTable("dTblGetProdNum");
                string sCommText = "SELECT [ProdNum] FROM [DiscOrders] WHERE [RefNum] = '" + sRefNum + "'";

                this.SQLQuery(sDiscProcessorConnString, sCommText, dTblGetProdNum);

                if (dTblGetProdNum.Rows.Count > 0)
                {
                    string sProdNum = Convert.ToString(dTblGetProdNum.Rows[0]["ProdNum"]).Trim();

                    sCommText = "UPDATE [DiscOrders] SET [Status] = '" + sStatus + "', [LastCheck] = '" + DateTime.Now.ToString("MM/dd/yy H:mm:ss").Trim()
                        + "' WHERE [UniqueID] = '" + sProdNum + sFrameNum + sDiscType + "'";

                    bool bSuccess = false;

                    this.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                    if (bSuccess == true)
                    {

                    }
                    else if (bSuccess != true)
                    {
                        MessageBox.Show("SQL INSERT/UPDATE Failed : " + Environment.NewLine + sCommText);
                    }
                }
                else if (dTblGetProdNum.Rows.Count == 0)
                {

                }
            }
            catch(Exception ex)
            {
                this.SaveExceptionToDB(ex);
            }
        }

        public void UpdateDiscOrdersTableStatusSittingBased(string sRefNum, string sStatus, string sDiscType)
        {
            try
            {
                string sCommText = "UPDATE [DiscOrders] SET [Status] = '" + sStatus + "', [LastCheck] = '" + DateTime.Now.ToString("MM/dd/yy H:mm:ss").Trim()
                    + "' WHERE [RefNum] = '" + sRefNum + "' AND [DiscType] = '" + sDiscType + "'";

                bool bSuccess = false;

                this.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                if (bSuccess == true)
                {

                }
                else if (bSuccess != true)
                {
                    MessageBox.Show("SQL INSERT/UPDATE Failed : " + Environment.NewLine + sCommText);
                }
            }
            catch (Exception ex)
            {
                this.SaveExceptionToDB(ex);
            }
        }

        public void UpdateDiscOrdersForErrors(string sErrorDescription, string sRefNum, string sFrameNum, string sDiscType, string sSitting, bool bSittingBased)
        {
            try
            {
                bool bSuccess = false;
                string sCommText = string.Empty;

                if (bSittingBased != true)
                {
                    sCommText = "UPDATE [DiscOrders] SET [Error] = '1', [ErrorDescription] = '" + sErrorDescription + "', [ErrorDate] = '"
                        + DateTime.Now.ToString().Trim() + "' WHERE [RefNum] = '" + sRefNum + "' AND [FrameNum] = '" + sFrameNum + "' AND [DiscType] = '" + sDiscType + "'";
                }
                else if (bSittingBased == true)
                {
                    sCommText = "UPDATE [DiscOrders] SET [Error] = '1', [ErrorDescription] = '" + sErrorDescription + "', [ErrorDate] = '"
                        + DateTime.Now.ToString().Trim() + "' WHERE [RefNum] = '" + sRefNum + "' AND [Sitting] = '" + sSitting + "' AND [DiscType] = '" + sDiscType + "'";
                }

                this.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                if (bSuccess == true)
                {

                }
                else if (bSuccess != true)
                {
                    MessageBox.Show("SQL INSERT/UPDATE Failed : " + Environment.NewLine + sCommText);
                }
            }
            catch(Exception ex)
            {
                this.SaveExceptionToDB(ex);
            }
        }

        public void CheckForErrors()
        {
            try
            {
                Trace.WriteLine("Checking for errors in the DiscProcessor.DiscOrders table.");

                StringBuilder sBuilder = new StringBuilder();

                DataTable dTblDiscOrders = new DataTable("DiscOrders");
                string sCommText = "SELECT * FROM [DiscOrders] WHERE [Status] = '90'";

                this.SQLQuery(sDiscProcessorConnString, sCommText, dTblDiscOrders);

                if (dTblDiscOrders.Rows.Count > 0)
                {
                    foreach (DataRow dRowDiscOrders in dTblDiscOrders.Rows)
                    {
                        sBuilder.Clear();

                        string sProdNum = Convert.ToString(dRowDiscOrders["ProdNum"]).Trim();
                        string sFrameNum = Convert.ToString(dRowDiscOrders["FrameNum"]).Trim();
                        string sRefNum = Convert.ToString(dRowDiscOrders["RefNum"]).Trim();
                        string sDiscType = Convert.ToString(dRowDiscOrders["DiscType"]).Trim();
                        string sError = Convert.ToString(dRowDiscOrders["Error"]).Trim();
                        string sErrorDescription = Convert.ToString(dRowDiscOrders["ErrorDescription"]).Trim();
                        string sUniqueID = Convert.ToString(dRowDiscOrders["UniqueID"]).Trim();

                        sBuilder.AppendFormat("Production #: " + sProdNum);
                        sBuilder.Append(Environment.NewLine);
                        sBuilder.AppendFormat("Reference #: " + sRefNum);
                        sBuilder.Append(Environment.NewLine);
                        sBuilder.AppendFormat("Frame #: " + sFrameNum);
                        sBuilder.Append(Environment.NewLine);
                        sBuilder.AppendFormat("UniqueID : " + sUniqueID);
                        sBuilder.Append(Environment.NewLine);
                        sBuilder.AppendFormat("Error : " + sError);
                        sBuilder.Append(Environment.NewLine);
                        sBuilder.AppendFormat("Error description : " + sErrorDescription);
                        sBuilder.Append(Environment.NewLine);

                        string sSubject = "DiscProcessor error notification.";
                        string sBody = "An error was recorded in the DiscProcessor.DiscOrders table: " + Environment.NewLine + Environment.NewLine + sBuilder.ToString().Trim();

                        string sEmailServer = string.Empty;
                        string sCCSendTo = string.Empty;
                        string sSendTo = string.Empty;
                        string sTextMsgSendTo = string.Empty;
                        bool bSuccess = false;

                        this.GatherEmailVariables(ref sEmailServer, ref sCCSendTo, ref sSendTo, ref sTextMsgSendTo, ref bSuccess);

                        if (bSuccess == true)
                        {
                            this.EmailError(sEmailServer, sCCSendTo, sSendTo, sSubject, sBody, sTextMsgSendTo);

                            sCommText = "UPDATE [DiscOrders] SET [ErrorChecked] = '1' WHERE [ProdNum] = '" + sProdNum + "' AND [FrameNum] = '" + sFrameNum + "' AND [DiscType] = '" + sDiscType + "'";
                            bool bSuccess1 = false;

                            this.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess1);

                            if (bSuccess1 == true)
                            {
                                string sStatus = "91";
                                this.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                            }
                            else if (bSuccess1 != true)
                            {
                                MessageBox.Show("SQL INSERT/UPDATE Failed : " + Environment.NewLine + sCommText);
                            }
                        }
                        else if (bSuccess != true)
                        {
                            sEmailServer = "email";
                            sCCSendTo = "thegrump1976@gmail.com";
                            sSendTo = "jlett@company.mail";
                            sTextMsgSendTo = "4233815188@tmomail.net";

                            this.EmailError(sEmailServer, sCCSendTo, sSendTo, sSubject, sBody, sTextMsgSendTo);

                            sCommText = "UPDATE [DiscOrders] SET [ErrorChecked] = '1' WHERE [ProdNum] = '" + sProdNum + "' AND [FrameNum] = '" + sFrameNum + "' AND [DiscType] = '" + sDiscType + "'";
                            bool bSuccess1 = false;

                            this.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess1);

                            if (bSuccess1 == true)
                            {
                                string sStatus = "91";
                                this.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                            }
                            else if (bSuccess1 != true)
                            {
                                MessageBox.Show("SQL INSERT/UPDATE Failed : " + Environment.NewLine + sCommText);
                            }
                        }
                    }
                }
                else if (dTblDiscOrders.Rows.Count == 0)
                {

                }

                Trace.WriteLine("Checking for errors in the DiscProcessor.Errors table.");

                DataTable dTblErrorsTable = new DataTable("ErrorsTable");
                sCommText = "SELECT * FROM [Errors] WHERE [Exception_Email_Sent] = '0'";

                this.SQLQuery(sDiscProcessorConnString, sCommText, dTblErrorsTable);

                if (dTblErrorsTable.Rows.Count > 0)
                {
                    foreach (DataRow dRowErrorsTable in dTblErrorsTable.Rows)
                    {
                        string sException = Convert.ToString(dRowErrorsTable["Exception"]).Trim();
                        string sExceptionDateTime = Convert.ToString(dRowErrorsTable["Exception_DateTime"]).Trim();

                        string sEmailServer = string.Empty;
                        string sCCSendTo = string.Empty;
                        string sSendTo = string.Empty;
                        string sTextMsgSendTo = string.Empty;
                        bool bSuccess = false;

                        this.GatherEmailVariables(ref sEmailServer, ref sCCSendTo, ref sSendTo, ref sTextMsgSendTo, ref bSuccess);

                        if (bSuccess == true)
                        {
                            string sSubject = "DiscProcessor error notification.";

                            string sBody = "An exception was recorded in the DiscProcessor.Errors table at " + sExceptionDateTime + " : " + Environment.NewLine + Environment.NewLine + sException;

                            this.EmailError(sEmailServer, sCCSendTo, sSendTo, sSubject, sBody, sTextMsgSendTo);

                            sCommText = "UPDATE [Errors] SET [Exception_Email_Sent] = '1' WHERE [Exception] = '" + sException + "' AND [Exception_DateTime] = '" + sExceptionDateTime + "'";
                            bool bSuccess1 = false;

                            this.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess1);

                            if (bSuccess1 == true)
                            {

                            }
                            else if (bSuccess1 != true)
                            {
                                MessageBox.Show("SQL INSERT/UPDATE Failed : " + Environment.NewLine + sCommText);
                            }
                        }
                        else if (bSuccess != true)
                        {

                        }
                    }
                }
                else if (dTblErrorsTable.Rows.Count == 0)
                {

                }

            }
            catch(Exception ex)
            {
                this.SaveExceptionToDB(ex);
            }
        }

        public void GatherEmailVariables(ref string sEmailServer, ref string sCCSendTo, ref string sSendTo, ref string sTextMsgSendTo, ref bool bSuccess)
        {
            try
            {
                DataTable dTblVariables = new DataTable("dTblVariables");
                string sCommText = "SELECT * FROM [Variables]";

                this.SQLQuery(sDiscProcessorConnString, sCommText, dTblVariables);

                if (dTblVariables.Rows.Count > 0)
                {
                    IEnumerable<DataRow> Variables =
                        from variables in dTblVariables.AsEnumerable()
                        select variables;

                    IEnumerable<DataRow> EmailServer =
                        Variables.Where(p => p.Field<string>("Label") == "EmailServer");

                    IEnumerable<DataRow> CCSendTo =
                        Variables.Where(p => p.Field<string>("Label") == "EmailCCSendTo");

                    IEnumerable<DataRow> SendTo =
                        Variables.Where(p => p.Field<string>("Label") == "EmailSendTo");

                    IEnumerable<DataRow> TextMsgSendTo =
                        Variables.Where(p => p.Field<string>("Label") == "TextMsgSendTo");

                    foreach (DataRow dRow in EmailServer)
                    {
                        sEmailServer = Convert.ToString(dRow.Field<string>("Value"));
                    }

                    foreach (DataRow dRow in CCSendTo)
                    {
                        sCCSendTo = Convert.ToString(dRow.Field<string>("Value"));
                    }

                    foreach (DataRow dRow in SendTo)
                    {
                        sSendTo = Convert.ToString(dRow.Field<string>("Value"));
                    }

                    foreach (DataRow dRow in TextMsgSendTo)
                    {
                        sTextMsgSendTo = Convert.ToString(dRow.Field<string>("Value"));
                    }

                    bSuccess = true;
                }
                else if (dTblVariables.Rows.Count == 0)
                {
                    bSuccess = false;
                }
            }
            catch(Exception ex)
            {
                this.SaveExceptionToDB(ex);
                bSuccess = false;
            }
        }

        public void EmailError(string sEmailServer, string sCCSendTo, string sSendTo, string sSubject, string sBody, string sTextMsgSendTo)
        {
            try
            {
                DataTable dTblSendNotifications = new DataTable("dTblSendNotifications");
                string sCommText = "SELECT [Value] FROM [Variables] WHERE [Label] = 'SendNotifications'";

                this.SQLQuery(sDiscProcessorConnString, sCommText, dTblSendNotifications);

                if (dTblSendNotifications.Rows.Count > 0)
                {
                    bool bSendNotifications = Convert.ToBoolean(dTblSendNotifications.Rows[0]["Value"]);

                    if (bSendNotifications == true)
                    {
                        MailAddress from = new MailAddress("APSAUTO@ADVANCEDPHOTO.COM", "APS");
                        MailAddress to = new MailAddress(sSendTo);
                        MailMessage mailMessage = new MailMessage(from, to);
                        mailMessage.Subject = sSubject;
                        mailMessage.Body = sBody;
                        MailAddress cc = new MailAddress(sCCSendTo);
                        MailAddress txt = new MailAddress(sTextMsgSendTo);
                        mailMessage.CC.Add(cc);
                        mailMessage.CC.Add(txt);

                        SmtpClient stmpClient = new SmtpClient(sEmailServer);
                        stmpClient.Credentials = CredentialCache.DefaultNetworkCredentials;

                        stmpClient.Send(mailMessage);
                    }
                    else if (bSendNotifications != true)
                    {

                    }
                }
                else if (dTblSendNotifications.Rows.Count == 0)
                {

                }
            }
            catch(Exception ex)
            {
                this.SaveExceptionToDB(ex);
            }
        }

        public void GatherVariables(string sLabel, string sValue, ref string sVariable, ref bool bSuccess)
        {
            //sLabel = Variables.Label
            //sValue = Variables.Value
            //sVariable = string equal to Variables.Value

            try
            {
                DataTable dTblVariables = new DataTable("dTblVariables");
                string sCommText = "SELECT * FROM [Variables]";

                this.SQLQuery(sDiscProcessorConnString, sCommText, dTblVariables);

                if (dTblVariables.Rows.Count > 0)
                {
                    IEnumerable<DataRow> Variables =
                        from variables in dTblVariables.AsEnumerable()
                        select variables;

                    IEnumerable<DataRow> Query =
                        Variables.Where(p => p.Field<string>("Label") == sLabel);

                    foreach (DataRow dRow in Query)
                    {
                        sVariable = Convert.ToString(dRow.Field<string>(sValue));
                    }

                    bSuccess = true;
                }
                else if (dTblVariables.Rows.Count == 0)
                {
                    bSuccess = false;

                    bool bInsertErrorSuccess = true;
                    string sError = "Failed to gather variable data." + Environment.NewLine + "Values:" + Environment.NewLine + "Label: " + sLabel +
                        Environment.NewLine + "Value: " + sValue;

                    sCommText = "INSERT INTO [Errors] VALUES ('" + sError + "','" + DateTime.Now.ToString().Trim() + "','0')";

                    this.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bInsertErrorSuccess);

                    if (bInsertErrorSuccess == true)
                    {

                    }
                    else if (bInsertErrorSuccess != true)
                    {

                    }
                }
            }
            catch(Exception ex)
            {
                this.SaveExceptionToDB(ex);
                bSuccess = false;
            }
        }

        public void UpdateFrameDataForExportDefGenerated(string sRefNum, string sFrameNum, string sDiscType)
        {
            try
            {
                bool bSuccess = true;
                string sCommText = "UPDATE [FrameData] SET [ExportDefGenerated] = '1', [ExportDefGeneratedDate] = '" + DateTime.Now.ToString("MM/dd/yy H:mm:ss").Trim() + "' WHERE [RefNum] = '" + sRefNum + "' AND [FrameNum] = '" + sFrameNum + "' AND [DiscType] = '" + sDiscType + "'";

                this.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                if (bSuccess == true)
                {

                }
                else if (bSuccess != true)
                {
                    MessageBox.Show("SQL INSERT/UPDATE Failed : " + Environment.NewLine + sCommText);
                }
            }
            catch(Exception ex)
            {
                this.SaveExceptionToDB(ex);
            }
        }

        public void GenerateCopyrightRelease(string sRenderedPath, ref bool bCRGenerated)
        {
            string sCopyrightRelease = "Copyright Release.txt";
            string sCRPath = sRenderedPath + sCopyrightRelease;

            try
            {
                int iHaveCopyright = Directory.GetFiles(sRenderedPath, "Copyright Release.txt", SearchOption.TopDirectoryOnly).Length;

                if (iHaveCopyright == 0)
                {
                    DataTable dTbl = new DataTable();
                    string sCommText = "SELECT [Value] FROM [Variables] WHERE [Label] = 'CopyrightRelease'";

                    this.SQLQuery(sDiscProcessorConnString, sCommText, dTbl);

                    if (dTbl.Rows.Count > 0)
                    {
                        string sCopyright = Convert.ToString(dTbl.Rows[0]["Value"]).Trim();

                        File.WriteAllText(sCRPath, sCopyright);

                        bCRGenerated = true;
                    }
                    else if (dTbl.Rows.Count == 0)
                    {
                        bCRGenerated = false;
                    }
                }
                else if (iHaveCopyright != 0)
                {
                    bCRGenerated = true;
                }
            }
            catch(Exception ex)
            {
                this.SaveExceptionToDB(ex);
                bCRGenerated = false;
            }
        }

        public void GatherNWPFileVariables(ref string ReadyDir1, ref string ReadyDir2, ref string ReadyDir3, ref string ReadyDir4, ref string sSendHereFile, ref bool bSuccess)
        {
            try
            {
                DataTable dTblVariables = new DataTable("dTblVariables");
                string sCommText = "SELECT * FROM [Variables]";

                this.SQLQuery(sDiscProcessorConnString, sCommText, dTblVariables);

                if (dTblVariables.Rows.Count > 0)
                {
                    IEnumerable<DataRow> Variables =
                        from variables in dTblVariables.AsEnumerable()
                        select variables;

                    IEnumerable<DataRow> Var1 =
                        Variables.Where(p => p.Field<string>("Label") == "ReadyDir1");

                    IEnumerable<DataRow> Var2 =
                        Variables.Where(p => p.Field<string>("Label") == "ReadyDir2");

                    IEnumerable<DataRow> Var3 =
                        Variables.Where(p => p.Field<string>("Label") == "ReadyDir3");

                    IEnumerable<DataRow> Var4 =
                        Variables.Where(p => p.Field<string>("Label") == "ReadyDir4");

                    IEnumerable<DataRow> Var5 =
                        Variables.Where(p => p.Field<string>("Label") == "SendHereFile");

                    foreach (DataRow dRow in Var1)
                    {
                        ReadyDir1 = Convert.ToString(dRow.Field<string>("Value"));
                    }

                    foreach (DataRow dRow in Var2)
                    {
                        ReadyDir2 = Convert.ToString(dRow.Field<string>("Value"));
                    }

                    foreach (DataRow dRow in Var3)
                    {
                        ReadyDir3 = Convert.ToString(dRow.Field<string>("Value"));
                    }

                    foreach (DataRow dRow in Var4)
                    {
                        ReadyDir4 = Convert.ToString(dRow.Field<string>("Value"));
                    }

                    foreach (DataRow dRow in Var5)
                    {
                        sSendHereFile = Convert.ToString(dRow.Field<string>("Value"));
                    }

                    bSuccess = true;
                }
                else if (dTblVariables.Rows.Count == 0)
                {
                    bSuccess = false;
                }
            }
            catch(Exception ex)
            {
                this.SaveExceptionToDB(ex);
                bSuccess = false;
            }
        }

        public void GetNameOnData(string sProdNum, string sFrameNum, ref string sNameOn)
        {
            try
            {
                sFrameNum = sFrameNum.TrimStart('0');

                DataTable dTbl = new DataTable();
                string sCommText = "SELECT First_name, Wname FROM Endcust WHERE Lookupnum = '" + sProdNum +
                    "' AND Sequence = " + sFrameNum;

                this.CDSQuery(sCDSConnString, sCommText, dTbl);

                if (dTbl.Rows.Count > 0)
                {
                    string sFName = Convert.ToString(dTbl.Rows[0]["First_name"]).Trim();
                    sNameOn = Convert.ToString(dTbl.Rows[0]["Wname"]).Trim();

                    if (sNameOn == string.Empty) // If WName = empty then use First_name value. 
                    {
                        sNameOn = sFName;
                    }

                    sNameOn = sNameOn.Replace("'", "''");
                }
                else if (dTbl.Rows.Count == 0)
                {
                    string sStop = string.Empty;

                    DataTable dTblSport = new DataTable("Sport");
                    sCommText = "SELECT First_name FROM Sport WHERE Lookupnum = '" + sProdNum + "' AND Sequence = " + sFrameNum;

                    this.CDSQuery(sCDSConnString, sCommText, dTblSport);

                    if (dTblSport.Rows.Count > 0)
                    {
                        string sFName = Convert.ToString(dTblSport.Rows[0]["First_name"]).Trim();

                        sNameOn = sFName;

                        sNameOn = sNameOn.Replace("'", "''");
                    }
                    else if (dTblSport.Rows.Count == 0)
                    {
                        string sStopped = string.Empty;
                    }
                }

                if (sNameOn != string.Empty)
                {
                    sNameOn = Regex.Replace(sNameOn, @"[\d-]", string.Empty); //Remove any numeric characters from NameOn (prevent the doubling up of year on)
                }

            }
            catch (Exception ex)
            {
                this.SaveExceptionToDB(ex);
            }
        }

        public void GetYear(string sPkgTag, ref string sYearOn)
        {
            try
            {
                DataTable dTbl = new DataTable();
                string sCommText = "SELECT Year FROM Pkgdetails WHERE Packagetag = '" + sPkgTag + "'";

                this.CDSQuery(sCDSConnString, sCommText, dTbl);

                if (dTbl.Rows.Count > 0)
                {
                    sYearOn = Convert.ToString(dTbl.Rows[0]["year"]).Trim();
                }
                else if (dTbl.Rows.Count == 0)
                {
                    string sStop = string.Empty;
                }
            }
            catch (Exception ex)
            {
                this.SaveExceptionToDB(ex);
            }
        }

        public bool VerifyGS(string sGSBG, ref bool bGSFound)
        {
            try
            {
                DataTable dTbl = new DataTable();
                string sCommText = "SELECT * FROM gsbkgrd WHERE Gs_bkgrd = '" + sGSBG + "'";

                this.CDSQuery(sCDSConnString, sCommText, dTbl);

                if (dTbl.Rows.Count > 0)
                {
                    bGSFound = true;
                }
                else if (dTbl.Rows.Count == 0)
                {
                    string sStop = string.Empty;
                    bGSFound = false;
                }
            }
            catch (Exception ex)
            {
                this.SaveExceptionToDB(ex);
                bGSFound = false;
            }

            return bGSFound;
        }

        public void StampIt(string sProdNum, string sAction, ref bool bStamped)
        {
            //need 2 stamps:
                // 1: Saved to DiscOrders table || DPRecSaved
                // 2: DP record submitted for processing  || DPRecProcd

            try
            {
                DataTable dTblStamps = new DataTable("dTblStamps");
                string sCommText = "SELECT * FROM Stamps WHERE Lookupnum = '" + sProdNum + "' AND Action = '" + sAction + "'";

                this.CDSQuery(sCDSConnString, sCommText, dTblStamps);

                if (dTblStamps.Rows.Count > 0)
                {

                }
                else if (dTblStamps.Rows.Count == 0)
                {
                    bool bSuccess = false;

                    sCommText = "INSERT INTO Stamps (User_id, Stationid, Lookupnum, Date, Time, Action, Wbs_task, Sequence, Framenum, Count," +
                    " Seconds, Wbs_plan, Wbs_track, Wbs_status, App_level, Processed) VALUES " +
                    "('DISCPROC', 'DProcessor', '" + sProdNum + "'," + " DATE(" + DateTime.Now.Date.ToString("yyyy,MM,dd").Trim() + "), '" + DateTime.Now.ToString("H:mm:ss").Trim() + "', '" + sAction + "', '" + sAction + "', 0, ' ', ' ', ' ', ' '," +
                    " .F., ' ', ' ', ' ' )";

                    this.CDSNonQuery(sCDSConnString, sCommText, ref bSuccess);

                    if (bSuccess == true)
                    {

                    }
                    else if (bSuccess != true)
                    {

                    }
                }
            }
            catch (Exception ex)
            {
                this.SaveExceptionToDB(ex);
            }
        }

        public void CheckForWebCodes(string sProdNum, ref string sDiscType)
        {
            try
            {
                DataTable dTblWebCodes = new DataTable("dTblWebCodes");
                string sCommText = "SELECT Code FROM Webcodes";

                this.CDSQuery(sCDSConnString, sCommText, dTblWebCodes);

                if (dTblWebCodes.Rows.Count > 0)
                {
                    foreach (DataRow dRowWebCodes in dTblWebCodes.Rows)
                    {
                        string sWebCode = Convert.ToString(dRowWebCodes["Code"]).Trim();

                        DataTable dTblCodes = new DataTable("dTblCodes");
                        sCommText = "SELECT * FROM Codes WHERE Lookupnum = '" + sProdNum + "' AND Code = '" + sWebCode + "'";

                        this.CDSQuery(sCDSConnString, sCommText, dTblCodes);

                        if (dTblCodes.Rows.Count > 0)
                        {
                            sDiscType += "W";

                            #region commented code. 

                            //if (sDiscType == "ICD")
                            //{
                            //    sDiscType = "ICDW";
                            //    return;
                            //}
                            //else if (sDiscType == "MEG")
                            //{
                            //    sDiscType = "MEGW";
                            //    return;
                            //}
                            //else if (sDiscType == "PEC")
                            //{
                            //    sDiscType = "PECW";
                            //    return;
                            //}

                            #endregion 
                        }
                        else if (dTblCodes.Rows.Count == 0)
                        {
                            // If no web codes appear in the order then leave original disctype.
                        }
                    }
                }
                else if (dTblWebCodes.Rows.Count == 0)
                {
                    string sStop = string.Empty;
                }
            }
            catch (Exception ex)
            {
                this.SaveExceptionToDB(ex);
            }
        }

        public void GetEventInfo(string sPath, string sRefNum, ref string sEventCode, ref string sEventPass, ref bool bHaveCodeAndPass)
        {
            try
            {
                string sImage = Path.GetFileName(sPath);

                DataTable dTblEventPics = new DataTable("dTblEventPics");
                string sCommText = "SELECT [eventcode] FROM [EventPics] WHERE [jpgnames] = '" + sImage + "' AND [refnum] = '" + sRefNum + "'";

                this.SQLQuery(sKrsolConnString, sCommText, dTblEventPics);

                if (dTblEventPics.Rows.Count > 0)
                {
                    sEventCode = Convert.ToString(dTblEventPics.Rows[0]["eventcode"]).Trim();

                    DataTable dTblEvent = new DataTable("dTblEvent");
                    sCommText = "SELECT [eventpass] FROM [Event] WHERE [eventcode] = '" + sEventCode + "'";

                    this.SQLQuery(sKrsolConnString, sCommText, dTblEvent);

                    if (dTblEvent.Rows.Count > 0)
                    {
                        sEventPass = Convert.ToString(dTblEvent.Rows[0]["eventpass"]).Trim();
                        bHaveCodeAndPass = true;
                    }
                    else if (dTblEvent.Rows.Count == 0)
                    {
                        string sStop = string.Empty;
                        bHaveCodeAndPass = false;
                    }
                }
                else if (dTblEventPics.Rows.Count == 0)
                {
                    string sStop = string.Empty;
                    bHaveCodeAndPass = false;
                }
            }
            catch (Exception ex)
            {
                this.SaveExceptionToDB(ex);
                bHaveCodeAndPass = false;
            }
        }

        public void MakeWebDirectories(string sRenderedPath, string sEventCode, string sEventPass, ref bool bMadeDirs, string sDiscType)
        {
            try
            {
                string sLabel = string.Empty;

                if (sDiscType == "ICDW")
                {
                    sLabel = "ICDWFilesDirectory";
                }
                else if (sDiscType != "ICDW")
                {
                    string sStop = string.Empty;

                    // Note: capture and send email notifying of setup required
                }

                string sValue = "Value";
                string sVariable = string.Empty;
                bool bSuccess = true;

                this.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                if (bSuccess == true)
                {
                    string sICDWFilesPath = sVariable;

                    // Create all of the directories needed.
                    foreach (string sICDWdirPath in Directory.GetDirectories(sICDWFilesPath, "*", SearchOption.AllDirectories))
                    {
                        Directory.CreateDirectory(sICDWdirPath.Replace(sICDWFilesPath, sRenderedPath));
                    }

                    // Copy all the files & replaces any files with the same name.
                    foreach (string sCopyPath in Directory.GetFiles(sICDWFilesPath, "*.*", SearchOption.AllDirectories))
                    {
                        File.Copy(sCopyPath, sCopyPath.Replace(sICDWFilesPath, sRenderedPath), true);
                    }

                    string sICDWPath = sRenderedPath + @"images\";
                    string sLoginXML = sRenderedPath + @"login.xml";
                    string sLoginXMLTemp = sRenderedPath + @"loginTEMP.xml";
                    string[] sImages = Directory.GetFiles(sRenderedPath, "*.jpg", SearchOption.TopDirectoryOnly);

                    // Move rendered images into the rendered path + \images\
                    foreach (string sImage in sImages)
                    {
                        string sFile = sRenderedPath + Path.GetFileName(sImage);
                        string sDestFile = sICDWPath + Path.GetFileName(sImage);

                        if (!Directory.Exists(sICDWPath))
                        {
                            Directory.CreateDirectory(sICDWPath);

                            File.Move(sFile, sDestFile);
                        }
                        else if (Directory.Exists(sICDWPath))
                        {
                            File.Move(sFile, sDestFile);
                        }
                    }

                    if (File.Exists(sLoginXML))
                    {
                        // Replace variables in the login.xml file.
                        string sText = File.ReadAllText(sLoginXML);

                        sText = sText.Replace("APSEVENT", sEventCode);
                        sText = sText.Replace("APSPASS", sEventPass);

                        File.WriteAllText(sLoginXMLTemp, sText);

                        File.Delete(sLoginXML);

                        File.Move(sLoginXMLTemp, sLoginXML);
                    }
                    else if (!File.Exists(sLoginXML))
                    {
                        string sStop = string.Empty;
                        bMadeDirs = false;
                    }
                }
                else if (bSuccess != true)
                {
                    string sStop = string.Empty;
                    bMadeDirs = false;
                }
                else if (sDiscType != "ICDW")
                {
                    string sStop = string.Empty;
                    bMadeDirs = false;
                }
            }
            catch (Exception ex)
            {
                this.SaveExceptionToDB(ex);
                bMadeDirs = false;
            }
        }

        public void CheckForStalledWork()
        {
            //10 - Exists in DP2
            //20 - Frame data processed
            //30 - Records added to the JobQueue table on HOLD
                //35 - Records added to the JobQueue table previously now flagged as READY
            //40 - Images have been rendered
            //50 - Copyright and Merge text files are with the rendered images 
            //60 - NWP files moved to initiate processing of the disc
            //80 - Redo
            //90 - Error
                //91 - Error email sent.

            try
            {
                string sListStatus = string.Empty;
                bool bSittingBased = false;

                List<int> lStatuses = new List<int>();
                lStatuses.Add(10);
                lStatuses.Add(20);
                lStatuses.Add(30);
                lStatuses.Add(35);
                lStatuses.Add(40);
                lStatuses.Add(50);
                lStatuses.Add(60);
                lStatuses.Add(80);
                lStatuses.Add(81);
                lStatuses.Add(82);

                foreach(int i in lStatuses)
                {
                    sListStatus = Convert.ToString(i);

                    DataTable dTblStalledWork = new DataTable("dTblStalledWork");
                    string sCommText = "SELECT * FROM [DiscOrders] WHERE [Status] = '" + sListStatus + "'";

                    this.SQLQuery(sDiscProcessorConnString, sCommText, dTblStalledWork);

                    if (dTblStalledWork.Rows.Count > 0)
                    {
                        foreach (DataRow dRowStalledWork in dTblStalledWork.Rows)
                        {
                            DateTime dTimeNow = DateTime.Now;
                            DateTime dTimeNowMinus1Hour = DateTime.Now.AddHours(-1);
                            DateTime dTimeNowMinus2Hours = DateTime.Now.AddHours(-2);
                            DateTime dTimeNowMinus6Hours = DateTime.Now.AddHours(-6);
                            DateTime dTimeLastCheck = Convert.ToDateTime(dRowStalledWork["LastCheck"]);

                            string sRefNum = Convert.ToString(dRowStalledWork["RefNum"]).Trim();
                            string sDiscType = Convert.ToString(dRowStalledWork["DiscType"]).Trim();

                            DataTable dTblGatherDiscTypes = new DataTable("dTblGatherDiscTypes");
                            sCommText = "SELECT * FROM [GatherDiscTypes] WHERE [GatherDiscType] = '" + sDiscType + "'";

                            this.SQLQuery(sDiscProcessorConnString, sCommText, dTblGatherDiscTypes);

                            if (dTblGatherDiscTypes.Rows.Count > 0)
                            {
                                bSittingBased = Convert.ToBoolean(dTblGatherDiscTypes.Rows[0]["SittingBased"]);

                                string sFrameNum = Convert.ToString(dRowStalledWork["FrameNum"]).Trim();
                                string sSitting = Convert.ToString(dRowStalledWork["Sitting"]).Trim();

                                if (dTimeLastCheck <= dTimeNowMinus2Hours)
                                {
                                    if (i == 10)
                                    {
                                        string sStop = string.Empty;

                                        string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ DiscOrders data not processed after 2 hours. ]";

                                        this.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sSitting, bSittingBased);

                                        string sStatus = "90";

                                        this.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                    }
                                    else if (i == 20)
                                    {
                                        string sStop = string.Empty;

                                        string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ FrameData records not processed after 2 hours. ]";

                                        this.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sSitting, bSittingBased);

                                        string sStatus = "90";

                                        this.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                    }
                                    else if (i == 30)
                                    {
                                        string sStop = string.Empty;

                                        string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ Records in JobQueue have not been flagged as READY after 2 hours. ]";

                                        this.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sSitting, bSittingBased);

                                        string sStatus = "90";

                                        this.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                    }
                                    else if (i == 35)
                                    {
                                        bool bSuccess = false;
                                        this.CheckForImages(sRefNum, sFrameNum, sDiscType, ref bSuccess);

                                        if (bSuccess == true)
                                        {
                                            string sStop = string.Empty;

                                            string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ Records in JobQueue have been flagged as READY but not rendered after 2 hours. ]";

                                            this.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sSitting, bSittingBased);

                                            string sStatus = "90";

                                            this.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                        }
                                        else if (bSuccess != true)
                                        {
                                            string sStop = string.Empty;

                                            string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ Images not located on server. ]";

                                            this.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sSitting, bSittingBased);

                                            string sStatus = "90";

                                            this.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                        }
                                    }
                                    else if (i == 40)
                                    {
                                        string sStop = string.Empty;

                                        string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ Images have been rendered for 2 hours but no Copyright or Merge files generated. ]";

                                        this.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sSitting, bSittingBased);

                                        string sStatus = "90";

                                        this.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                    }
                                    else if (i == 50)
                                    {
                                        string sStop = string.Empty;

                                        string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ Images rendered, Copyright and Merge files generated but no NWP file generation after 2 hours. ]";

                                        this.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sSitting, bSittingBased);

                                        string sStatus = "90";

                                        this.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                    }
                                    else if (i == 80)
                                    {
                                        string sStop = string.Empty;

                                        string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ Record flagged as REDO but no specific REDO action chosen. ]";

                                        this.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sSitting, bSittingBased);

                                        string sStatus = "90";

                                        this.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                    }
                                    else if (i == 81)
                                    {
                                        string sStop = string.Empty;

                                        string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ Record flagged as complete REDO but no further action after 2 hours. ]";

                                        this.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sSitting, bSittingBased);

                                        string sStatus = "90";

                                        this.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                    }
                                    else if (i == 82)
                                    {
                                        string sStop = string.Empty;

                                        string sErrorDescription = "[ " + DateTime.Now.ToString() + " ][ Record flagged as partial REDO but no further action after 2 hours. ]";

                                        this.UpdateDiscOrdersForErrors(sErrorDescription, sRefNum, sFrameNum, sDiscType, sSitting, bSittingBased);

                                        string sStatus = "90";

                                        this.UpdateDiscOrdersTableStatusFrameBased(sRefNum, sFrameNum, sStatus, sDiscType);
                                    }
                                }
                            }
                            else if (dTblGatherDiscTypes.Rows.Count == 0)
                            {

                            }
                        }
                    }
                    else if (dTblStalledWork.Rows.Count == 0)
                    {

                    }
                }
            }
            catch (Exception ex)
            {
                this.SaveExceptionToDB(ex);
            }
        }

        public void CheckForImages(string sRefNum, string sFrameNum, string sDiscType, ref bool bSuccess)
        {
            try
            {
                string sSitting = string.Empty;
                string sImageLocation = string.Empty;
                string sProdNum = string.Empty;

                DataTable dTblDiscOrders = new DataTable("dTblDiscOrders");
                string sCommText = "SELECT * FROM [DiscOrders] WHERE [RefNum] = '" + sRefNum + "' AND [FrameNum] = '" + sFrameNum + "' AND [DiscType] = '" + sDiscType + "'";

                this.SQLQuery(sDiscProcessorConnString, sCommText, dTblDiscOrders);

                if (dTblDiscOrders.Rows.Count > 0)
                {
                    sSitting = Convert.ToString(dTblDiscOrders.Rows[0]["Sitting"]).Trim();
                    sImageLocation = Path.GetDirectoryName(Convert.ToString(dTblDiscOrders.Rows[0]["ImageLocation"]).Trim());
                    sProdNum = Convert.ToString(dTblDiscOrders.Rows[0]["ProdNum"]).Trim();

                    string sImage = string.Empty;

                    DataTable dTblFrames = new DataTable("dTblFrames");
                    sCommText = "SELECT * FROM Frames WHERE Lookupnum = '" + sProdNum + "' AND Sitting = '" + sSitting + "'";

                    this.CDSQuery(sCDSConnString, sCommText, dTblFrames);

                    if (dTblFrames.Rows.Count > 0)
                    {
                        sImage = Convert.ToString(dTblFrames.Rows[0]["Image_id"]).Trim();

                        List<string> lImageTypes = new List<string>();
                        lImageTypes.Add("*.jpg");
                        lImageTypes.Add("*.png");

                        foreach (string s in lImageTypes)
                        {
                            string[] sFilesJPG = Directory.GetFiles(sImageLocation, s);

                            foreach (string file in sFilesJPG)
                            {
                                string sFile = Convert.ToString(file);
                                sFile = Path.GetFileName(sFile);
                                string sImagelowercase = sImage.ToLower().Trim();

                                if (sFile == sImage || sFile == sImagelowercase)
                                {
                                    bSuccess = true;
                                }
                                else if (sFile != sImage || sFile != sImagelowercase)
                                {

                                }
                            }
                        }
                    }
                    else if (dTblFrames.Rows.Count == 0)
                    {
                        string sStop = string.Empty;
                    }
                }
                else if (dTblDiscOrders.Rows.Count == 0)
                {
                    string sStop = string.Empty;
                }
            }
            catch (Exception ex)
            {
                this.SaveExceptionToDB(ex);
            }
        }

        public void CheckForResubmits()
        {
            try
            {
                DataTable dTblResubmits = new DataTable("dTblResubmits");
                string sCommText = "SELECT * FROM DP_Resubmits WHERE Status = 80";

                this.CDSQuery(sCDSConnString, sCommText, dTblResubmits);

                if (dTblResubmits.Rows.Count > 0)
                {
                    foreach (DataRow dRowResubmits in dTblResubmits.Rows)
                    {
                        string sProdNum = Convert.ToString(dRowResubmits["lookupnum"]).Trim();
                        string sSequence = Convert.ToString(dRowResubmits["sequence"]).Trim();
                        string sFrameNum = sSequence.PadLeft(4, '0');
                        string sSitting = Convert.ToString(dRowResubmits["sitting"]);

                        int iResubmitCount = 0;

                        DataTable dTblDiscOrders = new DataTable("dTblDiscOrders");
                        sCommText = "SELECT * FROM [DiscOrders] WHERE [ProdNum] = '" + sProdNum + "' AND ([FrameNum] = '" + sFrameNum + "' OR [Sitting] = '" + sSitting + "')";

                        this.SQLQuery(sDiscProcessorConnString, sCommText, dTblDiscOrders);

                        if (dTblDiscOrders.Rows.Count > 0)
                        {
                            // Note: ************This will need to be a foreach if multiple disctypes are ordered for a single frame**************************

                            iResubmitCount = Convert.ToInt32(dTblDiscOrders.Rows[0]["ResubmitCount"]);
                            iResubmitCount += 1;
                            string sDiscType = Convert.ToString(dTblDiscOrders.Rows[0]["DiscType"]).Trim();

                            DataTable dTblFrameData = new DataTable("dTblFrameData");
                            sCommText = "SELECT * FROM [FrameData] WHERE [ProdNum] = '" + sProdNum + "' AND [FrameNum] = '" + sFrameNum + "'";

                            this.SQLQuery(sDiscProcessorConnString, sCommText, dTblFrameData);

                            if (dTblFrameData.Rows.Count > 0)
                            {
                                sCommText = "UPDATE DP_Resubmits SET Status = 81 WHERE lookupnum = '" + sProdNum + "' AND sequence = " + sFrameNum + "";
                                bool bSuccess = false;

                                this.CDSNonQuery(sCDSConnString, sCommText, ref bSuccess);

                                if (bSuccess == true)
                                {
                                    sCommText = "DELETE FROM [FrameData] WHERE [ProdNum] = '" + sProdNum + "' AND [FrameNum] = '" + sFrameNum + "'";
                                    bool bSuccess1 = false;

                                    this.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess1);

                                    if (bSuccess1 == true)
                                    {
                                        bool bDeleted = false;
                                        this.DeleteRenderedDirectoryAndFiles(sProdNum, sFrameNum, ref bDeleted, sDiscType);

                                        if (bDeleted == true)
                                        {
                                            sCommText = "UPDATE [DiscOrders] SET [Status] = '10', [LastCheck] = '" + DateTime.Now.ToString("MM/dd/yy H:mm:ss").Trim() + "', [ResubmitCount] = '"
                                                 + iResubmitCount + "', [Error] = '0', [ErrorChecked] = '0', [ErrorDescription] = '' WHERE ProdNum = '" + sProdNum + "' AND FrameNum = '" + sFrameNum + "'";
                                            bool bSuccess2 = false;

                                            this.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess2);

                                            if (bSuccess2 == true)
                                            {
                                                // Done.
                                            }
                                            else if (bSuccess2 != true)
                                            {
                                                string sBody = "A resubmit was submitted but the updating of the DiscOrders table failed for ProdNum: " + sProdNum + " FrameNum: " + sFrameNum + ".";

                                                this.EmailAndStatusUpdatesForResubmits(sProdNum, sFrameNum, sBody);
                                            }
                                        }
                                        else if (bDeleted != true)
                                        {
                                            string sBody = "A resubmit was submitted but the rendered directory could not be deleted for ProdNum: " + sProdNum + " FrameNum: " + sFrameNum + ".";

                                            this.EmailAndStatusUpdatesForResubmits(sProdNum, sFrameNum, sBody);
                                        }
                                    }
                                    else if (bSuccess1 != true)
                                    {
                                        string sBody = "A resubmit was submitted but the deleting of the FrameData records failed for ProdNum: " + sProdNum + " FrameNum: " + sFrameNum + ".";

                                        this.EmailAndStatusUpdatesForResubmits(sProdNum, sFrameNum, sBody);
                                    }
                                }
                                else if (bSuccess != true)
                                {
                                    string sBody = "A resubmit was submitted but the updating of the DP_Resubmit records failed for ProdNum: " + sProdNum + " FrameNum: " + sFrameNum + ".";

                                    this.EmailAndStatusUpdatesForResubmits(sProdNum, sFrameNum, sBody);
                                }
                            }
                            else if (dTblFrameData.Rows.Count == 0)
                            {
                                sCommText = "UPDATE [DiscOrders] SET [Status] = '10', [LastCheck] = '" + DateTime.Now.ToString("MM/dd/yy H:mm:ss").Trim() + "', [ResubmitCount] = '"
                                    + iResubmitCount + "', [Error] = '0', [ErrorChecked] = '0', [ErrorDescription] = '' WHERE ProdNum = '" + sProdNum + "' AND FrameNum = '" + sFrameNum + "'";
                                bool bSuccess = false;

                                this.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess);

                                if (bSuccess == true)
                                {
                                    sCommText = "UPDATE DP_Resubmits SET Status = 81 WHERE lookupnum = '" + sProdNum + "' AND sequence = " + sFrameNum + "";
                                    bool bSuccess2 = false;

                                    this.CDSNonQuery(sCDSConnString, sCommText, ref bSuccess2);

                                    if (bSuccess2 == true)
                                    {
                                        // Done.
                                    }
                                    else if (bSuccess2 != true)
                                    {
                                        string sBody = "A resubmit was submitted but the updating of the DP_Resubmit records failed for ProdNum: " + sProdNum + " FrameNum: " + sFrameNum + ".";

                                        this.EmailAndStatusUpdatesForResubmits(sProdNum, sFrameNum, sBody);
                                    }
                                }
                                else if (bSuccess != true)
                                {
                                    string sBody = "A resubmit was submitted but the updating of the DiscOrders table failed for ProdNum: " + sProdNum + " FrameNum: " + sFrameNum + ".";

                                    this.EmailAndStatusUpdatesForResubmits(sProdNum, sFrameNum, sBody);
                                }
                            }
                        }
                        else if (dTblDiscOrders.Rows.Count == 0)
                        {
                            string sBody = "A resubmit was submitted but the was not located in the DiscOrders table for ProdNum: " + sProdNum + " FrameNum: " + sFrameNum + ".";

                            this.EmailAndStatusUpdatesForResubmits(sProdNum, sFrameNum, sBody);
                        }
                    }
                }
                else if (dTblResubmits.Rows.Count == 0)
                {
                    // No work to do.
                }
            }
            catch (Exception ex)
            {
                this.SaveExceptionToDB(ex);
            }
        }

        public void EmailAndStatusUpdatesForResubmits(string sProdNum, string sFrameNum, string sBody)
        {
            try
            {
                string sSubject = "DiscProcessor error notification.";

                string sEmailServer = string.Empty;
                string sCCSendTo = string.Empty;
                string sSendTo = string.Empty;
                string sTextMsgSendTo = string.Empty;
                bool bSuccess3 = false;

                this.GatherEmailVariables(ref sEmailServer, ref sCCSendTo, ref sSendTo, ref sTextMsgSendTo, ref bSuccess3);

                if (bSuccess3 == true)
                {
                    this.EmailError(sEmailServer, sCCSendTo, sSendTo, sSubject, sBody, sTextMsgSendTo);

                    string sCommText = "UPDATE [DiscOrders] SET [Status] = '91' WHERE ProdNum = '" + sProdNum + "' AND FrameNum = '" + sFrameNum + "'";
                    bool bSuccess4 = false;

                    this.SQLNonQuery(sDiscProcessorConnString, sCommText, ref bSuccess4);

                    if (bSuccess4 == true)
                    {
                        sCommText = "UPDATE DP_Resubmits SET Status = 82 WHERE lookupnum = '" + sProdNum + "' AND sequence = " + sFrameNum + "";
                        bool bSuccess5 = false;

                        this.CDSNonQuery(sCDSConnString, sCommText, ref bSuccess5);

                        if (bSuccess5 == true)
                        {
                            // Done.
                        }
                        else if (bSuccess5 != true)
                        {
                            string sStop = string.Empty;
                        }
                    }
                    else if (bSuccess4 != true)
                    {
                        string sStop = string.Empty;
                    }
                }
                else if (bSuccess3 != true)
                {
                    string sStop = string.Empty;
                }
            }
            catch (Exception ex)
            {
                this.SaveExceptionToDB(ex);
            }
        }

        public bool DeleteRenderedDirectoryAndFiles(string sProdNum, string sFrameNum, ref bool bDeleted, string sDiscType)
        {
            try
            {
                string sLabel = string.Empty;

                if (sDiscType == "ICD" || sDiscType == "ICDW")
                {
                    sLabel = "ICDRenderedPath";
                }
                else if (sDiscType == "MEG" || sDiscType == "MEGW")
                {
                    sLabel = "MEGRenderedPath";
                }
                else  if (sDiscType == "PEC" || sDiscType == "PECW")
                {
                    sLabel = "PECRenderedPath";
                }

                string sValue = "Value";
                string sVariable = string.Empty;
                bool bSuccess = true;

                this.GatherVariables(sLabel, sValue, ref sVariable, ref bSuccess);

                if (bSuccess == true)
                {
                    string sRenderedPath = sVariable + sProdNum + sFrameNum;

                    if (Directory.Exists(sRenderedPath))
                    {
                        Directory.Delete(sRenderedPath, true);

                        bDeleted = true;
                    }
                    else if (!Directory.Exists(sRenderedPath))
                    {

                    }
                }
                else if (bSuccess != true)
                {
                    bDeleted = false;

                    string sStop = string.Empty;
                }
            }
            catch (Exception ex)
            {
                bDeleted = false;
                this.SaveExceptionToDB(ex);
            }
            return bDeleted;
        }

        public string GetOriginalCustomerFromIMQ(ref string sCustNum, string sProdNum)
        {
            try
            {
                string sCustID = string.Empty;
                string sAPSCust = string.Empty;

                DataTable dTblItems = new DataTable("dTblItems");
                string sCommText = "SELECT Custid FROM Items WHERE Lookupnum = '" + sProdNum + "'";

                this.CDSQuery(sCDSConnString, sCommText, dTblItems);

                if (dTblItems.Rows.Count > 0)
                {
                    sCustID = Convert.ToString(dTblItems.Rows[0]["Custid"]).Trim();

                    DataTable dTblIMQ_Orders = new DataTable("dTblIMQ_Orders");
                    sCommText = "SELECT Apscust FROM Imq_orders WHERE Racnum = '" + sCustID + "'";

                    this.CDSQuery(sCDSConnString, sCommText, dTblIMQ_Orders);

                    if (dTblIMQ_Orders.Rows.Count > 0)
                    {
                        sAPSCust = Convert.ToString(dTblIMQ_Orders.Rows[0]["Apscust"]).Trim();

                        sCustNum = sAPSCust;
                    }
                    else if (dTblIMQ_Orders.Rows.Count == 0)
                    {
                        string sStop = string.Empty;
                    }
                }
                else if (dTblItems.Rows.Count == 0)
                {
                    string sStop = string.Empty;
                }
            }
            catch (Exception ex)
            {
                this.SaveExceptionToDB(ex);
            }
            return sCustNum;
        }

        public bool IsDigitsOnly(string sString, ref bool bDigitOnly)
        {
            foreach (char c in sString)
            {
                if (c < '0' || c > '9')
                {
                    bDigitOnly = false;
                }
                else
                {
                    bDigitOnly = true;
                }
            }
            return bDigitOnly;
        }

        public string GetCurrentLogFile(ref string sCurrentLogFile, ref bool bLogFileExists)
        {
            try
            {
                DataTable dTblCurrentLogFile = new DataTable("dTblCurrentLogFile");
                string sCommText = "SELECT * FROM [Variables] WHERE [Label] = 'CurrentDPLogFile'";

                this.SQLQuery(sDiscProcessorConnString, sCommText, dTblCurrentLogFile);

                if (dTblCurrentLogFile.Rows.Count > 0)
                {
                    sCurrentLogFile = Convert.ToString(dTblCurrentLogFile.Rows[0]["Value"]).Trim();

                    if (File.Exists(sCurrentLogFile))
                    {
                        bLogFileExists = true;
                    }
                    else if (!File.Exists(sCurrentLogFile))
                    {
                        bLogFileExists = false;
                    }
                }
                else if (dTblCurrentLogFile.Rows.Count == 0)
                {
                    string sStop = string.Empty;
                    bLogFileExists = false;
                }
            }
            catch (Exception ex)
            {
                this.SaveExceptionToDB(ex);
                bLogFileExists = false;
            }

            return sCurrentLogFile;
        }

        public DataTable GetSittingsAndFilterDataTableForSittingBasedDiscs(ref DataTable dTbl)
        {
            try
            {
                string sdTblProdNum = string.Empty;
                string sdTblSequence = string.Empty;
                string sdTblSitting = string.Empty;
                string sFramesProdNum = string.Empty;
                string sFramesSequence = string.Empty;
                string sFramesSitting = string.Empty;
                DataTable dTblFrames = new DataTable("dTblFrames");
                string sCommText = string.Empty;

                foreach (DataRow dRow in dTbl.Rows)
                {
                    sdTblProdNum = Convert.ToString(dRow["Lookupnum"]).Trim();
                    sdTblSequence = Convert.ToString(dRow["Sequence"]).Trim();
                    sdTblSitting = Convert.ToString(dRow["Sitting"]).Trim();

                    sCommText = "SELECT Sitting, Lookupnum, Sequence FROM Frames WHERE Lookupnum = '" + sdTblProdNum + "' AND Sequence = " + sdTblSequence;

                    this.CDSQuery(sCDSConnString, sCommText, dTblFrames);

                    if (dTblFrames.Rows.Count > 0)
                    {
                        sFramesProdNum = Convert.ToString(dTblFrames.Rows[0]["Lookupnum"]).Trim();
                        sFramesSequence = Convert.ToString(dTblFrames.Rows[0]["Sequence"]).Trim();
                        sFramesSitting = Convert.ToString(dTblFrames.Rows[0]["Sitting"]).Trim();

                        if (sdTblProdNum == sFramesProdNum && sdTblSequence == sFramesSequence)
                        {
                            if (sdTblSitting == "0" && (sFramesSitting != string.Empty && sdTblSitting != sFramesSitting))
                            {
                                //dRow["Sitting"] = sFramesSitting;
                                dRow["Sitting"] = Convert.ToInt32(sFramesSitting);
                            }
                            else if (sdTblSitting != "0" || sFramesSitting == string.Empty || sdTblSitting == sFramesSitting)
                            {
                                // Do nothing.
                            }
                        }

                    }
                    else if (dTblFrames.Rows.Count == 0)
                    {
                        string sStop = string.Empty;
                    }
                }

                string sPreviousProdNum = string.Empty;
                string sPreviousSequence = string.Empty;
                string sPreviousSitting = string.Empty;
                string sCurrentProdNum = string.Empty;
                string sCurrentSequence = string.Empty;
                string sCurrentSitting = string.Empty;

                for (int i = dTbl.Rows.Count - 1; i >= 0; i--)
                {
                    DataRow dRowFrames = dTbl.Rows[i];

                    sCurrentProdNum = Convert.ToString(dRowFrames["Lookupnum"]).Trim();
                    sCurrentSequence = Convert.ToString(dRowFrames["Sequence"]).Trim();
                    sCurrentSitting = Convert.ToString(dRowFrames["Sitting"]).Trim();

                    if (sCurrentProdNum == sPreviousProdNum && sCurrentSequence != sPreviousSequence && sCurrentSitting == sPreviousSitting)
                    {
                        dRowFrames.Delete();
                        dTbl.AcceptChanges();
                    }

                    sPreviousProdNum = sCurrentProdNum;
                    sPreviousSequence = sCurrentSequence;
                    sPreviousSitting = sCurrentSitting;
                }
            }
            catch (Exception ex)
            {
                this.SaveExceptionToDB(ex);
            }
            return dTbl;
        }

        public bool ImageExists(string sPath, ref bool bExists)
        {
            try
            {
                if (File.Exists(sPath))
                {
                    bExists = true;
                }
                else if (!File.Exists(sPath))
                {
                    bExists = false;
                }
            }
            catch (Exception ex)
            {
                bExists = false;
                this.SaveExceptionToDB(ex);
            }
            return bExists;
        }

        public bool IsDiscTypeSittingBased(string sDiscType, ref bool bSittingBased, ref bool bSuccess)
        {
            try
            {
                DataTable dTblGatherDiscTypes = new DataTable("dTblGatherDiscTypes");
                string sCommText = "SELECT * FROM [GatherDiscTypes] WHERE [GatherDiscType] = '" + sDiscType + "'";

                this.SQLQuery(sDiscProcessorConnString, sCommText, dTblGatherDiscTypes);

                if (dTblGatherDiscTypes.Rows.Count > 0)
                {
                    bSuccess = true;

                    bSittingBased = Convert.ToBoolean(dTblGatherDiscTypes.Rows[0]["SittingBased"]);
                }
                else if (dTblGatherDiscTypes.Rows.Count == 0)
                {
                    bSuccess = false;
                }
            }
            catch (Exception ex)
            {
                this.SaveExceptionToDB(ex);
                bSuccess = false;
            }

            return bSuccess;
        }

        public void PopulateCDSDiscOrders()
        {
            try
            {
                DataTable dTblDiscOrders = new DataTable("dTblDiscOrders");
                string sCommText = "SELECT * FROM [DiscOrders]";

                this.SQLQuery(sDiscProcessorConnString, sCommText, dTblDiscOrders);

                if (dTblDiscOrders.Rows.Count > 0)
                {
                    foreach (DataRow dRowDiscOrders in dTblDiscOrders.Rows)
                    {
                        string sRefNum = Convert.ToString(dRowDiscOrders["RefNum"]).Trim();
                        string sProdNum = Convert.ToString(dRowDiscOrders["ProdNum"]).Trim();
                        string sFrameNum = Convert.ToString(dRowDiscOrders["FrameNum"]).Trim();
                        string sCDSFrameNum = sFrameNum.TrimStart(new Char[] { '0' });
                        string sSitting = Convert.ToString(dRowDiscOrders["Sitting"]);
                        string sDiscType = Convert.ToString(dRowDiscOrders["DiscType"]).Trim();

                        bool bSuccess = false;

                        sCommText = "INSERT INTO DiscOrders (Cust_ref, Lookupnum, Sequence, Sitting, DiscType) VALUES ('" + sRefNum + "', '" + sProdNum + "', " +
                            sCDSFrameNum + ", '" + sSitting + "', '" + sDiscType + "')";

                        this.CDSNonQuery(sCDSConnString, sCommText, ref bSuccess);

                        if (bSuccess == true)
                        {

                        }
                        else if (bSuccess != true)
                        {
                            string sStop = string.Empty;
                        }
                    }
                }
                else if (dTblDiscOrders.Rows.Count == 0)
                {
                    string sStop = string.Empty;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString().Trim());
            }
        }

        public void ParseNWPlog()
        {
            try
            {

            }
            catch (Exception ex)
            {
                this.SaveExceptionToDB(ex);
            }
        }
    }
}
