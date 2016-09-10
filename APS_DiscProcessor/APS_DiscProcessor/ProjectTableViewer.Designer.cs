namespace APS_DiscProcessor
{
    partial class ProjectTableViewer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProjectTableViewer));
            this.dGV01 = new System.Windows.Forms.DataGridView();
            this.btnSave = new System.Windows.Forms.Button();
            this.chkBoxEditMode = new System.Windows.Forms.CheckBox();
            this.cBoxTables = new System.Windows.Forms.ComboBox();
            this.discProcessorDataSet = new APS_DiscProcessor.DiscProcessorDataSet();
            this.changeLogbindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.changeLogTableAdapter = new APS_DiscProcessor.DiscProcessorDataSetTableAdapters.ChangeLogTableAdapter();
            this.variablesbindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.variablesTableAdapter = new APS_DiscProcessor.DiscProcessorDataSetTableAdapters.VariablesTableAdapter();
            this.discTypesbindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.discTypesTableAdapter = new APS_DiscProcessor.DiscProcessorDataSetTableAdapters.DiscTypesTableAdapter();
            this.customerLabelsbindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.customerLabelsTableAdapter = new APS_DiscProcessor.DiscProcessorDataSetTableAdapters.CustomerLabelsTableAdapter();
            this.statusbindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.statusTableAdapter = new APS_DiscProcessor.DiscProcessorDataSetTableAdapters.StatusTableAdapter();
            this.errorsbindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.errorsTableAdapter = new APS_DiscProcessor.DiscProcessorDataSetTableAdapters.ErrorsTableAdapter();
            ((System.ComponentModel.ISupportInitialize)(this.dGV01)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.discProcessorDataSet)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.changeLogbindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.variablesbindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.discTypesbindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.customerLabelsbindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.statusbindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.errorsbindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // dGV01
            // 
            this.dGV01.AllowUserToDeleteRows = false;
            this.dGV01.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dGV01.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dGV01.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllHeaders;
            this.dGV01.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.dGV01.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dGV01.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dGV01.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.dGV01.Location = new System.Drawing.Point(12, 81);
            this.dGV01.MultiSelect = false;
            this.dGV01.Name = "dGV01";
            this.dGV01.Size = new System.Drawing.Size(1411, 522);
            this.dGV01.TabIndex = 4;
            this.dGV01.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dGV01_CellValueChanged);
            // 
            // btnSave
            // 
            this.btnSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btnSave.Font = new System.Drawing.Font("Arial", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSave.Location = new System.Drawing.Point(666, 624);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(102, 46);
            this.btnSave.TabIndex = 5;
            this.btnSave.Text = "&Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // chkBoxEditMode
            // 
            this.chkBoxEditMode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkBoxEditMode.AutoSize = true;
            this.chkBoxEditMode.ForeColor = System.Drawing.SystemColors.Control;
            this.chkBoxEditMode.Location = new System.Drawing.Point(12, 653);
            this.chkBoxEditMode.Name = "chkBoxEditMode";
            this.chkBoxEditMode.Size = new System.Drawing.Size(74, 17);
            this.chkBoxEditMode.TabIndex = 6;
            this.chkBoxEditMode.Text = "Edit Mode";
            this.chkBoxEditMode.UseVisualStyleBackColor = true;
            this.chkBoxEditMode.CheckedChanged += new System.EventHandler(this.chkBoxEditMode_CheckedChanged);
            // 
            // cBoxTables
            // 
            this.cBoxTables.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.cBoxTables.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cBoxTables.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cBoxTables.FormattingEnabled = true;
            this.cBoxTables.Location = new System.Drawing.Point(628, 34);
            this.cBoxTables.Name = "cBoxTables";
            this.cBoxTables.Size = new System.Drawing.Size(150, 24);
            this.cBoxTables.TabIndex = 7;
            this.cBoxTables.SelectedIndexChanged += new System.EventHandler(this.cBoxTables_SelectedIndexChanged);
            // 
            // discProcessorDataSet
            // 
            this.discProcessorDataSet.DataSetName = "DiscProcessorDataSet";
            this.discProcessorDataSet.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema;
            // 
            // changeLogbindingSource
            // 
            this.changeLogbindingSource.DataMember = "ChangeLog";
            this.changeLogbindingSource.DataSource = this.discProcessorDataSet;
            // 
            // changeLogTableAdapter
            // 
            this.changeLogTableAdapter.ClearBeforeFill = true;
            // 
            // variablesbindingSource
            // 
            this.variablesbindingSource.DataMember = "Variables";
            this.variablesbindingSource.DataSource = this.discProcessorDataSet;
            // 
            // variablesTableAdapter
            // 
            this.variablesTableAdapter.ClearBeforeFill = true;
            // 
            // discTypesbindingSource
            // 
            this.discTypesbindingSource.DataMember = "DiscTypes";
            this.discTypesbindingSource.DataSource = this.discProcessorDataSet;
            // 
            // discTypesTableAdapter
            // 
            this.discTypesTableAdapter.ClearBeforeFill = true;
            // 
            // customerLabelsbindingSource
            // 
            this.customerLabelsbindingSource.DataMember = "CustomerLabels";
            this.customerLabelsbindingSource.DataSource = this.discProcessorDataSet;
            // 
            // customerLabelsTableAdapter
            // 
            this.customerLabelsTableAdapter.ClearBeforeFill = true;
            // 
            // statusbindingSource
            // 
            this.statusbindingSource.DataMember = "Status";
            this.statusbindingSource.DataSource = this.discProcessorDataSet;
            // 
            // statusTableAdapter
            // 
            this.statusTableAdapter.ClearBeforeFill = true;
            // 
            // errorsbindingSource
            // 
            this.errorsbindingSource.DataMember = "Errors";
            this.errorsbindingSource.DataSource = this.discProcessorDataSet;
            // 
            // errorsTableAdapter
            // 
            this.errorsTableAdapter.ClearBeforeFill = true;
            // 
            // ProjectTableViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.ClientSize = new System.Drawing.Size(1435, 682);
            this.Controls.Add(this.cBoxTables);
            this.Controls.Add(this.chkBoxEditMode);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.dGV01);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProjectTableViewer";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Change Variables";
            this.Load += new System.EventHandler(this.ChangeVariables_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dGV01)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.discProcessorDataSet)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.changeLogbindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.variablesbindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.discTypesbindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.customerLabelsbindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.statusbindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.errorsbindingSource)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dGV01;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.CheckBox chkBoxEditMode;
        private System.Windows.Forms.ComboBox cBoxTables;
        private DiscProcessorDataSet discProcessorDataSet;
        private System.Windows.Forms.BindingSource changeLogbindingSource;
        private DiscProcessorDataSetTableAdapters.ChangeLogTableAdapter changeLogTableAdapter;
        private System.Windows.Forms.BindingSource variablesbindingSource;
        private DiscProcessorDataSetTableAdapters.VariablesTableAdapter variablesTableAdapter;
        private System.Windows.Forms.BindingSource discTypesbindingSource;
        private DiscProcessorDataSetTableAdapters.DiscTypesTableAdapter discTypesTableAdapter;
        private System.Windows.Forms.BindingSource customerLabelsbindingSource;
        private DiscProcessorDataSetTableAdapters.CustomerLabelsTableAdapter customerLabelsTableAdapter;
        private System.Windows.Forms.BindingSource statusbindingSource;
        private DiscProcessorDataSetTableAdapters.StatusTableAdapter statusTableAdapter;
        private System.Windows.Forms.BindingSource errorsbindingSource;
        private DiscProcessorDataSetTableAdapters.ErrorsTableAdapter errorsTableAdapter;
    }
}