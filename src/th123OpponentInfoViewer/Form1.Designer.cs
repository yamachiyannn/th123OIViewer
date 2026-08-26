namespace th123OpponentInfoViewer
{
    partial class Form1
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.txtOutput = new System.Windows.Forms.TextBox();
            this.txtIpInput = new System.Windows.Forms.TextBox();
            this.btnEscEnded = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // txtOutput
            // 
            this.txtOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutput.Location = new System.Drawing.Point(0, 0);
            this.txtOutput.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);
            this.txtOutput.Multiline = true;
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.ReadOnly = true;
            this.txtOutput.Size = new System.Drawing.Size(437, 575);
            this.txtOutput.TabIndex = 0;
            // 
            // txtIpInput
            // 
            this.txtIpInput.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtIpInput.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.txtIpInput.Location = new System.Drawing.Point(14, 586);
            this.txtIpInput.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.txtIpInput.Name = "txtIpInput";
            this.txtIpInput.Size = new System.Drawing.Size(322, 25);
            this.txtIpInput.TabIndex = 1;
            this.txtIpInput.TextChanged += new System.EventHandler(this.txtIpInput_TextChanged);
            // 
            // btnEscEnded
            // 
            this.btnEscEnded.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnEscEnded.Location = new System.Drawing.Point(350, 587);
            this.btnEscEnded.Name = "btnEscEnded";
            this.btnEscEnded.Size = new System.Drawing.Size(75, 23);
            this.btnEscEnded.TabIndex = 2;
            this.btnEscEnded.Text = "Esc";
            this.btnEscEnded.UseVisualStyleBackColor = true;
            this.btnEscEnded.Click += new System.EventHandler(this.btnEscEnded_Click_1);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(437, 624);
            this.Controls.Add(this.btnEscEnded);
            this.Controls.Add(this.txtIpInput);
            this.Controls.Add(this.txtOutput);
            this.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.TextBox txtOutput;
        private System.Windows.Forms.Button btnEscEnded;
        private System.Windows.Forms.TextBox txtIpInput;
    }
}

