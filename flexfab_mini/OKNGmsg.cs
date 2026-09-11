using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace flexfab
{
    public partial class OKNGmsg : Form
    {
        public bool IsConfirmed { get; private set; } = false;

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            this.Activate();         // 폼 활성화
            this.Focus();            // 포커스 설정
            this.BringToFront();     // 최상단에 위치
        }

        public OKNGmsg(string message)
        {
            InitializeComponent();
            label1.Text = message;

            // NG 버튼 숨김
            buttonNG.Visible = false;

            // OK 버튼 중앙 배치
            buttonOK.Left = (this.ClientSize.Width - buttonOK.Width) / 2;
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            IsConfirmed = true;
            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void buttonNG_Click(object sender, EventArgs e)
        {
            IsConfirmed = false;
            DialogResult = DialogResult.Cancel;
            this.Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                buttonOK.PerformClick();
                return true;
            }
            else if (keyData == Keys.Space)
            {
                buttonOK.PerformClick();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void buttonRetry_Click(object sender, EventArgs e)
        {

        }
    }
}
