namespace Admin
{
    partial class AdminForm
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
            comboBox1 = new ComboBox();
            comboBox2 = new ComboBox();
            button1 = new Button();
            textBox1 = new TextBox();
            textBox2 = new TextBox();
            textBox3 = new TextBox();
            textBox4 = new TextBox();
            button2 = new Button();
            groupBox1 = new GroupBox();
            groupBox2 = new GroupBox();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            SuspendLayout();
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(17, 14);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(87, 23);
            comboBox1.TabIndex = 0;
            // 
            // comboBox2
            // 
            comboBox2.FormattingEnabled = true;
            comboBox2.Location = new Point(113, 13);
            comboBox2.Name = "comboBox2";
            comboBox2.Size = new Size(88, 23);
            comboBox2.TabIndex = 1;
            // 
            // button1
            // 
            button1.Location = new Point(218, 11);
            button1.Name = "button1";
            button1.Size = new Size(102, 36);
            button1.TabIndex = 2;
            button1.Text = "Set";
            button1.UseVisualStyleBackColor = true;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(17, 69);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(200, 23);
            textBox1.TabIndex = 3;
            textBox1.Text = "IP Address";
            // 
            // textBox2
            // 
            textBox2.Location = new Point(18, 6);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(243, 23);
            textBox2.TabIndex = 4;
            textBox2.Text = "제품명";
            // 
            // textBox3
            // 
            textBox3.Location = new Point(18, 58);
            textBox3.Name = "textBox3";
            textBox3.Size = new Size(243, 23);
            textBox3.TabIndex = 5;
            textBox3.Text = "검사자";
            // 
            // textBox4
            // 
            textBox4.Location = new Point(18, 112);
            textBox4.Name = "textBox4";
            textBox4.Size = new Size(243, 23);
            textBox4.TabIndex = 6;
            textBox4.Text = "Lot";
            // 
            // button2
            // 
            button2.Location = new Point(282, 45);
            button2.Name = "button2";
            button2.Size = new Size(102, 36);
            button2.TabIndex = 7;
            button2.Text = "Set";
            button2.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(textBox1);
            groupBox1.Controls.Add(button1);
            groupBox1.Controls.Add(comboBox2);
            groupBox1.Controls.Add(comboBox1);
            groupBox1.Location = new Point(8, 15);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(343, 116);
            groupBox1.TabIndex = 8;
            groupBox1.TabStop = false;
            groupBox1.Text = "groupBox1";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(button2);
            groupBox2.Controls.Add(textBox4);
            groupBox2.Controls.Add(textBox3);
            groupBox2.Controls.Add(textBox2);
            groupBox2.Location = new Point(7, 140);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(392, 150);
            groupBox2.TabIndex = 9;
            groupBox2.TabStop = false;
            groupBox2.Text = "groupBox2";
            // 
            // AdminForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1207, 700);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Name = "AdminForm";
            Text = "AdminForm";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private ComboBox comboBox1;
        private ComboBox comboBox2;
        private Button button1;
        private TextBox textBox1;
        private TextBox textBox2;
        private TextBox textBox3;
        private TextBox textBox4;
        private Button button2;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
    }
}