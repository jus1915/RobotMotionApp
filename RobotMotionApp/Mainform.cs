using System;
using System.Drawing;
using System.Windows.Forms;
using RobotMotionApp.Core;
using RobotMotionApp.Models;

namespace RobotMotionApp.UI
{
    public class MainForm : Form
    {
        private RobotController _robot;
        private SequenceRunner _runner;
        private MotionSequence _sequence;

        // Controls
        private GroupBox grpConnection, grpParams, grpSequence, grpRepeat, grpLog;
        private NumericUpDown nudIrq, nudAxisNo;
        private Button btnConnect, btnDisconnect, btnServoOn, btnServoOff;
        private Label lblStatus;
        private NumericUpDown nudMaxVel, nudMaxAccel;
        private Button btnApplyParams;
        private ListBox lstSteps;
        private Button btnAddMove, btnAddWait, btnRemoveStep, btnClearSteps;
        private NumericUpDown nudRepeat;
        private CheckBox chkInfinite;
        private Button btnStart, btnStop;
        private ProgressBar pbRepeat;
        private Label lblRepeatInfo;
        private RichTextBox rtbLog;
        private Button btnClearLog;

        public MainForm()
        {
            InitializeComponent();
            InitializeRobot();
            BuildDefaultSequence();
        }

        private void InitializeRobot()
        {
            _robot = new RobotController();
            _sequence = new MotionSequence { Name = "MySequence" };
            _runner = new SequenceRunner(_robot);

            _runner.OnLog += delegate (string msg)
            {
                SafeInvoke(delegate { AppendLog(msg); });
            };
            _runner.OnStateChanged += delegate (RunnerState state)
            {
                SafeInvoke(delegate { UpdateRunButtons(state); });
            };
            _runner.OnProgress += delegate (int cur, int total)
            {
                SafeInvoke(delegate
                {
                    int t = (total == 0) ? pbRepeat.Maximum : total;
                    pbRepeat.Maximum = t;
                    pbRepeat.Value = Math.Min(cur, t);
                    lblRepeatInfo.Text = string.Format("{0} / {1}", cur, total == 0 ? "∞" : total.ToString());
                });
            };
        }

        private void BuildDefaultSequence()
        {
            // 기본 예시 시퀀스
            _sequence.AddStep(new SequenceStep { Type = StepType.ServoOn, AxisNo = 0 });
            _sequence.AddStep(new SequenceStep { Type = StepType.SetMaxVel, AxisNo = 0, Value = 1000 });
            _sequence.AddStep(new SequenceStep { Type = StepType.SetMaxAccel, AxisNo = 0, Value = 500 });
            _sequence.AddStep(new SequenceStep
            {
                Type = StepType.MovePos,
                AxisNo = 0,
                Pos = 10000,
                Vel = 1000,
                Accel = 500,
                Decel = 500,
                Async = false
            });
            _sequence.AddStep(new SequenceStep { Type = StepType.Wait, Value = 500 });
            _sequence.AddStep(new SequenceStep
            {
                Type = StepType.MovePos,
                AxisNo = 0,
                Pos = 0,
                Vel = 1000,
                Accel = 500,
                Decel = 500,
                Async = false
            });
            RefreshStepList();
        }

