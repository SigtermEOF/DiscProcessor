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
    public partial class Login : Form
    {
        TaskMethods TM02 = null;
        string sCDSConnString = APS_DiscProcessor.Properties.Settings.Default.CDSConnString.ToString();
        string sDiscProcessorConnString = APS_DiscProcessor.Properties.Settings.Default.DiscProcessorConnString.ToString();
        public bool bLoginSuccess { get; set; }
        public string sUser { get; set;  }

        public Login()
        {
            InitializeComponent();

            TM02 = new TaskMethods();
        }

        private void Login_Load(object sender, EventArgs e)
        {
            string sStop = string.Empty;

            this.CreatePWTextBox();

            this.textBox1.Focus();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string sText = this.textBox1.Text;
            bool bDigitOnly = false;

            if (sText != string.Empty || sText != "")
            {
                if (sText == "MAINT")
                {
                    // Override this check for Todd's alpha password.
                    bDigitOnly = true;
                }
                else if (sText != "MAINT")
                {
                    TM02.IsDigitsOnly(sText, ref bDigitOnly);
                }                

                if (bDigitOnly == true)
                {
                    DataTable dTblUsers = new DataTable("dTblUsers");
                    string sCommText = "SELECT * FROM Users WHERE Password = '" + sText + "'";

                    TM02.CDSQuery(sCDSConnString, sCommText, dTblUsers);

                    if (dTblUsers.Rows.Count > 0)
                    {
                        string sUserID = Convert.ToString(dTblUsers.Rows[0]["User_id"]).Trim();

                        DataTable dTblVariables = new DataTable("dTblVariables");
                        sCommText = "SELECT [Value] FROM [Variables] WHERE [Label] = 'Admin'";

                        TM02.SQLQuery(sDiscProcessorConnString, sCommText, dTblVariables);

                        if (dTblVariables.Rows.Count > 0)
                        {
                            string sAdmins = Convert.ToString(dTblVariables.Rows[0]["Value"]).Trim();
                            List<string> lAdmins = sAdmins.Split(',').ToList();

                            foreach (string s in lAdmins)
                            {
                                string sAdmin = s.Trim();

                                if (sUserID == sAdmin)
                                {
                                    bLoginSuccess = true;

                                    sUser = sUserID;

                                    this.Close();
                                }
                            }
                        }
                        else if (dTblUsers.Rows.Count == 0)
                        {
                            string sStop = string.Empty;
                        }
                    }
                    else if (dTblUsers.Rows.Count == 0)
                    {
                        MessageBox.Show("Invalid employee number");
                        this.textBox1.Text = string.Empty;
                        this.textBox1.Focus();
                        Application.DoEvents();
                    }
                }
                else if (bDigitOnly != true)
                {
                    MessageBox.Show("Please enter a numeric employee number");
                    this.textBox1.Text = string.Empty;
                    this.textBox1.Focus();
                    Application.DoEvents();
                }
            }
            else if (sText == string.Empty || sText == "")
            {
                MessageBox.Show("Please enter an employee number.");
                this.textBox1.Text = string.Empty;
                this.textBox1.Focus();
                Application.DoEvents();
            }
        }

        private void CreatePWTextBox()
        {
            try
            {
                textBox1.MaxLength = 8;
                textBox1.PasswordChar = '*';
                textBox1.TextAlign = HorizontalAlignment.Center;
            }
            catch (Exception ex)
            {
                TM02.SaveExceptionToDB(ex);
            }
        }
    }
}
