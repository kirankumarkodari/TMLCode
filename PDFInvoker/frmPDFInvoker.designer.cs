namespace PDFInvoker
{
    partial class frmPDFInvoker
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.rtbEventLog = new System.Windows.Forms.RichTextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnChoosePath = new System.Windows.Forms.Button();
            this.tbSelectedPath = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tmrEditNotify = new System.Windows.Forms.Timer(this.components);
            this.odlgSelectFile = new System.Windows.Forms.OpenFileDialog();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.rtbEventLog, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(890, 318);
            this.tableLayoutPanel1.TabIndex = 3;
            // 
            // rtbEventLog
            // 
            this.rtbEventLog.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.rtbEventLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbEventLog.Location = new System.Drawing.Point(3, 56);
            this.rtbEventLog.Name = "rtbEventLog";
            this.rtbEventLog.ReadOnly = true;
            this.rtbEventLog.Size = new System.Drawing.Size(884, 259);
            this.rtbEventLog.TabIndex = 4;
            this.rtbEventLog.Text = "";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnChoosePath);
            this.panel1.Controls.Add(this.tbSelectedPath);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(884, 47);
            this.panel1.TabIndex = 3;
            // 
            // btnChoosePath
            // 
            this.btnChoosePath.Location = new System.Drawing.Point(517, 20);
            this.btnChoosePath.Name = "btnChoosePath";
            this.btnChoosePath.Size = new System.Drawing.Size(75, 23);
            this.btnChoosePath.TabIndex = 2;
            this.btnChoosePath.Text = "Choose";
            this.btnChoosePath.UseVisualStyleBackColor = true;
            this.btnChoosePath.Click += new System.EventHandler(this.btnChoosePath_Click);
            // 
            // tbSelectedPath
            // 
            this.tbSelectedPath.Location = new System.Drawing.Point(109, 20);
            this.tbSelectedPath.Name = "tbSelectedPath";
            this.tbSelectedPath.Size = new System.Drawing.Size(401, 20);
            this.tbSelectedPath.TabIndex = 1;
            this.tbSelectedPath.Text = "C:\\inetpub\\wwwroot\\TML\\UploadedFiles";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(90, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "PDF Source Path";
            // 
            // tmrEditNotify
            // 
            this.tmrEditNotify.Enabled = true;
            this.tmrEditNotify.Tick += new System.EventHandler(this.tmrEditNotify_Tick);
            // 
            // odlgSelectFile
            // 
            this.odlgSelectFile.DefaultExt = "*.txt";
            this.odlgSelectFile.FileName = "ProcessFiles.txt";
            this.odlgSelectFile.Title = "Choose Process File ";
            // 
            // frmPDFInvoker
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(890, 318);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "frmPDFInvoker";
            this.Text = "PDF To Excel Application Invoker - Ver 1.2";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmPDFInvoker_FormClosing);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnChoosePath;
        private System.Windows.Forms.TextBox tbSelectedPath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox rtbEventLog;
        private System.Windows.Forms.Timer tmrEditNotify;
        private System.Windows.Forms.OpenFileDialog odlgSelectFile;

    }
}