        #region UI Build
        private void InitializeComponent()
        {
            this.Text = "Robot Motion Controller — AXL (Ajin)";
            this.Size = new Size(980, 730);
            this.MinimumSize = new Size(920, 690);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Malgun Gothic", 9f);

            // ── Connection ──────────────────────────────────────────
            grpConnection = MakeGroup("1. AXL 초기화 & 연결", 8, 8, 470, 92);

            grpConnection.Controls.Add(MakeLabel("IRQ:", 10, 26));
            nudIrq = new NumericUpDown { Minimum = -1, Maximum = 15, Value = 7, Location = new Point(45, 23), Width = 55 };
            grpConnection.Controls.Add(nudIrq);

            grpConnection.Controls.Add(MakeLabel("기본 축:", 110, 26));
            nudAxisNo = new NumericUpDown { Minimum = 0, Maximum = 31, Value = 0, Location = new Point(160, 23), Width = 55 };
            grpConnection.Controls.Add(nudAxisNo);

            btnConnect = MakeButton("AxlOpen", 225, 21, 80, 28, Color.FromArgb(0, 120, 215), Color.White);
            btnConnect.Click += BtnConnect_Click;
            grpConnection.Controls.Add(btnConnect);

            btnDisconnect = MakeButton("AxlClose", 312, 21, 80, 28);
            btnDisconnect.Enabled = false;
            btnDisconnect.Click += BtnDisconnect_Click;
            grpConnection.Controls.Add(btnDisconnect);

            lblStatus = new Label
            {
                Text = "● 미초기화",
                ForeColor = Color.Red,
                Location = new Point(402, 26),
                AutoSize = true,
                Font = new Font("Malgun Gothic", 9f, FontStyle.Bold)
            };
            grpConnection.Controls.Add(lblStatus);

            btnServoOn = MakeButton("Servo ON", 10, 58, 85, 26, Color.FromArgb(50, 160, 50), Color.White);
            btnServoOn.Enabled = false;
            btnServoOn.Click += delegate { SafeRobotAction(delegate { _robot.ServoOn((int)nudAxisNo.Value); }, "Servo ON"); };
            grpConnection.Controls.Add(btnServoOn);

            btnServoOff = MakeButton("Servo OFF", 103, 58, 85, 26, Color.FromArgb(180, 60, 60), Color.White);
            btnServoOff.Enabled = false;
            btnServoOff.Click += delegate { SafeRobotAction(delegate { _robot.ServoOff((int)nudAxisNo.Value); }, "Servo OFF"); };
            grpConnection.Controls.Add(btnServoOff);

            // ── Params ───────────────────────────────────────────────
            grpParams = MakeGroup("2. 모션 파라미터", 486, 8, 474, 92);

            grpParams.Controls.Add(MakeLabel("MaxVel (unit/s):", 10, 26));
            nudMaxVel = new NumericUpDown { Minimum = 1, Maximum = 999999, Value = 1000, Location = new Point(125, 23), Width = 90 };
            grpParams.Controls.Add(nudMaxVel);

            grpParams.Controls.Add(MakeLabel("MaxAccel:", 225, 26));
            nudMaxAccel = new NumericUpDown { Minimum = 1, Maximum = 999999, Value = 500, Location = new Point(295, 23), Width = 90 };
            grpParams.Controls.Add(nudMaxAccel);

            btnApplyParams = MakeButton("스텝 추가", 395, 21, 70, 28, Color.FromArgb(0, 120, 215), Color.White);
            btnApplyParams.Click += BtnApplyParams_Click;
            grpParams.Controls.Add(btnApplyParams);

            grpParams.Controls.Add(new Label
            {
                Text = "※ 클릭 시 시퀀스 맨 앞에 SetMaxVel / SetMaxAccel 스텝을 삽입합니다.",
                Location = new Point(10, 60),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Malgun Gothic", 8f)
            });

            // ── Sequence ─────────────────────────────────────────────
            grpSequence = MakeGroup("3. 시퀀스 스텝", 8, 108, 668, 388);

            lstSteps = new ListBox
            {
                Location = new Point(10, 22),
                Size = new Size(528, 334),
                Font = new Font("Courier New", 8.5f),
                ScrollAlwaysVisible = true
            };
            grpSequence.Controls.Add(lstSteps);

            int bx = 548, by = 22, bw = 110, bh = 30, bg = 8;
            btnAddMove = MakeButton("+ MovePos", bx, by, bw, bh);
            btnAddMove.Click += BtnAddMove_Click;
            grpSequence.Controls.Add(btnAddMove);

            btnAddWait = MakeButton("+ Wait", bx, by += bh + bg, bw, bh);
            btnAddWait.Click += BtnAddWait_Click;
            grpSequence.Controls.Add(btnAddWait);

            btnRemoveStep = MakeButton("선택 삭제", bx, by += bh + bg * 3, bw, bh, Color.FromArgb(180, 60, 60), Color.White);
            btnRemoveStep.Click += BtnRemoveStep_Click;
            grpSequence.Controls.Add(btnRemoveStep);

            btnClearSteps = MakeButton("전체 초기화", bx, by += bh + bg, bw, bh);
            btnClearSteps.Click += BtnClearSteps_Click;
            grpSequence.Controls.Add(btnClearSteps);

            // ── Repeat & Run ──────────────────────────────────────────
            grpRepeat = MakeGroup("4. 반복 및 실행", 8, 504, 668, 90);

            grpRepeat.Controls.Add(MakeLabel("반복 횟수:", 10, 30));
            nudRepeat = new NumericUpDown { Minimum = 1, Maximum = 99999, Value = 5, Location = new Point(85, 27), Width = 70 };
            nudRepeat.ValueChanged += delegate { _sequence.RepeatCount = (int)nudRepeat.Value; };
            grpRepeat.Controls.Add(nudRepeat);

            chkInfinite = new CheckBox { Text = "무한반복", Location = new Point(165, 30), AutoSize = true };
            chkInfinite.CheckedChanged += ChkInfinite_CheckedChanged;
            grpRepeat.Controls.Add(chkInfinite);

            btnStart = MakeButton("▶ 시작", 275, 23, 90, 34, Color.FromArgb(50, 160, 50), Color.White);
            btnStart.Font = new Font("Malgun Gothic", 10f, FontStyle.Bold);
            btnStart.Click += BtnStart_Click;
            grpRepeat.Controls.Add(btnStart);

            btnStop = MakeButton("⏹ 정지", 373, 23, 90, 34, Color.FromArgb(180, 60, 60), Color.White);
            btnStop.Font = new Font("Malgun Gothic", 10f, FontStyle.Bold);
            btnStop.Enabled = false;
            btnStop.Click += delegate { _runner.Stop(); };
            grpRepeat.Controls.Add(btnStop);

            pbRepeat = new ProgressBar { Location = new Point(10, 64), Size = new Size(390, 18) };
            grpRepeat.Controls.Add(pbRepeat);

            lblRepeatInfo = new Label { Text = "0 / 0", Location = new Point(406, 66), AutoSize = true };
            grpRepeat.Controls.Add(lblRepeatInfo);

            // ── Log ───────────────────────────────────────────────────
            grpLog = MakeGroup("5. 실행 로그", 684, 108, 274, 486);

            rtbLog = new RichTextBox
            {
                Location = new Point(8, 22),
                Size = new Size(252, 424),
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.LimeGreen,
                Font = new Font("Courier New", 8f),
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            grpLog.Controls.Add(rtbLog);

            btnClearLog = MakeButton("로그 지우기", 8, 450, 100, 26);
            btnClearLog.Click += delegate { rtbLog.Clear(); };
            grpLog.Controls.Add(btnClearLog);

            this.Controls.AddRange(new Control[] {
                grpConnection, grpParams, grpSequence, grpRepeat, grpLog
            });

            this.FormClosing += delegate
            {
                _runner.Stop();
                _robot.Dispose();
            };
        }
        #endregion

        #region Event Handlers

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                bool ok = _robot.Open((int)nudIrq.Value);
                if (ok)
                {
                    lblStatus.Text = "● 초기화됨";
                    lblStatus.ForeColor = Color.LimeGreen;
                    btnConnect.Enabled = false;
                    btnDisconnect.Enabled = true;
                    btnServoOn.Enabled = true;
                    btnServoOff.Enabled = true;
                    int axes = _robot.GetAxisCount();
                    AppendLog(string.Format("[초기화] AxlOpen 성공 — 감지 축 수: {0}", axes));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "초기화 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDisconnect_Click(object sender, EventArgs e)
        {
            _robot.Disconnect();
            lblStatus.Text = "● 미초기화";
            lblStatus.ForeColor = Color.Red;
            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            btnServoOn.Enabled = false;
            btnServoOff.Enabled = false;
            AppendLog("[초기화] AxlClose 완료");
        }

        private void BtnApplyParams_Click(object sender, EventArgs e)
        {
            int axNo = (int)nudAxisNo.Value;
            _sequence.Steps.Insert(0, new SequenceStep
            { Type = StepType.SetMaxAccel, AxisNo = axNo, Value = (double)nudMaxAccel.Value });
            _sequence.Steps.Insert(0, new SequenceStep
            { Type = StepType.SetMaxVel, AxisNo = axNo, Value = (double)nudMaxVel.Value });
            RenumberSteps();
            RefreshStepList();
            AppendLog(string.Format("[파라미터] Vel={0}, Accel={1} 스텝 삽입됨", nudMaxVel.Value, nudMaxAccel.Value));
        }

        private void BtnAddMove_Click(object sender, EventArgs e)
        {
            using (StepEditDialog dlg = new StepEditDialog(StepType.MovePos))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _sequence.AddStep(dlg.Result);
                    RefreshStepList();
                }
            }
        }

