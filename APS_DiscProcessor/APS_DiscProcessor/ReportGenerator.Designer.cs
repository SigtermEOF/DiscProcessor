namespace APS_DiscProcessor
{
    partial class ReportGenerator
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            Microsoft.Reporting.WinForms.ReportDataSource reportDataSource1 = new Microsoft.Reporting.WinForms.ReportDataSource();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ReportGenerator));
            this.repViewerDiscOrders = new Microsoft.Reporting.WinForms.ReportViewer();
            this.DiscProcessorDataSet = new APS_DiscProcessor.DiscProcessorDataSet();
            this.btnEnter = new System.Windows.Forms.Button();
            this.txtBoxQueryString = new System.Windows.Forms.TextBox();
            this.discOrdersbindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.discOrdersTableAdapter = new APS_DiscProcessor.DiscProcessorDataSetTableAdapters.DiscOrdersTableAdapter();
            this.lblWHERE = new System.Windows.Forms.Label();
            this.txtBoxPrevQuery = new System.Windows.Forms.TextBox();
            this.lblPrevQuery = new System.Windows.Forms.Label();
            this.cmboBoxTables = new System.Windows.Forms.ComboBox();
            this.lblTable = new System.Windows.Forms.Label();
            this.FrameDataBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.FrameDataTableAdapter = new APS_DiscProcessor.DiscProcessorDataSetTableAdapters.FrameDataTableAdapter();
            this.lblRecsReturned = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.DiscProcessorDataSet)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.discOrdersbindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.FrameDataBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // repViewerDiscOrders
            // 
            this.repViewerDiscOrders.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            reportDataSource1.Name = "DiscOrdersReportingDS";
            reportDataSource1.Value = this.discOrdersbindingSource;
            this.repViewerDiscOrders.LocalReport.DataSources.Add(reportDataSource1);
            this.repViewerDiscOrders.LocalReport.ReportEmbeddedResource = "APS_DiscProcessor.DiscOrders.rdlc";
            this.repViewerDiscOrders.Location = new System.Drawing.Point(12, 323);
            this.repViewerDiscOrders.Name = "repViewerDiscOrders";
            this.repViewerDiscOrders.Size = new System.Drawing.Size(1381, 426);
            this.repViewerDiscOrders.TabIndex = 4;
            // 
            // DiscProcessorDataSet
            // 
            this.DiscProcessorDataSet.DataSetName = "DiscProcessorDataSet";
            this.DiscProcessorDataSet.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema;
            // 
            // btnEnter
            // 
            this.btnEnter.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnEnter.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnEnter.Location = new System.Drawing.Point(569, 289);
            this.btnEnter.Name = "btnEnter";
            this.btnEnter.Size = new System.Drawing.Size(75, 28);
            this.btnEnter.TabIndex = 1;
            this.btnEnter.Text = "&Enter";
            this.btnEnter.UseVisualStyleBackColor = true;
            this.btnEnter.Click += new System.EventHandler(this.button1_Click);
            // 
            // txtBoxQueryString
            // 
            this.txtBoxQueryString.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.txtBoxQueryString.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtBoxQueryString.Location = new System.Drawing.Point(396, 26);
            this.txtBoxQueryString.Multiline = true;
            this.txtBoxQueryString.Name = "txtBoxQueryString";
            this.txtBoxQueryString.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtBoxQueryString.Size = new System.Drawing.Size(435, 257);
            this.txtBoxQueryString.TabIndex = 0;
            // 
            // discOrdersbindingSource
            // 
            this.discOrdersbindingSource.DataMember = "DiscOrders";
            this.discOrdersbindingSource.DataSource = this.DiscProcessorDataSet;
            // 
            // discOrdersTableAdapter
            // 
            this.discOrdersTableAdapter.ClearBeforeFill = true;
            // 
            // lblWHERE
            // 
            this.lblWHERE.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.lblWHERE.AutoSize = true;
            this.lblWHERE.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblWHERE.ForeColor = System.Drawing.SystemColors.Control;
            this.lblWHERE.Location = new System.Drawing.Point(280, 27);
            this.lblWHERE.Name = "lblWHERE";
            this.lblWHERE.Size = new System.Drawing.Size(109, 19);
            this.lblWHERE.TabIndex = 3;
            this.lblWHERE.Text = "Query string:";
            this.lblWHERE.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtBoxPrevQuery
            // 
            this.txtBoxPrevQuery.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.txtBoxPrevQuery.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Bold);
            this.txtBoxPrevQuery.Location = new System.Drawing.Point(850, 27);
            this.txtBoxPrevQuery.Multiline = true;
            this.txtBoxPrevQuery.Name = "txtBoxPrevQuery";
            this.txtBoxPrevQuery.ReadOnly = true;
            this.txtBoxPrevQuery.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtBoxPrevQuery.Size = new System.Drawing.Size(435, 257);
            this.txtBoxPrevQuery.TabIndex = 5;
            // 
            // lblPrevQuery
            // 
            this.lblPrevQuery.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.lblPrevQuery.AutoSize = true;
            this.lblPrevQuery.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPrevQuery.ForeColor = System.Drawing.SystemColors.Control;
            this.lblPrevQuery.Location = new System.Drawing.Point(995, 298);
            this.lblPrevQuery.Name = "lblPrevQuery";
            this.lblPrevQuery.Size = new System.Drawing.Size(153, 19);
            this.lblPrevQuery.TabIndex = 6;
            this.lblPrevQuery.Text = "^ Previous Query ^";
            // 
            // cmboBoxTables
            // 
            this.cmboBoxTables.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmboBoxTables.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold);
            this.cmboBoxTables.FormattingEnabled = true;
            this.cmboBoxTables.Items.AddRange(new object[] {
            "DiscOrders",
            "FrameData"});
            this.cmboBoxTables.Location = new System.Drawing.Point(12, 48);
            this.cmboBoxTables.Name = "cmboBoxTables";
            this.cmboBoxTables.Size = new System.Drawing.Size(194, 24);
            this.cmboBoxTables.TabIndex = 7;
            this.cmboBoxTables.SelectedIndexChanged += new System.EventHandler(this.cmboBoxTables_SelectedIndexChanged);
            // 
            // lblTable
            // 
            this.lblTable.AutoSize = true;
            this.lblTable.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTable.ForeColor = System.Drawing.SystemColors.Control;
            this.lblTable.Location = new System.Drawing.Point(77, 26);
            this.lblTable.Name = "lblTable";
            this.lblTable.Size = new System.Drawing.Size(56, 19);
            this.lblTable.TabIndex = 8;
            this.lblTable.Text = "Table:";
            // 
            // FrameDataBindingSource
            // 
            this.FrameDataBindingSource.DataMember = "FrameData";
            this.FrameDataBindingSource.DataSource = this.DiscProcessorDataSet;
            // 
            // FrameDataTableAdapter
            // 
            this.FrameDataTableAdapter.ClearBeforeFill = true;
            // 
            // lblRecsReturned
            // 
            this.lblRecsReturned.AutoSize = true;
            this.lblRecsReturned.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRecsReturned.ForeColor = System.Drawing.Color.Silver;
            this.lblRecsReturned.Location = new System.Drawing.Point(77, 182);
            this.lblRecsReturned.Name = "lblRecsReturned";
            this.lblRecsReturned.Size = new System.Drawing.Size(153, 19);
            this.lblRecsReturned.TabIndex = 9;
            this.lblRecsReturned.Text = "Records returned: ";
            // 
            // ReportGenerator
            // 
            this.AcceptButton = this.btnEnter;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.ClientSize = new System.Drawing.Size(1413, 769);
            this.Controls.Add(this.lblRecsReturned);
            this.Controls.Add(this.lblTable);
            this.Controls.Add(this.cmboBoxTables);
            this.Controls.Add(this.lblPrevQuery);
            this.Controls.Add(this.txtBoxPrevQuery);
            this.Controls.Add(this.lblWHERE);
            this.Controls.Add(this.txtBoxQueryString);
            this.Controls.Add(this.btnEnter);
            this.Controls.Add(this.repViewerDiscOrders);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ReportGenerator";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Report Generator";
            this.Load += new System.EventHandler(this.ReportGenerator_Load);
            ((System.ComponentModel.ISupportInitialize)(this.DiscProcessorDataSet)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.discOrdersbindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.FrameDataBindingSource)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Microsoft.Reporting.WinForms.ReportViewer repViewerDiscOrders;
        private DiscProcessorDataSet DiscProcessorDataSet;
        private System.Windows.Forms.Button btnEnter;
        private System.Windows.Forms.TextBox txtBoxQueryString;
        private System.Windows.Forms.BindingSource discOrdersbindingSource;
        private DiscProcessorDataSetTableAdapters.DiscOrdersTableAdapter discOrdersTableAdapter;
        private System.Windows.Forms.Label lblWHERE;
        private System.Windows.Forms.TextBox txtBoxPrevQuery;
        private System.Windows.Forms.Label lblPrevQuery;
        private System.Windows.Forms.ComboBox cmboBoxTables;
        private System.Windows.Forms.Label lblTable;
        private System.Windows.Forms.BindingSource FrameDataBindingSource;
        private DiscProcessorDataSetTableAdapters.FrameDataTableAdapter FrameDataTableAdapter;
        private System.Windows.Forms.Label lblRecsReturned;
    }
}