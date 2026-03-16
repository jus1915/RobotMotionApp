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
        private int _axisCount = 0;

        // ── 상단 컨트롤 ───────────────────────────────────────────
        private NumericUpDown nudIrq;
        private Button btnOpen, btnClose;
        private Label lblStatus, lblAxisCount;

        // 서보 제어
        private NumericUpDown nudServoAxis;
        private Button btnServoOn, btnServoOff;
        private Button btnServoOnAll, btnServoOffAll;
        private Button btnSetZero;

        // 축 테이블
        private DataGridView dgvAxis;

        // 시퀀스
        private ListBox lstSteps;

        // 반복 & 실행
        private NumericUpDown nudRepeat;
        private CheckBox chkInfinite;
        private Button btnStart, btnStop;
        private ProgressBar pbRepeat;
        private Label lblRepeatInfo;

        // 로그
        private RichTextBox rtbLog;

        // 타이머
        private System.Windows.Forms.Timer tmrStatus;

        // ────────────────────────────────────────────────────────
        public MainForm()
        {
            BuildUI();
            InitRobot();
            BuildDefaultSequence();
        }

        private void InitRobot()
        {
            _robot = new RobotController();
            _sequence = new MotionSequence { Name = "MySequence" };
            _runner = new SequenceRunner(_robot);

            _runner.OnLog += m => SafeInvoke(() => Log(m));
            _runner.OnStateChanged += s => SafeInvoke(() => SetRunning(s == RunnerState.Running));
            _runner.OnProgress += (cur, total) => SafeInvoke(() =>
            {
                int t = total == 0 ? Math.Max(pbRepeat.Maximum, 1) : total;
                pbRepeat.Maximum = t;
                pbRepeat.Value = Math.Min(cur, t);
                lblRepeatInfo.Text = $"{cur} / {(total == 0 ? "∞" : total.ToString())}";
            });

            tmrStatus = new System.Windows.Forms.Timer { Interval = 150 };
            tmrStatus.Tick += OnStatusTick;
        }

        private void BuildDefaultSequence()
        {
            double vel = 5.0, acc = 10.0;
            _sequence.AddStep(new SequenceStep { Type = StepType.ServoOn, AxisNo = 0 });
            _sequence.AddStep(new SequenceStep { Type = StepType.SetMaxVel, AxisNo = 0, Value = vel });
            _sequence.AddStep(new SequenceStep { Type = StepType.SetMaxAccel, AxisNo = 0, Value = acc });
            _sequence.AddStep(new SequenceStep
            {
                Type = StepType.MovePos,
                AxisNo = 0,
                Pos = 10.0,
                Vel = vel,
                Accel = acc,
                Decel = acc
            });
            _sequence.AddStep(new SequenceStep { Type = StepType.Wait, Value = 500 });
            _sequence.AddStep(new SequenceStep
            {
                Type = StepType.MovePos,
                AxisNo = 0,
                Pos = 0.0,
                Vel = vel,
                Accel = acc,
                Decel = acc
            });
            RefreshList();
        }

        // ═══════════════════════════════════════════════════════════
        #region UI Build
        private void BuildUI()
        {
            Text = "Robot Motion Controller — AXL (Ajin)";
            Size = new Size(1340, 900);
            MinimumSize = new Size(1200, 820);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Malgun Gothic", 9f);
            BackColor = C(230, 233, 238);
            FormClosing += (_, __) => { tmrStatus?.Stop(); _runner?.Stop(); _robot?.Dispose(); };

            const int M = 8; // margin

            // ══════════════════════════════════════════════════════
            // ROW 1: AXL 초기화 | 서보 제어
            // ══════════════════════════════════════════════════════
            int row1Y = M, row1H = 98;

            // ── [1] AXL 초기화 ─────────────────────────────────────
            var g1 = Grp("1.  AXL 초기화", M, row1Y, 310, row1H);
            Controls.Add(g1);
            {
                g1.Controls.Add(Lbl("IRQ :", 10, 27));
                nudIrq = Nud(48, 24, 52, -1, 15, 7);
                g1.Controls.Add(nudIrq);

                btnOpen = Btn("AxlOpen", 108, 23, 90, 28, C(0, 120, 215), Color.White);
                btnClose = Btn("AxlClose", 206, 23, 90, 28);
                btnClose.Enabled = false;
                btnOpen.Click += OnOpen;
                btnClose.Click += OnClose;
                g1.Controls.Add(btnOpen);
                g1.Controls.Add(btnClose);

                lblStatus = new Label
                {
                    Text = "●  미초기화",
                    ForeColor = Color.OrangeRed,
                    Location = new Point(10, 62),
                    AutoSize = true,
                    Font = new Font("Malgun Gothic", 8.5f, FontStyle.Bold)
                };
                g1.Controls.Add(lblStatus);

                lblAxisCount = new Label
                {
                    Text = "",
                    ForeColor = Color.DimGray,
                    Location = new Point(155, 62),
                    AutoSize = true
                };
                g1.Controls.Add(lblAxisCount);
            }

            // ── [2] 서보 제어 ──────────────────────────────────────
            var g2 = Grp("2.  서보 제어 & 영점", 326, row1Y, 990, row1H);
            Controls.Add(g2);
            {
                // 개별 축
                g2.Controls.Add(Lbl("대상 축 :", 10, 27));
                nudServoAxis = Nud(70, 24, 52, 0, 31, 0);
                g2.Controls.Add(nudServoAxis);

                btnServoOn = Btn("Servo ON", 130, 23, 90, 28, C(30, 150, 50), Color.White);
                btnServoOff = Btn("Servo OFF", 228, 23, 90, 28, C(170, 40, 40), Color.White);
                btnSetZero = Btn("Set Zero", 326, 23, 88, 28, C(100, 60, 160), Color.White);
                btnServoOn.Enabled = false;
                btnServoOff.Enabled = false;
                btnSetZero.Enabled = false;
                btnServoOn.Click += (_, __) => RobotDo(() => _robot.ServoOn((int)nudServoAxis.Value), $"Servo ON  Axis {nudServoAxis.Value}");
                btnServoOff.Click += (_, __) => RobotDo(() => _robot.ServoOff((int)nudServoAxis.Value), $"Servo OFF Axis {nudServoAxis.Value}");
                btnSetZero.Click += (_, __) => RobotDo(() => _robot.SetZeroPos((int)nudServoAxis.Value), $"SetZero  Axis {nudServoAxis.Value}");
                g2.Controls.Add(btnServoOn);
                g2.Controls.Add(btnServoOff);
                g2.Controls.Add(btnSetZero);

                // 구분선
                var sep = new Label
                {
                    Text = "|",
                    Location = new Point(428, 12),
                    AutoSize = true,
                    ForeColor = Color.Silver,
                    Font = new Font("Malgun Gothic", 22f)
                };
                g2.Controls.Add(sep);

                // 전체 축
                g2.Controls.Add(Lbl("전체 축 :", 448, 27));
                btnServoOnAll = Btn("전체 Servo ON", 520, 23, 120, 28, C(20, 120, 40), Color.White);
                btnServoOffAll = Btn("전체 Servo OFF", 648, 23, 120, 28, C(140, 30, 30), Color.White);
                btnServoOnAll.Enabled = false;
                btnServoOffAll.Enabled = false;
                btnServoOnAll.Click += (_, __) => { for (int i = 0; i < _axisCount; i++) { int ax = i; RobotDo(() => _robot.ServoOn(ax), $"Servo ON  Axis {ax}"); } };
                btnServoOffAll.Click += (_, __) => { for (int i = 0; i < _axisCount; i++) { int ax = i; RobotDo(() => _robot.ServoOff(ax), $"Servo OFF Axis {ax}"); } };
                g2.Controls.Add(btnServoOnAll);
                g2.Controls.Add(btnServoOffAll);

                g2.Controls.Add(new Label
                {
                    Text = "※ 아래 축 테이블에서 행을 선택 후 [적용] 버튼으로 UPP / MaxVel / MaxAccel 을 축별로 설정하세요.",
                    Location = new Point(10, 64),
                    AutoSize = true,
                    ForeColor = Color.Gray,
                    Font = new Font("Malgun Gothic", 7.5f)
                });
            }

            // ══════════════════════════════════════════════════════
            // ROW 2: 축별 파라미터 & 실시간 상태 테이블
            // ══════════════════════════════════════════════════════
            int row2Y = row1Y + row1H + M;
            var g3 = Grp("3.  축별 파라미터 설정 (Unit / Pulse / MaxVel / MaxAccel)  &  실시간 상태", M, row2Y, 1316, 198);
            Controls.Add(g3);
            {
                dgvAxis = BuildAxisGrid(1296, 168);
                dgvAxis.Location = new Point(8, 22);
                g3.Controls.Add(dgvAxis);
            }

            // ══════════════════════════════════════════════════════
            // ROW 3: 시퀀스 (좌) + 로그 (우)
            // ══════════════════════════════════════════════════════
            int row3Y = row2Y + 198 + M;
            int seqW = 870, logW = 1316 - seqW - M;
            int seqH = 330, runH = 72;

            // ── 시퀀스 ─────────────────────────────────────────────
            var g4 = Grp("4.  시퀀스 스텝", M, row3Y, seqW, seqH);
            Controls.Add(g4);
            {
                lstSteps = new ListBox
                {
                    Location = new Point(8, 22),
                    Size = new Size(730, seqH - 34),
                    Font = new Font("Courier New", 8.5f),
                    ScrollAlwaysVisible = true,
                    BackColor = C(20, 22, 30),
                    ForeColor = C(160, 210, 255),
                    BorderStyle = BorderStyle.FixedSingle
                };
                g4.Controls.Add(lstSteps);

                int bx = 746, by = 22, bw = 116, bh = 30, bg = 5;
                SeqBtn(g4, "+ MovePos", bx, by, bw, bh, C(40, 80, 150), OnAddMove);
                SeqBtn(g4, "+ Wait", bx, by += bh + bg, bw, bh, C(40, 80, 150), OnAddWait);
                SeqBtn(g4, "▲  위로", bx, by += bh + bg * 4, bw, bh, null, (_, __) => MoveStep(-1));
                SeqBtn(g4, "▼  아래로", bx, by += bh + bg, bw, bh, null, (_, __) => MoveStep(1));
                SeqBtn(g4, "선택 삭제", bx, by += bh + bg * 4, bw, bh, C(150, 40, 40), OnRemoveStep);
                SeqBtn(g4, "전체 초기화", bx, by += bh + bg, bw, bh, null, OnClearSteps);
            }

            // ── 반복 & 실행 ─────────────────────────────────────────
            var g5 = Grp("5.  반복 및 실행", M, row3Y + seqH + M, seqW, runH);
            Controls.Add(g5);
            {
                g5.Controls.Add(Lbl("반복 횟수 :", 10, 28));
                nudRepeat = Nud(84, 25, 70, 1, 99999, 5);
                nudRepeat.ValueChanged += (_, __) => _sequence.RepeatCount = (int)nudRepeat.Value;
                g5.Controls.Add(nudRepeat);

                chkInfinite = new CheckBox { Text = "무한반복", Location = new Point(164, 28), AutoSize = true };
                chkInfinite.CheckedChanged += OnInfiniteChanged;
                g5.Controls.Add(chkInfinite);

                btnStart = Btn("▶  시작", 266, 17, 116, 38, C(30, 150, 50), Color.White);
                btnStop = Btn("⏹  정지", 390, 17, 116, 38, C(170, 40, 40), Color.White);
                btnStart.Font = btnStop.Font = new Font("Malgun Gothic", 10f, FontStyle.Bold);
                btnStop.Enabled = false;
                btnStart.Click += OnStart;
                btnStop.Click += (_, __) => _runner.Stop();
                g5.Controls.Add(btnStart);
                g5.Controls.Add(btnStop);

                pbRepeat = new ProgressBar
                {
                    Location = new Point(516, 24),
                    Size = new Size(284, 22),
                    Style = ProgressBarStyle.Continuous
                };
                g5.Controls.Add(pbRepeat);

                lblRepeatInfo = new Label { Text = "0 / 0", Location = new Point(806, 27), AutoSize = true };
                g5.Controls.Add(lblRepeatInfo);
            }

            // ── 로그 ───────────────────────────────────────────────
            int logX = M + seqW + M;
            int logTotalH = seqH + M + runH;
            var g6 = Grp("6.  실행 로그", logX, row3Y, logW, logTotalH);
            Controls.Add(g6);
            {
                rtbLog = new RichTextBox
                {
                    Location = new Point(8, 22),
                    Size = new Size(logW - 22, logTotalH - 62),
                    ReadOnly = true,
                    BackColor = C(14, 14, 20),
                    ForeColor = C(80, 220, 120),
                    Font = new Font("Courier New", 8f),
                    ScrollBars = RichTextBoxScrollBars.Vertical,
                    BorderStyle = BorderStyle.None
                };
                g6.Controls.Add(rtbLog);

                var btnClear = Btn("로그 지우기", 8, logTotalH - 38, 100, 26);
                btnClear.Click += (_, __) => rtbLog.Clear();
                g6.Controls.Add(btnClear);
            }
        }

        private DataGridView BuildAxisGrid(int w, int h)
        {
            var dg = new DataGridView
            {
                Size = new Size(w, h),
                ReadOnly = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = C(20, 22, 30),
                GridColor = C(48, 54, 66),
                BorderStyle = BorderStyle.None,
                Font = new Font("Courier New", 9f),
                EditMode = DataGridViewEditMode.EditOnKeystroke,
                ColumnHeadersHeight = 36,
                RowTemplate = { Height = 28 },
            };

            // 헤더 스타일
            dg.ColumnHeadersDefaultCellStyle.BackColor = C(32, 40, 54);
            dg.ColumnHeadersDefaultCellStyle.ForeColor = C(160, 200, 255);
            dg.ColumnHeadersDefaultCellStyle.Font = new Font("Malgun Gothic", 8.5f, FontStyle.Bold);
            dg.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dg.EnableHeadersVisualStyles = false;

            // 기본 셀 스타일
            dg.DefaultCellStyle.BackColor = C(20, 22, 30);
            dg.DefaultCellStyle.ForeColor = C(200, 210, 220);
            dg.DefaultCellStyle.SelectionBackColor = C(45, 60, 90);
            dg.DefaultCellStyle.SelectionForeColor = Color.White;
            dg.AlternatingRowsDefaultCellStyle.BackColor = C(26, 28, 38);

            // ── 읽기전용 컬럼 (실시간) ─────────────────────────────
            DgCol(dg, "Axis", "Axis", 30, true, MR: false);
            DgCol(dg, "Servo", "Servo", 44, true, MR: false);
            DgCol(dg, "ActPos", "ActPos\n(unit)", 108, true, MR: true);
            DgCol(dg, "CmdPos", "CmdPos\n(unit)", 108, true, MR: true);
            DgCol(dg, "ActVel", "ActVel\n(u/s)", 90, true, MR: true);
            DgCol(dg, "Torque", "Torque\n(%)", 72, true, MR: true);
            DgCol(dg, "InMot", "In Motion", 76, true, MR: false);

            // ── 편집가능 컬럼 ──────────────────────────────────────
            var editBg = new DataGridViewCellStyle
            {
                BackColor = C(28, 38, 58),
                ForeColor = C(180, 230, 255),
                SelectionBackColor = C(50, 75, 115),
                SelectionForeColor = Color.White
            };
            DgCol(dg, "Unit", "Unit\n✏", 72, false, MR: true, style: editBg);
            DgCol(dg, "Pulse", "Pulse\n✏", 90, false, MR: true, style: editBg);
            DgCol(dg, "MaxVel", "MaxVel\n✏", 82, false, MR: true, style: editBg);
            DgCol(dg, "MaxAccel", "MaxAccel\n✏", 82, false, MR: true, style: editBg);

            // ── 적용 버튼 컬럼 ─────────────────────────────────────
            var applyCol = new DataGridViewButtonColumn
            {
                Name = "Apply",
                HeaderText = "적용",
                Text = "적 용",
                UseColumnTextForButtonValue = true,
                FillWeight = 44,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = C(40, 60, 100),
                    ForeColor = Color.White,
                    SelectionBackColor = C(60, 90, 140),
                    SelectionForeColor = Color.White,
                    Font = new Font("Malgun Gothic", 8.5f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            };
            dg.Columns.Add(applyCol);

            // 포맷
            dg.Columns["ActPos"].DefaultCellStyle.Format = "F4";
            dg.Columns["CmdPos"].DefaultCellStyle.Format = "F4";
            dg.Columns["ActVel"].DefaultCellStyle.Format = "F3";
            dg.Columns["Torque"].DefaultCellStyle.Format = "F2";
            dg.Columns["Unit"].DefaultCellStyle.Format = "G6";
            dg.Columns["MaxVel"].DefaultCellStyle.Format = "F3";
            dg.Columns["MaxAccel"].DefaultCellStyle.Format = "F3";

            dg.CellClick += OnGridApply;
            dg.SelectionChanged += (_, __) => dg.ClearSelection();

            return dg;
        }
        #endregion

        // ═══════════════════════════════════════════════════════════
        #region Grid Data

        private void InitGrid(int n)
        {
            dgvAxis.Rows.Clear();
            for (int i = 0; i < n; i++)
            {
                double unit = 20; int pulse = 8388608;
                try { _robot.GetMoveUnitPerPulse(i, out unit, out pulse); } catch { }
                double mv = 0;
                try { mv = _robot.GetMaxVel(i); } catch { }

                dgvAxis.Rows.Add(i, "---", 0.0, 0.0, 0.0, 0.0, "---",
                    unit, pulse, mv, 0.0);
            }
        }

        private void RefreshParams()
        {
            for (int i = 0; i < _axisCount && i < dgvAxis.Rows.Count; i++)
            {
                double unit = 20; int pulse = 8388608;
                try { _robot.GetMoveUnitPerPulse(i, out unit, out pulse); } catch { }
                double mv = 0;
                try { mv = _robot.GetMaxVel(i); } catch { }

                dgvAxis.Rows[i].Cells["Unit"].Value = unit;
                dgvAxis.Rows[i].Cells["Pulse"].Value = pulse;
                dgvAxis.Rows[i].Cells["MaxVel"].Value = mv;
            }
        }

        private void OnStatusTick(object s, EventArgs e)
        {
            if (!_robot.IsConnected || _axisCount == 0) return;
            try
            {
                for (int ax = 0; ax < _axisCount && ax < dgvAxis.Rows.Count; ax++)
                {
                    double aPos = _robot.GetActPos(ax);
                    double cPos = _robot.GetCmdPos(ax);
                    double aVel = _robot.GetActVel(ax);
                    double tor = _robot.GetTorque(ax);
                    bool inM = _robot.IsInMotion(ax);
                    bool srv = _robot.IsServoOn(ax);

                    var row = dgvAxis.Rows[ax];
                    row.Cells["ActPos"].Value = aPos;
                    row.Cells["CmdPos"].Value = cPos;
                    row.Cells["ActVel"].Value = aVel;
                    row.Cells["Torque"].Value = tor;
                    row.Cells["InMot"].Value = inM ? "▶ Moving" : "■ Stop";
                    row.Cells["Servo"].Value = srv ? "ON" : "OFF";
                    row.Cells["Servo"].Style.ForeColor = srv ? Color.LimeGreen : Color.OrangeRed;
                    row.Cells["InMot"].Style.ForeColor = inM ? Color.Orange : C(100, 200, 100);
                }
            }
            catch { }
        }

        private void OnGridApply(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgvAxis.Columns[e.ColumnIndex].Name != "Apply") return;
            int ax = e.RowIndex;
            try
            {
                double unit = Convert.ToDouble(dgvAxis.Rows[ax].Cells["Unit"].Value);
                int pulse = Convert.ToInt32(dgvAxis.Rows[ax].Cells["Pulse"].Value);
                double maxVel = Convert.ToDouble(dgvAxis.Rows[ax].Cells["MaxVel"].Value);
                double maxAcc = Convert.ToDouble(dgvAxis.Rows[ax].Cells["MaxAccel"].Value);

                RobotDo(() => _robot.SetMoveUnitPerPulse(ax, unit, pulse),
                        $"UPP    Axis{ax}  unit={unit}  pulse={pulse}");
                if (maxVel > 0)
                    RobotDo(() => _robot.SetMaxVel(ax, maxVel), $"MaxVel Axis{ax} = {maxVel:F3}");
                if (maxAcc > 0)
                    RobotDo(() => _robot.SetMaxAccel(ax, maxAcc), $"MaxAcc Axis{ax} = {maxAcc:F3}");

                RefreshParams();
            }
            catch (Exception ex) { Log("[오류] Apply: " + ex.Message); }
        }
        #endregion

        // ═══════════════════════════════════════════════════════════
        #region Button Handlers

        private void OnOpen(object s, EventArgs e)
        {
            try
            {
                if (!_robot.Open((int)nudIrq.Value)) return;

                lblStatus.Text = "●  초기화됨";
                lblStatus.ForeColor = C(30, 180, 60);
                btnOpen.Enabled = false;
                btnClose.Enabled = true;
                btnServoOn.Enabled = btnServoOff.Enabled = true;
                btnServoOnAll.Enabled = btnServoOffAll.Enabled = true;
                btnSetZero.Enabled = true;

                _axisCount = _robot.GetAxisCount();
                nudServoAxis.Maximum = Math.Max(0, _axisCount - 1);
                lblAxisCount.Text = $"(감지 축: {_axisCount}개)";

                InitGrid(_axisCount);
                tmrStatus.Start();
                Log($"[초기화] AxlOpen 성공 — 감지 축 수: {_axisCount}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "초기화 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnClose(object s, EventArgs e)
        {
            tmrStatus.Stop();
            _robot.Disconnect();
            _axisCount = 0;
            lblStatus.Text = "●  미초기화";
            lblStatus.ForeColor = Color.OrangeRed;
            lblAxisCount.Text = "";
            btnOpen.Enabled = true;
            btnClose.Enabled = false;
            btnServoOn.Enabled = btnServoOff.Enabled = false;
            btnServoOnAll.Enabled = btnServoOffAll.Enabled = false;
            btnSetZero.Enabled = false;
            dgvAxis.Rows.Clear();
            Log("[초기화] AxlClose 완료");
        }

        private void OnAddMove(object s, EventArgs e)
        {
            using (var dlg = new StepEditDialog(StepType.MovePos))
                if (dlg.ShowDialog() == DialogResult.OK) { _sequence.AddStep(dlg.Result); RefreshList(); }
        }

        private void OnAddWait(object s, EventArgs e)
        {
            using (var dlg = new StepEditDialog(StepType.Wait))
                if (dlg.ShowDialog() == DialogResult.OK) { _sequence.AddStep(dlg.Result); RefreshList(); }
        }

        private void OnRemoveStep(object s, EventArgs e)
        {
            int i = lstSteps.SelectedIndex;
            if (i < 0) return;
            _sequence.Steps.RemoveAt(i);
            Renumber(); RefreshList();
        }

        private void OnClearSteps(object s, EventArgs e)
        {
            if (MessageBox.Show("모든 스텝을 삭제할까요?", "확인",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            { _sequence.Steps.Clear(); RefreshList(); }
        }

        private void OnInfiniteChanged(object s, EventArgs e)
        {
            nudRepeat.Enabled = !chkInfinite.Checked;
            _sequence.RepeatCount = chkInfinite.Checked ? 0 : (int)nudRepeat.Value;
        }

        private async void OnStart(object s, EventArgs e)
        {
            if (!_robot.IsConnected)
            { MessageBox.Show("AXL이 초기화되지 않았습니다.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (_sequence.Steps.Count == 0)
            { MessageBox.Show("시퀀스 스텝이 없습니다.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            SetRunning(true);
            pbRepeat.Value = 0;
            pbRepeat.Maximum = _sequence.RepeatCount == 0 ? 100 : _sequence.RepeatCount;
            await _runner.StartAsync(_sequence);
        }
        #endregion

        // ═══════════════════════════════════════════════════════════
        #region Helpers

        private void MoveStep(int dir)
        {
            int i = lstSteps.SelectedIndex, j = i + dir;
            if (i < 0 || j < 0 || j >= _sequence.Steps.Count) return;
            var t = _sequence.Steps[i]; _sequence.Steps[i] = _sequence.Steps[j]; _sequence.Steps[j] = t;
            Renumber(); RefreshList();
            lstSteps.SelectedIndex = j;
        }

        private void SetRunning(bool r) { btnStart.Enabled = !r; btnStop.Enabled = r; }
        private void RefreshList() { lstSteps.Items.Clear(); foreach (var s in _sequence.Steps) lstSteps.Items.Add(s.ToString()); }
        private void Renumber() { for (int i = 0; i < _sequence.Steps.Count; i++) _sequence.Steps[i].StepIndex = i; }
        private void Log(string m) { if (rtbLog.Lines.Length > 2000) rtbLog.Clear(); rtbLog.AppendText(m + "\n"); rtbLog.ScrollToCaret(); }
        private void SafeInvoke(Action a) { if (InvokeRequired) Invoke(a); else a(); }
        private void RobotDo(Action a, string name)
        { try { a(); Log($"[명령] {name} 완료"); } catch (Exception ex) { Log($"[오류] {name}: {ex.Message}"); } }

        // ── UI 팩토리 ─────────────────────────────────────────────
        static Color C(int r, int g, int b) => Color.FromArgb(r, g, b);

        static GroupBox Grp(string t, int x, int y, int w, int h) =>
            new GroupBox { Text = t, Location = new Point(x, y), Size = new Size(w, h), FlatStyle = FlatStyle.Flat };

        static Label Lbl(string t, int x, int y) =>
            new Label { Text = t, Location = new Point(x, y), AutoSize = true };

        static Button Btn(string t, int x, int y, int w, int h, Color? bg = null, Color? fg = null)
        {
            var b = new Button { Text = t, Location = new Point(x, y), Size = new Size(w, h), FlatStyle = FlatStyle.Flat };
            if (bg.HasValue) { b.BackColor = bg.Value; b.FlatAppearance.BorderColor = C(Math.Max(0, bg.Value.R - 30), Math.Max(0, bg.Value.G - 30), Math.Max(0, bg.Value.B - 30)); }
            if (fg.HasValue) b.ForeColor = fg.Value;
            return b;
        }

        static NumericUpDown Nud(int x, int y, int w, decimal mn, decimal mx, decimal val) =>
            new NumericUpDown { Location = new Point(x, y), Width = w, Minimum = mn, Maximum = mx, Value = val };

        static void DgCol(DataGridView dg, string name, string header, int fw, bool ro,
            bool MR = false, DataGridViewCellStyle style = null)
        {
            var col = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                FillWeight = fw,
                ReadOnly = ro,
            };
            col.DefaultCellStyle.Alignment = MR
                ? DataGridViewContentAlignment.MiddleRight
                : DataGridViewContentAlignment.MiddleCenter;
            col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (style != null) col.DefaultCellStyle = style;
            dg.Columns.Add(col);
        }

        static void SeqBtn(GroupBox g, string t, int x, int y, int w, int h,
            Color? bg, EventHandler handler)
        {
            var b = Btn(t, x, y, w, h, bg, bg.HasValue ? (Color?)Color.White : null);
            b.Click += handler;
            g.Controls.Add(b);
        }
        #endregion
    }
}