        private void BtnAddWait_Click(object sender, EventArgs e)
        {
            using (StepEditDialog dlg = new StepEditDialog(StepType.Wait))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _sequence.AddStep(dlg.Result);
                    RefreshStepList();
                }
            }
        }

        private void BtnRemoveStep_Click(object sender, EventArgs e)
        {
            int idx = lstSteps.SelectedIndex;
            if (idx < 0) return;
            _sequence.Steps.RemoveAt(idx);
            RenumberSteps();
            RefreshStepList();
        }

        private void BtnClearSteps_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("모든 스텝을 삭제할까요?", "확인",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _sequence.Steps.Clear();
                RefreshStepList();
            }
        }

        private void ChkInfinite_CheckedChanged(object sender, EventArgs e)
        {
            nudRepeat.Enabled = !chkInfinite.Checked;
            _sequence.RepeatCount = chkInfinite.Checked ? 0 : (int)nudRepeat.Value;
        }

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            if (!_robot.IsConnected)
            {
                MessageBox.Show("AXL이 초기화되지 않았습니다. AxlOpen을 먼저 실행하세요.",
                    "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_sequence.Steps.Count == 0)
            {
                MessageBox.Show("시퀀스 스텝이 없습니다.", "경고",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnStart.Enabled = false;
            btnStop.Enabled = true;
            pbRepeat.Value = 0;
            pbRepeat.Maximum = _sequence.RepeatCount == 0 ? 100 : _sequence.RepeatCount;

            await _runner.StartAsync(_sequence);
        }

        #endregion

        #region Helpers

        private void UpdateRunButtons(RunnerState state)
        {
            bool running = (state == RunnerState.Running);
            btnStart.Enabled = !running;
            btnStop.Enabled = running;
        }

        private void RefreshStepList()
        {
            lstSteps.Items.Clear();
            foreach (SequenceStep step in _sequence.Steps)
                lstSteps.Items.Add(step.ToString());
        }

        private void RenumberSteps()
        {
            for (int i = 0; i < _sequence.Steps.Count; i++)
                _sequence.Steps[i].StepIndex = i;
        }

        private void AppendLog(string msg)
        {
            if (rtbLog.Lines.Length > 2000) rtbLog.Clear();
            rtbLog.AppendText(msg + "\n");
            rtbLog.ScrollToCaret();
        }

        private void SafeInvoke(Action action)
        {
            if (InvokeRequired) Invoke(action);
            else action();
        }

        private void SafeRobotAction(Action action, string name)
        {
            try { action(); AppendLog("[명령] " + name + " 완료"); }
            catch (Exception ex) { AppendLog("[오류] " + name + ": " + ex.Message); }
        }

        private static GroupBox MakeGroup(string text, int x, int y, int w, int h)
        {
            return new GroupBox { Text = text, Location = new Point(x, y), Size = new Size(w, h) };
        }

        private static Label MakeLabel(string text, int x, int y)
        {
            return new Label { Text = text, Location = new Point(x, y), AutoSize = true };
        }

        private static Button MakeButton(string text, int x, int y, int w, int h,
            Color? back = null, Color? fore = null)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat
            };
            if (back.HasValue) { btn.BackColor = back.Value; btn.FlatAppearance.BorderColor = back.Value; }
            if (fore.HasValue) btn.ForeColor = fore.Value;
            return btn;
        }

        #endregion
    }
}