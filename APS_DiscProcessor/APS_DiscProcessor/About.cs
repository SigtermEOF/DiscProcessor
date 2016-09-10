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
    public partial class About : Form
    {
        TaskMethods TM01 = null;
        string sDiscProcessorConnString = APS_DiscProcessor.Properties.Settings.Default.DiscProcessorConnString.ToString();

        public About()
        {
            InitializeComponent();

            TM01 = new TaskMethods();
        }

        private void About_Load(object sender, EventArgs e)
        {
            string sVersion = string.Empty;

            DataTable dTblVersion = new DataTable("dTblVersion");
            string sCommText = "SELECT * FROM [ChangeLog] WHERE [App] = 'Processor'";

            TM01.SQLQuery(sDiscProcessorConnString, sCommText, dTblVersion);

            if (dTblVersion.Rows.Count > 0)
            {
                DataRow dRowVersion = dTblVersion.Rows[dTblVersion.Rows.Count - 1];

                sVersion = Convert.ToString(dRowVersion["Version"]).Trim();

                this.lblVersion.Text = "Version: " + sVersion;

                this.lblCopyright.Text = "© Advanced Photographic Solutions LLC " + DateTime.Now.Year + " TM: Advanced Photographic Solutions LLC";

                Application.DoEvents();
            }
            else if (dTblVersion.Rows.Count == 0)
            {
                string sStop = string.Empty;
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }        
    }
}
