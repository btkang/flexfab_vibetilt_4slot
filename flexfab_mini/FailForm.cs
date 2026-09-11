using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace flexfab
{
    public partial class FailForm : Form
    {

        public FailForm()
        {
            InitializeComponent();
            // 폼 설정
            this.Text = "Fail Form";
            this.Size = new System.Drawing.Size(552, 450); // 폼 크기
            this.StartPosition = FormStartPosition.CenterScreen; // 폼을 화면 중앙에 배치
            this.FormBorderStyle = FormBorderStyle.FixedDialog; // 폼 크기 고정
            this.MaximizeBox = false; // 최대화 불가
            this.MinimizeBox = false; // 최소화 불가
            this.BackColor = System.Drawing.Color.Red; // 빨간색 배경 설정

            // FAIL 텍스트 라벨 설정
            Label failLabel = new Label();
            failLabel.Text = "FAIL";
            failLabel.Font = new System.Drawing.Font("Arial", 135, System.Drawing.FontStyle.Bold); // 글꼴 크기 조절
            failLabel.ForeColor = System.Drawing.Color.White; // 글자 색은 흰색
            failLabel.AutoSize = true;
            failLabel.Location = new System.Drawing.Point(0, 0); // 왼쪽 상단 정렬

            // OK 버튼 설정
            button1 = new Button();
            button1.Text = "OK";
            button1.Size = new System.Drawing.Size(530, 100);
            button1.Location = new System.Drawing.Point(this.Width / 2 - button1.Width / 2, this.Height - button1.Height - 20); // 하단 중앙에 위치
            button1.Click += button1_Click;

            // 컨트롤 추가
            this.Controls.Add(failLabel);
            this.Controls.Add(button1);
        }

        public void SetResult(string result)
        {
            label_result.Text = result;  // label_result에 검사 결과 설정
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close(); // OK 버튼을 클릭하면 폼 닫기
        }
    }
}
