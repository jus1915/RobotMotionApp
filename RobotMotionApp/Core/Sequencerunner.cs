using System;
using System.Threading;
using System.Threading.Tasks;
using RobotMotionApp.Models;

namespace RobotMotionApp.Core
{
    public enum RunnerState { Idle, Running, Stopped, Error }

    public class SequenceRunner
    {
        private readonly RobotController _robot;
        private CancellationTokenSource _cts;
        private Task _runTask;

        public RunnerState State { get; private set; }
        public int CurrentRepeat { get; private set; }
        public int CurrentStepIndex { get; private set; }

        public event Action<string> OnLog;
        public event Action<RunnerState> OnStateChanged;
        public event Action<int, int> OnProgress;  // (현재반복, 총반복)

        public SequenceRunner(RobotController robot)
        {
            _robot = robot;
            State = RunnerState.Idle;
        }

        public Task StartAsync(MotionSequence sequence)
        {
            if (State == RunnerState.Running)
                throw new InvalidOperationException("이미 실행 중입니다.");

            _cts = new CancellationTokenSource();
            _runTask = Task.Run(() => RunSequence(sequence, _cts.Token));
            return _runTask;
        }

        public void Stop()
        {
            if (_cts != null) _cts.Cancel();
        }

        private void RunSequence(MotionSequence seq, CancellationToken ct)
        {
            SetState(RunnerState.Running);
            Log(string.Format("▶ 시퀀스 [{0}] 시작 — 반복: {1}회",
                seq.Name, seq.RepeatCount == 0 ? "∞" : seq.RepeatCount.ToString()));

            try
            {
                int totalRepeats = seq.RepeatCount == 0 ? int.MaxValue : seq.RepeatCount;

                for (int rep = 1; rep <= totalRepeats; rep++)
                {
                    ct.ThrowIfCancellationRequested();
                    CurrentRepeat = rep;
                    if (OnProgress != null) OnProgress(rep, seq.RepeatCount);
                    Log(string.Format("--- 반복 {0}/{1} ---", rep,
                        seq.RepeatCount == 0 ? "∞" : seq.RepeatCount.ToString()));

                    foreach (SequenceStep step in seq.Steps)
                    {
                        ct.ThrowIfCancellationRequested();
                        CurrentStepIndex = step.StepIndex;
                        Log("  실행: " + step.ToString());
                        ExecuteStep(step, seq.MotionTimeoutMs, ct);
                    }
                }

                Log("✔ 시퀀스 완료");
                SetState(RunnerState.Idle);
            }
            catch (OperationCanceledException)
            {
                Log("⏹ 시퀀스 정지됨");
                SetState(RunnerState.Stopped);
            }
            catch (RobotException rex)
            {
                Log("❌ 로봇 오류: " + rex.Message);
                SetState(RunnerState.Error);
            }
            catch (Exception ex)
            {
                Log("❌ 예외: " + ex.Message);
                SetState(RunnerState.Error);
            }
        }

        private void ExecuteStep(SequenceStep step, int timeoutMs, CancellationToken ct)
        {
            switch (step.Type)
            {
                case StepType.ServoOn:
                    _robot.ServoOn(step.AxisNo);
                    break;

                case StepType.ServoOff:
                    _robot.ServoOff(step.AxisNo);
                    break;

                case StepType.SetMaxVel:
                    _robot.SetMaxVel(step.AxisNo, step.Value);
                    break;

                case StepType.SetMaxAccel:
                    _robot.SetMaxAccel(step.AxisNo, step.Value);
                    break;

                case StepType.SetAbsRelMode:
                    _robot.SetAbsRelMode(step.AxisNo, step.Mode);
                    break;

                case StepType.MovePos:
                    if (step.Async)
                    {
                        _robot.MoveStartPos(step.AxisNo, step.Pos, step.Vel, step.Accel, step.Decel);
                        if (step.WaitDone)
                            _robot.WaitMotionDone(step.AxisNo, timeoutMs);
                    }
                    else
                    {
                        // AxmMovePos 는 내부적으로 완료까지 대기
                        _robot.MovePos(step.AxisNo, step.Pos, step.Vel, step.Accel, step.Decel);
                    }
                    break;

                case StepType.MoveMultiPos:
                    _robot.MoveMultiPos(step.MultiAxisNos, step.MultiPos,
                        step.Vel, step.Accel, step.Decel);
                    if (step.WaitDone)
                        _robot.WaitMultiMotionDone(step.MultiAxisNos, timeoutMs);
                    break;

                case StepType.Wait:
                    Task.Delay((int)step.Value, ct).GetAwaiter().GetResult();
                    break;
            }
        }

        private void SetState(RunnerState state)
        {
            State = state;
            if (OnStateChanged != null) OnStateChanged(state);
        }

        private void Log(string msg)
        {
            if (OnLog != null)
                OnLog(string.Format("[{0:HH:mm:ss.fff}] {1}", DateTime.Now, msg));
        }
    }
}