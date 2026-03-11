using System;
using System.Collections.Generic;

namespace RobotMotionApp.Models
{
    public enum StepType
    {
        MovePos,        // 단축 이동 (절대/상대)
        MoveMultiPos,   // 다축 동시 이동
        SetMaxVel,      // 최대 속도 설정
        SetMaxAccel,    // 최대 가속/감속 설정
        SetAbsRelMode,  // 절대/상대 모드
        Wait,           // 딜레이
        ServoOn,        // 서보 ON
        ServoOff        // 서보 OFF
    }

    public class SequenceStep
    {
        public int StepIndex { get; set; }
        public StepType Type { get; set; }
        public string Description { get; set; }

        // 단축 이동
        public int AxisNo { get; set; }   // 축 번호
        public double Pos { get; set; }   // 목표 위치
        public double Vel { get; set; }   // 속도
        public double Accel { get; set; }   // 가속도
        public double Decel { get; set; }   // 감속도
        public bool WaitDone { get; set; } = true;
        public bool Async { get; set; } = false; // false=동기(AxmMovePos), true=비동기(AxmMoveStartPos)

        // 다축 이동
        public int[] MultiAxisNos { get; set; }
        public double[] MultiPos { get; set; }

        // 설정값 (SetMaxVel, SetMaxAccel, Wait ms)
        public double Value { get; set; }

        // 모드값 (SetAbsRelMode)
        public uint Mode { get; set; }

        public override string ToString()
        {
            switch (Type)
            {
                case StepType.MovePos:
                    return string.Format("[{0:D2}] MovePos    Axis={1}  Pos={2:F3}  Vel={3:F1}  Acc={4:F1}  {5}",
                        StepIndex, AxisNo, Pos, Vel, Accel, Async ? "(비동기)" : "(동기)");
                case StepType.MoveMultiPos:
                    return string.Format("[{0:D2}] MoveMulti  Axes=[{1}]  Vel={2:F1}",
                        StepIndex,
                        MultiAxisNos != null ? string.Join(",", MultiAxisNos) : "",
                        Vel);
                case StepType.SetMaxVel:
                    return string.Format("[{0:D2}] SetMaxVel  Axis={1}  {2:F1} unit/s", StepIndex, AxisNo, Value);
                case StepType.SetMaxAccel:
                    return string.Format("[{0:D2}] SetMaxAccel Axis={1}  {2:F1} unit/s²", StepIndex, AxisNo, Value);
                case StepType.SetAbsRelMode:
                    return string.Format("[{0:D2}] AbsRelMode Axis={1}  {2}", StepIndex, AxisNo, Mode == 0 ? "ABS" : "REL");
                case StepType.Wait:
                    return string.Format("[{0:D2}] Wait       {1:F0} ms", StepIndex, Value);
                case StepType.ServoOn:
                    return string.Format("[{0:D2}] ServoON    Axis={1}", StepIndex, AxisNo);
                case StepType.ServoOff:
                    return string.Format("[{0:D2}] ServoOFF   Axis={1}", StepIndex, AxisNo);
                default:
                    return string.Format("[{0:D2}] Unknown", StepIndex);
            }
        }
    }

    public class MotionSequence
    {
        public string Name { get; set; }
        public List<SequenceStep> Steps { get; set; }
        public int RepeatCount { get; set; }   // 0 = 무한
        public int MotionTimeoutMs { get; set; }

        public MotionSequence()
        {
            Name = "Sequence_1";
            Steps = new List<SequenceStep>();
            RepeatCount = 1;
            MotionTimeoutMs = 30000;
        }

        public void AddStep(SequenceStep step)
        {
            step.StepIndex = Steps.Count;
            Steps.Add(step);
        }
    }
}