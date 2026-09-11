namespace flexfab
{
    partial class SerialMac
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
            button_OK = new Button();
            textBox_Serial = new TextBox();
            textBox_Mac = new TextBox();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            SuspendLayout();
            var bigFont = new Font("맑은 고딕", 16);
            var midFont = new Font("맑은 고딕", 12);
            //
            // button_OK
            //
            button_OK.Location = new Point(10, 310);
            button_OK.Name = "button_OK";
            button_OK.Size = new Size(430, 75);
            button_OK.TabIndex = 0;
            button_OK.Text = "OK";
            button_OK.Font = bigFont;
            button_OK.UseVisualStyleBackColor = true;
            button_OK.Click += button_OK_Click;
            //
            // textBox_Serial
            //
            textBox_Serial.Location = new Point(100, 55);
            textBox_Serial.Name = "textBox_Serial";
            textBox_Serial.Size = new Size(480, 38);
            textBox_Serial.Font = bigFont;
            textBox_Serial.TabIndex = 1;
            //
            // textBox_Mac
            //
            textBox_Mac.Location = new Point(100, 155);
            textBox_Mac.Name = "textBox_Mac";
            textBox_Mac.Size = new Size(480, 38);
            textBox_Mac.Font = bigFont;
            textBox_Mac.TabIndex = 2;
            //
            // label1
            //
            label1.AutoSize = true;
            label1.Location = new Point(20, 60);
            label1.Name = "label1";
            label1.Font = bigFont;
            label1.TabIndex = 3;
            label1.Text = "Serial";
            //
            // label2
            //
            label2.AutoSize = true;
            label2.Location = new Point(20, 160);
            label2.Name = "label2";
            label2.Font = bigFont;
            label2.TabIndex = 4;
            label2.Text = "Mac";
            //
            // label3
            //
            label3.AutoSize = true;
            label3.Location = new Point(280, 280);
            label3.Name = "label3";
            label3.Font = midFont;
            label3.TabIndex = 5;
            label3.Text = "취소해야 할 경우 OK 클릭 후 Stop 버튼";
            //
            // SerialMac
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(600, 400);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(textBox_Mac);
            Controls.Add(textBox_Serial);
            Controls.Add(button_OK);
            Name = "SerialMac";
            Text = "SerialMac";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button_OK;
        private TextBox textBox_Serial;
        private TextBox textBox_Mac;
        private Label label1;
        private Label label2;
        private Label label3;
    }
}