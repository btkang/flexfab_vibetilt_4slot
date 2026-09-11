namespace flexfab
{
    partial class OKNGmsg
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
            buttonOK = new Button();
            buttonNG = new Button();
            label1 = new Label();
            SuspendLayout();
            // 
            // buttonOK
            // 
            buttonOK.Location = new Point(82, 94);
            buttonOK.Name = "buttonOK";
            buttonOK.Size = new Size(195, 65);
            buttonOK.TabIndex = 0;
            buttonOK.Text = "OK";
            buttonOK.UseVisualStyleBackColor = true;
            buttonOK.Click += buttonOK_Click;
            // 
            // buttonNG
            // 
            buttonNG.Location = new Point(332, 94);
            buttonNG.Name = "buttonNG";
            buttonNG.Size = new Size(195, 65);
            buttonNG.TabIndex = 1;
            buttonNG.Text = "NG";
            buttonNG.UseVisualStyleBackColor = true;
            buttonNG.Click += buttonNG_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(82, 40);
            label1.Name = "label1";
            label1.Size = new Size(39, 15);
            label1.TabIndex = 2;
            label1.Text = "label1";
            // 
            // OKNGmsg
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(619, 214);
            Controls.Add(label1);
            Controls.Add(buttonNG);
            Controls.Add(buttonOK);
            Name = "OKNGmsg";
            Text = "OKNGmsg";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button buttonOK;
        private Button buttonNG;
        private Label label1;
    }
}