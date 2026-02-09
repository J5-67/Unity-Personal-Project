# 📡 Yuni's Sync Note (From Home ↔ To Lab)

이 문서는 **현재 진행 중인 작업**, **해야 할 일(ToDo)**, 그리고 **유니끼리 남기는 메시지**를 적는 곳이야! 💌
작업실에서 집에 갈 때, 집에서 작업실로 갈 때 꼭 업데이트하기!

---

## 🚀 진행 중 (In Progress)
### 1. 🏗️ 훅 & 줄 타기 (Hook Physics) - [Refining]
*   [x] **Winch Down 안정화**: 최대 길이에서 덜덜거림, 무한 추락, 늘어짐 현상 완벽 해결 (StrictMode 적용).
*   [x] **Descent Speed Control**: `S` 키 하강 시 속도를 `climbSpeed`의 50%로 제한하여 중력 가속도 상쇄.
*   [!] **Tuning Note**: 하강 속도가 맘에 안 들면 `PlayerHook.cs`의 `limitSpeed` 계수(`0.5f`) 조절 필요. (Drag 조절은 비추!)

### 2. 이전 완료 항목
*   [x] **W Key (Up)**: 줄을 빠르게 감으면서 위치를 강제로 당김 (`MovePosition`).
*   [x] **S Key (Down)**: 줄을 풀면서 **속도 제한(Velocity Limit)** 방식을 적용하여 매우 부드럽게 하강. (뚝뚝 끊김 해결!) 🧈
*   [x] **Climb Speed**: `PlayerHook` 스크립트에 `climbSpeed`(기본값 6) 변수 추가하여 W/S 속도 통합 관리.
*   [x] **No Auto-Stretch**: 그네 타거나 매달려 있을 때 줄이 늘어나는 현상을 완벽하게 차단 (Ratchet + Velocity Cut).

### 2. ⚡ 물리 엔진 튜닝 (Physics Tuning) - [완료]
*   [x] **Hybrid Solver**:
    *   **W/Idle**: 위치(Position) 기반의 강력한 보정 (단단함).
    *   **S (Down)**: 속도(Velocity) 기반의 유연한 제어 (부드러움).
    *   이 두 가지 방식을 상황에 따라 스위칭하도록 코드 전면 개편.

---

## ✅ 해야 할 일 (ToDo List)
*   [ ] **작업실 도착하면**: `PlayerHook` 인스펙터에서 `climbSpeed` 값을 6~10 사이로 조절해보면서 최종 손맛 확인하기.
*   [ ] **튜토리얼 구역**: 이제 훅 액션(Zip, Swing, Winch Up/Down)이 완성됐으니, 이걸 다 써먹을 수 있는 **종합 훈련장** 레벨 만들기.
*   [ ] **사운드**: 윈치 감을 때(끼릭끼릭)랑 풀 때(휘리릭) 효과음 추가하면 더 찰질 듯!

## 💡 아이디어 & 백로그 (Ideas & Backlog)
*   **[Combat] 원거리 공격 적 (Turret/Drone)**: 플레이어를 조준(Lock-on) 하고 발사하는 적. 대시/불릿타임 활용도 UP.
*   **[Gimmick] 움직이는 발판 (Moving Platform)**: 훅을 걸면 따라오거나 회전하는 물리 기반 플랫폼.
*   **[Interact] 파괴 가능한 벽 (Breakable Wall)**: 대시로 충돌 시 파괴되는 벽. 숏컷/비밀공간 연출용.

---

## 📂 수정된 파일 목록 (Modified Files)
*   `DEV_HISTORY.md`: 2026-02-09 작업 내용 정리.
*   `3.Script/0.Core/DynamicCamera.cs` (New): 마우스 커서(조준) 방향으로 카메라를 살짝 이동시켜 시야 확보 (Dynamic FOV 폐기).
*   `3.Script/0.Core/SpeedEffects.cs` (New): 고속 이동 시 잔상(Ghost) 및 집중선(SpeedLine) 이펙트 제어.
*   `3.Script/0.Core/GridSnapper.cs` (New): 에디터에서 오브젝트를 그리드에 맞춰 딱딱 붙여주는 레벨 디자인 도구.

---

## 💌 유니의 메시지 (Message)
## 💌 유니의 메시지 (Message)
> **From 집 유니 🏠**:
> 오빠! 🔥 **[비주얼 폴리싱 & 맵 설계]** 완료!
>
> 1.  **📸 Dynamic Camera (Fix)**: 마우스 조준 방향으로 시야 확보! (FollowOffset 제어 방식)
> 2.  **⚡ Speed Line (New)**: 화면에 집중선 촥! (Camera Local Space + Stretched Billboard)
> 3.  **🏗️ Grid Snapper**: 레벨 디자인 툴 준비 완료.
> 4.  **🗺️ Tutorial Map**: "The Awakening" 맵 설계도(`DEV_HISTORY.md`) 참고해서 `Winch Tunnel` 같은 재밌는 구간 만들어보자!
>
> (참고: **Spike**는 충돌 위치에 따라 밀어내는 방향이 다르니까(천장은 아래로 ↓), 배치할 때 유의해!)
>
> 이제 진짜 게임 만드는 느낌 난다! 재밌게 만들어봐! 화이팅! 💕
