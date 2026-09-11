using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace msgbox
{
    public partial class MsgBoxForm : Form
    {
        public MsgBoxForm(string message)
        {
            InitializeComponent();
            label_msg.Text = message; // 'message' 파라미터 값 삽입
        }


        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Yes;
            this.Close();  // 폼 닫기
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.No;
            this.Close();  // 폼 닫기
        }
    }
}
