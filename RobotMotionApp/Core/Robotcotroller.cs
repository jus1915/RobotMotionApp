using System;
using System.Runtime.InteropServices;

// ─────────────────────────────────────────────────────────────────
// EZSoftware UC C# 래퍼 사용 방법 (두 가지 중 하나 선택)
//
// [방법 A] CAXL 래퍼 클래스 참조 (PID_AutoTunning 프로젝트와 동일한 방식 - 권장)
//   - C:\Program Files (x86)\EzSoftware UC\AXL(Library)\Library\64Bit\ 에서
//     CAXL.cs / CAXM.cs 파일을 프로젝트에 추가
//   - using static CAXL; using static CAXM; 사용
//   - 모든 함수 파라미터가 ref 방식
//
// [방법 B] DllImport 직접 사용 (현재 이 파일)
//   - AXL.dll을 실행파일과 같은 폴더에 배치
//   - 파라미터를 ref로 선언해야 함 (out 아님!)
// ─────────────────────────────────────────────────────────────────

namespace RobotMotionApp.Core
{
    public class RobotController : IDisposable
    {
        #region AXL.dll DllImport (ref 방식 - PID_AutoTunning 프로젝트 참고)

        private const string DLL = "AXL.dll";

        // ── 라이브러리 초기화/종료 ──────────────────────────────────
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxlOpen(int nIRQ);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxlClose();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxlIsOpened();

        // ── 축 정보 ────────────────────────────────────────────────
        // ref 방식 (out 아님!)
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmInfoGetAxisCount(ref int lpAxisCount);

        // ── 서보 ON/OFF ────────────────────────────────────────────
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmSignalServoOn(int lAxisNo, uint dwOnOff);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmSignalIsServoOn(int lAxisNo, ref uint dwpOnOff);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmSignalReadServoAlarm(int lAxisNo, ref uint dwpAlarm);

        // ── 모션 파라미터 ──────────────────────────────────────────
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmMotSetMaxVel(int lAxisNo, double dMaxVel);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmMotGetMaxVel(int lAxisNo, ref double dpMaxVel);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmMotSetMaxAccel(int lAxisNo, double dMaxAccel);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmMotSetMaxDecel(int lAxisNo, double dMaxDecel);

        // dwAbsRelMode: 0=절대(ABS), 1=상대(REL)
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmMotSetAbsRelMode(int lAxisNo, uint dwAbsRelMode);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmMotGetAbsRelMode(int lAxisNo, ref uint dwpAbsRelMode);

        // dwProfileMode: 0=대칭사다리꼴, 2=대칭S-곡선
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmMotSetProfileMode(int lAxisNo, uint dwProfileMode);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmMotGetProfileMode(int lAxisNo, ref uint dwpProfileMode);

        // ── 이동 ───────────────────────────────────────────────────
        // 비동기 이동 (즉시 반환)
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmMoveStartPos(int lAxisNo, double dPos,
            double dVel, double dAccel, double dDecel);

        // 동기 이동 (완료까지 대기)
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmMovePos(int lAxisNo, double dPos,
            double dVel, double dAccel, double dDecel);

        // ── 정지 ───────────────────────────────────────────────────
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmMoveEStop(int lAxisNo);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmMoveSStop(int lAxisNo);

        // ── 다축 동시 이동 ─────────────────────────────────────────
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmMoveMultiPos(int lArraySize,
            int[] lpAxisNo, double[] dpPos,
            double dVel, double dAccel, double dDecel);

        // ── 상태/위치 읽기 (ref 방식) ──────────────────────────────
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmStatusReadInMotion(int lAxisNo, ref uint dwpInMotion);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmStatusGetActPos(int lAxisNo, ref double dpActPos);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmStatusGetCmdPos(int lAxisNo, ref double dpCmdPos);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmStatusSetActPos(int lAxisNo, double dActPos);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmStatusSetCmdPos(int lAxisNo, double dCmdPos);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmStatusReadActVel(int lAxisNo, ref double dpActVel);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint AxmStatusReadTorque(int lAxisNo, ref double dpTorque);

        #endregion

        public const uint AXT_RT_SUCCESS = 0;
        public const uint SERVO_ON = 1;
        public const uint SERVO_OFF = 0;
        public const uint ABS_MODE = 0;
        public const uint REL_MODE = 1;

        private bool _isOpen = false;
        private bool _disposed = false;

        public bool IsConnected { get { return _isOpen; } }

        // ── 초기화 ─────────────────────────────────────────────────
        /// <summary>AXL 라이브러리 초기화 (IRQ: 7 권장, -1=자동)</summary>
        public bool Open(int irq = 7)
        {
            uint ret = AxlOpen(irq);
            _isOpen = (ret == AXT_RT_SUCCESS);
            if (!_isOpen)
                throw new RobotException(string.Format("AxlOpen 실패 (코드: 0x{0:X})", ret));
            return _isOpen;
        }

        public bool Connect(string unused = "", int port = 7)
        {
            return Open(port);
        }

        public void Disconnect()
        {
            if (_isOpen)
            {
                AxlClose();
                _isOpen = false;
            }
        }

        // ── 서보 ───────────────────────────────────────────────────
        public void ServoOn(int axisNo = 0)
        {
            Check();
            uint ret = AxmSignalServoOn(axisNo, SERVO_ON);
            if (ret != AXT_RT_SUCCESS)
                throw new RobotException(string.Format("ServoOn 실패 axis={0} (0x{1:X})", axisNo, ret));
        }

