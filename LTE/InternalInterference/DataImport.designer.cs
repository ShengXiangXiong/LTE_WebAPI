namespace LTE.InternalInterference
{
    partial class DataImport
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.txtFile = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnOpen = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.lstShow2 = new System.Windows.Forms.ListBox();
            this.btnImport = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.lstShow1 = new System.Windows.Forms.ListBox();
            this.button3 = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.selectTableName = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.dateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(398, 192);
            this.button1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(100, 29);
            this.button1.TabIndex = 0;
            this.button1.Text = "up";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(398, 281);
            this.button2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(100, 29);
            this.button2.TabIndex = 1;
            this.button2.Text = "down";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // txtFile
            // 
            this.txtFile.Location = new System.Drawing.Point(226, 31);
            this.txtFile.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtFile.Name = "txtFile";
            this.txtFile.Size = new System.Drawing.Size(163, 25);
            this.txtFile.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(136, 34);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(82, 15);
            this.label1.TabIndex = 3;
            this.label1.Text = "文件路径：";
            // 
            // btnOpen
            // 
            this.btnOpen.Location = new System.Drawing.Point(398, 29);
            this.btnOpen.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(100, 29);
            this.btnOpen.TabIndex = 4;
            this.btnOpen.Text = "浏览...";
            this.btnOpen.UseVisualStyleBackColor = true;
            this.btnOpen.Click += new System.EventHandler(this.btnOpen_Click);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(506, 28);
            this.button4.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(208, 30);
            this.button4.TabIndex = 5;
            this.button4.Text = "导入文件.txt|.xls|csv";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // lstShow2
            // 
            this.lstShow2.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lstShow2.FormattingEnabled = true;
            this.lstShow2.ItemHeight = 20;
            this.lstShow2.Items.AddRange(new object[] {
            "  "});
            this.lstShow2.Location = new System.Drawing.Point(544, 142);
            this.lstShow2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.lstShow2.Name = "lstShow2";
            this.lstShow2.Size = new System.Drawing.Size(309, 424);
            this.lstShow2.TabIndex = 7;
            this.lstShow2.SelectedIndexChanged += new System.EventHandler(this.lstShow2_SelectedIndexChanged);
            // 
            // btnImport
            // 
            this.btnImport.Location = new System.Drawing.Point(398, 459);
            this.btnImport.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(100, 29);
            this.btnImport.TabIndex = 8;
            this.btnImport.Text = "import";
            this.btnImport.UseVisualStyleBackColor = true;
            this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog1_FileOk_1);
            // 
            // lstShow1
            // 
            this.lstShow1.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lstShow1.FormattingEnabled = true;
            this.lstShow1.ItemHeight = 20;
            this.lstShow1.Items.AddRange(new object[] {
            "  "});
            this.lstShow1.Location = new System.Drawing.Point(42, 142);
            this.lstShow1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.lstShow1.Name = "lstShow1";
            this.lstShow1.Size = new System.Drawing.Size(316, 424);
            this.lstShow1.TabIndex = 6;
            this.lstShow1.SelectedIndexChanged += new System.EventHandler(this.lstShow1_SelectedIndexChanged);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(398, 360);
            this.button3.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(100, 29);
            this.button3.TabIndex = 9;
            this.button3.Text = "remove";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(39, 89);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(82, 15);
            this.label2.TabIndex = 10;
            this.label2.Text = "数据库表名";
            // 
            // selectTableName
            // 
            this.selectTableName.FormattingEnabled = true;
            this.selectTableName.Items.AddRange(new object[] {
            "RRCReqNum",
            "HandOverAtt",
            "ThroughPut"});
            this.selectTableName.Location = new System.Drawing.Point(129, 85);
            this.selectTableName.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.selectTableName.Name = "selectTableName";
            this.selectTableName.Size = new System.Drawing.Size(160, 23);
            this.selectTableName.TabIndex = 11;
            this.selectTableName.SelectedIndexChanged += new System.EventHandler(this.selectTableName_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(542, 91);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(37, 15);
            this.label3.TabIndex = 12;
            this.label3.Text = "日期";
            // 
            // dateTimePicker
            // 
            this.dateTimePicker.Location = new System.Drawing.Point(587, 85);
            this.dateTimePicker.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.dateTimePicker.Name = "dateTimePicker";
            this.dateTimePicker.Size = new System.Drawing.Size(265, 25);
            this.dateTimePicker.TabIndex = 13;
            // 
            // DataImport
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(918, 585);
            this.Controls.Add(this.dateTimePicker);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.selectTableName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.btnImport);
            this.Controls.Add(this.lstShow2);
            this.Controls.Add(this.lstShow1);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.btnOpen);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtFile);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MaximizeBox = false;
            this.Name = "DataImport";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "数据导入";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox txtFile;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnOpen;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.ListBox lstShow2;
        private System.Windows.Forms.Button btnImport;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.ListBox lstShow1;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox selectTableName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.DateTimePicker dateTimePicker;
    }
}

