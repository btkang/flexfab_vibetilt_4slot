namespace flexfab
{
    partial class FailForm
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
            button1 = new Button();
            label_result = new Label();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(8, 258);
            button1.Name = "button1";
            button1.Size = new Size(527, 149);
            button1.TabIndex = 0;
            button1.Text = "OK";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // label_result
            // 
            label_result.AutoSize = true;
            label_result.Location = new Point(11, 227);
            label_result.Name = "label_result";
            label_result.Size = new Size(39, 15);
            label_result.TabIndex = 1;
            label_result.Text = "label1";
            // 
            // FailForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(536, 411);
            Controls.Add(label_result);
            Controls.Add(button1);
            Name = "FailForm";
            Text = "FailForm";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private Label label_result;
    }
}