        public void ServoOff(int axisNo = 0)
        {
            Check();
            AxmSignalServoOn(axisNo, SERVO_OFF);
        }

        public bool IsServoOn(int axisNo)
        {
            Check();
            uint val = 0;
            AxmSignalIsServoOn(axisNo, ref val);
            return (val != 0);
        }

        public void ServoOnAll()
        {
            int count = GetAxisCount();
            for (int i = 0; i < count; i++) ServoOn(i);
        }

        // ── 파라미터 ───────────────────────────────────────────────
        public void SetMaxVel(int axisNo, double vel)
        {
            Check();
            uint ret = AxmMotSetMaxVel(axisNo, vel);
            if (ret != AXT_RT_SUCCESS)
                throw new RobotException(string.Format("SetMaxVel 실패 (0x{0:X})", ret));
        }

        public void SetMaxAccel(int axisNo, double accel)
        {
            Check();
            AxmMotSetMaxAccel(axisNo, accel);
            AxmMotSetMaxDecel(axisNo, accel);
        }

        public void SetAbsRelMode(int axisNo, uint mode)
        {
            Check();
            AxmMotSetAbsRelMode(axisNo, mode);
        }

        public void SetProfileMode(int axisNo, uint mode = 0)
        {
            Check();
            AxmMotSetProfileMode(axisNo, mode);
        }

        // ── 이동 ───────────────────────────────────────────────────
        /// <summary>비동기 이동 (AxmMoveStartPos)</summary>
        public void MoveStartPos(int axisNo, double pos, double vel, double accel, double decel)
        {
            Check();
            uint ret = AxmMoveStartPos(axisNo, pos, vel, accel, decel);
            if (ret != AXT_RT_SUCCESS)
                throw new RobotException(string.Format("MoveStartPos 실패 axis={0} (0x{1:X})", axisNo, ret));
        }

        /// <summary>동기 이동 (AxmMovePos - 완료까지 블로킹)</summary>
        public void MovePos(int axisNo, double pos, double vel, double accel, double decel)
        {
            Check();
            uint ret = AxmMovePos(axisNo, pos, vel, accel, decel);
            if (ret != AXT_RT_SUCCESS)
                throw new RobotException(string.Format("MovePos 실패 axis={0} (0x{1:X})", axisNo, ret));
        }

        /// <summary>다축 동시 이동 (비동기)</summary>
        public void MoveMultiPos(int[] axisNos, double[] positions,
            double vel, double accel, double decel)
        {
            Check();
            if (axisNos.Length != positions.Length)
                throw new ArgumentException("축 번호/위치 배열 크기 불일치");
            uint ret = AxmMoveMultiPos(axisNos.Length, axisNos, positions, vel, accel, decel);
            if (ret != AXT_RT_SUCCESS)
                throw new RobotException(string.Format("MoveMultiPos 실패 (0x{0:X})", ret));
        }

        public void Stop(int axisNo) { Check(); AxmMoveSStop(axisNo); }
        public void EStop(int axisNo) { Check(); AxmMoveEStop(axisNo); }

        // ── 상태 읽기 ──────────────────────────────────────────────
        public bool IsInMotion(int axisNo)
        {
            Check();
            uint status = 0;
            AxmStatusReadInMotion(axisNo, ref status);
            return (status != 0);
        }

        public bool WaitMotionDone(int axisNo, int timeoutMs = 30000)
        {
            Check();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (!IsInMotion(axisNo)) return true;
                System.Threading.Thread.Sleep(10);
            }
            return false;
        }

        public bool WaitMultiMotionDone(int[] axisNos, int timeoutMs = 30000)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                bool anyMoving = false;
                foreach (int ax in axisNos)
                    if (IsInMotion(ax)) { anyMoving = true; break; }
                if (!anyMoving) return true;
                System.Threading.Thread.Sleep(10);
            }
            return false;
        }

        public double GetActPos(int axisNo)
        {
            Check();
            double pos = 0;
            AxmStatusGetActPos(axisNo, ref pos);
            return pos;
        }

        public double GetCmdPos(int axisNo)
        {
            Check();
            double pos = 0;
            AxmStatusGetCmdPos(axisNo, ref pos);
            return pos;
        }

        public double GetActVel(int axisNo)
        {
            Check();
            double vel = 0;
            AxmStatusReadActVel(axisNo, ref vel);
            return vel;
        }

        public double GetTorque(int axisNo)
        {
            Check();
            double tor = 0;
            AxmStatusReadTorque(axisNo, ref tor);
            return tor;
        }

        public void SetZeroPos(int axisNo)
        {
            Check();
            AxmStatusSetActPos(axisNo, 0);
            AxmStatusSetCmdPos(axisNo, 0);
        }

        public int GetAxisCount()
        {
            Check();
            int count = 0;
            AxmInfoGetAxisCount(ref count);
            return count;
        }

        private void Check()
        {
            if (!_isOpen)
                throw new RobotException("AXL 라이브러리가 초기화되지 않았습니다. Open()을 먼저 호출하세요.");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Disconnect();
                _disposed = true;
            }
        }
    }

    public class RobotException : Exception
    {
        public RobotException(string message) : base(message) { }
        public RobotException(string message, Exception inner) : base(message, inner) { }
    }
}