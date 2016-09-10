namespace APS_DiscProcessor
{
    partial class ICDorMEGResubmit
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
            this.txtResubmit = new System.Windows.Forms.TextBox();
            this.btnResubmit = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.cmboBoxFrame = new System.Windows.Forms.ComboBox();
            this.btnQuery = new System.Windows.Forms.Button();
            this.chkBoxAllFrames = new System.Windows.Forms.CheckBox();
            this.btnClear = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtResubmit
            // 
            this.txtResubmit.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtResubmit.Location = new System.Drawing.Point(168, 70);
            this.txtResubmit.Name = "txtResubmit";
            this.txtResubmit.Size = new System.Drawing.Size(100, 21);
            this.txtResubmit.TabIndex = 0;
            this.txtResubmit.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // btnResubmit
            // 
            this.btnResubmit.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnResubmit.Location = new System.Drawing.Point(185, 195);
            this.btnResubmit.Name = "btnResubmit";
            this.btnResubmit.Size = new System.Drawing.Size(75, 23);
            this.btnResubmit.TabIndex = 1;
            this.btnResubmit.Text = "Resubmit";
            this.btnResubmit.UseVisualStyleBackColor = true;
            this.btnResubmit.Click += new System.EventHandler(this.btnResubmit_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(165, 37);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(131, 15);
            this.label1.TabIndex = 2;
            this.label1.Text = "Enter a prod # or ref #: ";
            // 
            // cmboBoxFrame
            // 
            this.cmboBoxFrame.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmboBoxFrame.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmboBoxFrame.FormattingEnabled = true;
            this.cmboBoxFrame.Location = new System.Drawing.Point(168, 166);
            this.cmboBoxFrame.Name = "cmboBoxFrame";
            this.cmboBoxFrame.Size = new System.Drawing.Size(121, 23);
            this.cmboBoxFrame.TabIndex = 3;
            // 
            // btnQuery
            // 
            this.btnQuery.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnQuery.Location = new System.Drawing.Point(180, 96);
            this.btnQuery.Name = "btnQuery";
            this.btnQuery.Size = new System.Drawing.Size(75, 23);
            this.btnQuery.TabIndex = 4;
            this.btnQuery.Text = "Query";
            this.btnQuery.UseVisualStyleBackColor = true;
            this.btnQuery.Click += new System.EventHandler(this.btnQuery_Click);
            // 
            // chkBoxAllFrames
            // 
            this.chkBoxAllFrames.AutoSize = true;
            this.chkBoxAllFrames.ForeColor = System.Drawing.Color.White;
            this.chkBoxAllFrames.Location = new System.Drawing.Point(295, 169);
            this.chkBoxAllFrames.Name = "chkBoxAllFrames";
            this.chkBoxAllFrames.Size = new System.Drawing.Size(74, 17);
            this.chkBoxAllFrames.TabIndex = 5;
            this.chkBoxAllFrames.Text = "All Frames";
            this.chkBoxAllFrames.UseVisualStyleBackColor = true;
            this.chkBoxAllFrames.CheckedChanged += new System.EventHandler(this.chkBoxAllFrames_CheckedChanged);
            // 
            // btnClear
            // 
            this.btnClear.Font = new System.Drawing.Font("Arial", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnClear.Location = new System.Drawing.Point(12, 12);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(75, 216);
            this.btnClear.TabIndex = 6;
            this.btnClear.Text = "Clear";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // ICDorMEGResubmit
            // 
            this.AcceptButton = this.btnQuery;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.ClientSize = new System.Drawing.Size(393, 240);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.chkBoxAllFrames);
            this.Controls.Add(this.btnQuery);
            this.Controls.Add(this.cmboBoxFrame);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnResubmit);
            this.Controls.Add(this.txtResubmit);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(409, 279);
            this.MinimumSize = new System.Drawing.Size(409, 279);
            this.Name = "ICDorMEGResubmit";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "ICD or MEG Resubmit";
            this.Load += new System.EventHandler(this.Resubmit_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtResubmit;
        private System.Windows.Forms.Button btnResubmit;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cmboBoxFrame;
        private System.Windows.Forms.Button btnQuery;
        private System.Windows.Forms.CheckBox chkBoxAllFrames;
        private System.Windows.Forms.Button btnClear;
    }
}