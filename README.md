# RobotMotionApp

**Ajin EZSoftware UC (AXL.dll) 기반 독립형 로봇 모션 시퀀서**

EZSoftware UC가 설치된 PC에서 `AXL.dll`을 직접 참조하여 로봇의 반복 모션 시퀀스를 구동하는 독립 소프트웨어입니다.

---

## 📌 주요 기능

| 기능 | 설명 |
|------|------|
| **AXL 초기화** | `AxlOpen(IRQ)` / `AxlClose()` |
| **서보 ON/OFF** | 축별 서보 전원 제어 |
| **모션 파라미터** | MaxVel / MaxAccel 스텝 설정 |
| **단축 이동** | `AxmMovePos` (동기) / `AxmMoveStartPos` (비동기) |
| **다축 동시 이동** | `AxmMoveMultiPos` |
| **Wait** | ms 단위 딜레이 |
| **반복 실행** | 지정 횟수 또는 무한 반복 |
| **실시간 로그** | 스텝별 실행 상태 출력 |

---

## 🗂️ 프로젝트 구조

```
RobotMotionApp/
├── Program.cs                  # 진입점
├── Core/
│   ├── RobotController.cs      # AXL.dll DllImport 래퍼 (ref 방식)
│   └── SequenceRunner.cs       # 시퀀스 비동기 실행 엔진
├── Models/
│   └── SequenceStep.cs         # 스텝 모델 & MotionSequence
└── UI/
    ├── MainForm.cs             # 메인 WinForms UI
    └── StepEditDialog.cs       # 스텝 추가 다이얼로그
```

---

## ⚙️ 환경 요구사항

| 항목 | 사양 |
|------|------|
| OS | Windows 10/11 (64-bit) |
| .NET | .NET Framework 4.8 |
| 언어 버전 | C# 7.3 |
| 빌드 플랫폼 | x64 |
| EZSoftware UC | 설치 필수 |

---

## 🚀 실제 PC에서 클론 후 설정 방법

### 1. 저장소 클론

```bash
git clone https://github.com/YOUR_USERNAME/RobotMotionApp.git
cd RobotMotionApp
```

### 2. AXL DLL 복사

EZSoftware UC가 설치된 PC의 아래 경로에서 DLL을 복사합니다:

```
C:\Program Files (x86)\EzSoftware UC\AXL(Library)\Library\64Bit\
    AXL.dll        ← 필수
    EzBasicAxl.dll ← 필수
```

빌드 출력 폴더에 복사:
```
RobotMotionApp\bin\x64\Debug\
    AXL.dll
    EzBasicAxl.dll
```

> ⚠️ `AXL.dll`과 `EzBasicAxl.dll`은 라이선스 보호 파일이므로 Git에 포함하지 않습니다.

### 3. 의존 런타임 확인

AXL.dll이 요구하는 런타임이 설치되어 있어야 합니다:

- **Visual C++ 2010 재배포 패키지 (x64)**
  - `MSVCR100.dll`, `MSVCP100.dll`, `mfc100.dll`
  - 설치: https://www.microsoft.com/en-us/download/details.aspx?id=26999

- **Wibu-Systems CodeMeter Runtime**
  - `WIBUCM64.dll` (EZSoftware UC 설치 시 자동 설치됨)
  - EZSoftware UC가 정상 설치된 PC라면 이미 있음

### 4. Visual Studio에서 빌드

```
플랫폼: x64
구성:   Debug 또는 Release
```

> ⚠️ **반드시 x64로 빌드** — AXL.dll이 64비트 전용입니다.

---

## 🔧 AXL 함수 시그니처 주의사항

이 프로젝트는 AXL.dll의 함수를 `DllImport`로 직접 호출합니다.  
파라미터는 **`out`이 아닌 `ref`** 방식을 사용합니다 (EZSoftware UC C# 래퍼 기준):

```csharp
// ✅ 올바른 방식 (ref)
AxmStatusGetActPos(axisNo, ref pos);
AxmStatusReadInMotion(axisNo, ref status);

// ❌ 잘못된 방식 (out)
AxmStatusGetActPos(axisNo, out pos);
```

### CAXL/CAXM 래퍼 사용 (선택 사항)

EZSoftware UC 설치 경로에 C# 래퍼 소스가 있다면 프로젝트에 추가하여 사용할 수 있습니다:

```
C:\Program Files (x86)\EzSoftware UC\AXL(Library)\Library\
    CAXL.cs
    CAXM.cs
    CAXDev.cs
```

---

## 📋 시퀀스 스텝 종류

| StepType | 설명 | 주요 파라미터 |
|----------|------|--------------|
| `ServoOn` | 서보 ON | AxisNo |
| `ServoOff` | 서보 OFF | AxisNo |
| `SetMaxVel` | 최대 속도 설정 | AxisNo, Value (unit/s) |
| `SetMaxAccel` | 최대 가속/감속 설정 | AxisNo, Value (unit/s²) |
| `MovePos` | 단축 이동 | AxisNo, Pos, Vel, Accel, Decel |
| `MoveMultiPos` | 다축 동시 이동 | MultiAxisNos[], MultiPos[] |
| `Wait` | 딜레이 | Value (ms) |

---

## 📝 개발 이력

| 날짜 | 내용 |
|------|------|
| 2026-03-11 | 최초 구현 — AXL.dll 연동, WinForms UI, 시퀀스 엔진 |

---

## ⚠️ 주의사항

- 실제 로봇 구동 전 **반드시 안전 영역 확인** 후 실행
- 서보 ON 상태에서 파라미터 변경 시 주의
- 소프트 리밋 설정은 AXL 파라미터로 별도 구성 필요
