using System;
using System.Drawing;
using System.Windows.Forms;
using RobotMotionApp.Models;

namespace RobotMotionApp.UI
{
    public class StepEditDialog : Form
    {
        public SequenceStep Result { get; private set; }
        private readonly StepType _type;

        private NumericUpDown nudAxisNo, nudPos, nudVel, nudAccel, nudDecel;
        private NumericUpDown nudWaitMs;
        private CheckBox chkAsync;

        public StepEditDialog(StepType type)
        {
            _type = type;
            BuildUI();
        }

        private void BuildUI()
        {
            string title;
            switch (_type)
            {
                case StepType.MovePos: title = "MovePos 스텝 추가"; break;
                case StepType.Wait: title = "Wait 스텝 추가"; break;
                default: title = "스텝 추가"; break;
            }
            this.Text = title;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Malgun Gothic", 9f);

            int y = 16;

            if (_type == StepType.MovePos)
            {
                // Axis No
                this.Controls.Add(MakeLabel("축 번호 (AxisNo):", 16, y + 4));
                nudAxisNo = new NumericUpDown { Minimum = 0, Maximum = 31, Value = 0, Location = new Point(160, y), Width = 70 };
                this.Controls.Add(nudAxisNo);
                y += 36;

                // Position
                this.Controls.Add(MakeLabel("목표 위치 (Pos):", 16, y + 4));
                nudPos = new NumericUpDown { Minimum = -9999999, Maximum = 9999999, DecimalPlaces = 2, Value = 0, Location = new Point(160, y), Width = 100 };
                this.Controls.Add(nudPos);
                y += 36;

                // Velocity
                this.Controls.Add(MakeLabel("속도 Vel (unit/s):", 16, y + 4));
                nudVel = new NumericUpDown { Minimum = 1, Maximum = 999999, Value = 1000, Location = new Point(160, y), Width = 100 };
                this.Controls.Add(nudVel);
                y += 36;

                // Accel
                this.Controls.Add(MakeLabel("가속 Accel:", 16, y + 4));
                nudAccel = new NumericUpDown { Minimum = 1, Maximum = 999999, Value = 500, Location = new Point(160, y), Width = 100 };
                this.Controls.Add(nudAccel);
                y += 36;

                // Decel
                this.Controls.Add(MakeLabel("감속 Decel:", 16, y + 4));
                nudDecel = new NumericUpDown { Minimum = 1, Maximum = 999999, Value = 500, Location = new Point(160, y), Width = 100 };
                this.Controls.Add(nudDecel);
                y += 36;

                // Async
                chkAsync = new CheckBox { Text = "비동기 이동 (AxmMoveStartPos)", Location = new Point(16, y), AutoSize = true };
                this.Controls.Add(chkAsync);
                y += 30;
            }
            else if (_type == StepType.Wait)
            {
                this.Controls.Add(MakeLabel("대기 시간 (ms):", 16, y + 4));
                nudWaitMs = new NumericUpDown { Minimum = 0, Maximum = 60000, Value = 1000, Location = new Point(140, y), Width = 100 };
                this.Controls.Add(nudWaitMs);
                y += 40;
            }

            // OK / Cancel
            Button btnOk = new Button
            {
                Text = "추가",
                DialogResult = DialogResult.Cancel,  // will change on click
                Location = new Point(80, y + 10),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnOk.Click += BtnOk_Click;
            this.Controls.Add(btnOk);

            Button btnCancel = new Button
            {
                Text = "취소",
                DialogResult = DialogResult.Cancel,
                Location = new Point(170, y + 10),
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.Flat
            };
            this.Controls.Add(btnCancel);

            this.CancelButton = btnCancel;
            this.Width = 300;
            this.Height = y + 100;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            Result = new SequenceStep { Type = _type };

            if (_type == StepType.MovePos)
            {
                Result.AxisNo = (int)nudAxisNo.Value;
                Result.Pos = (double)nudPos.Value;
                Result.Vel = (double)nudVel.Value;
                Result.Accel = (double)nudAccel.Value;
                Result.Decel = (double)nudDecel.Value;
                Result.Async = chkAsync.Checked;
                Result.WaitDone = true;
            }
            else if (_type == StepType.Wait)
            {
                Result.Value = (double)nudWaitMs.Value;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private static Label MakeLabel(string text, int x, int y)
        {
            return new Label { Text = text, Location = new Point(x, y), AutoSize = true };
        }
    }
}