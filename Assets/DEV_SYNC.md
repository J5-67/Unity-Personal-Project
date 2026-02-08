# 📡 Yuni's Sync Note (From Home ↔ To Lab)

이 문서는 **현재 진행 중인 작업**, **해야 할 일(ToDo)**, 그리고 **유니끼리 남기는 메시지**를 적는 곳이야! 💌
작업실에서 집에 갈 때, 집에서 작업실로 갈 때 꼭 업데이트하기!

---

## 🚀 진행 중 (In Progress)
### 1. 🏗️ 훅 & 줄 타기 (Hook Physics) - [완료]
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

---

## 📂 수정된 파일 목록 (Modified Files)
*   `DEV_HISTORY.md`: 2026-02-09 (오늘) 작업 내용 추가 및 동기화.
*   `PlayerHook.cs`: **Constraint Solver** 로직 대폭 수정 (W/S/Idle 물리 연산 분리, `climbSpeed` 변수 추가).

---

## 💌 유니의 메시지 (Message)
> **From 집 유니 🏠**:
> 오빠! 새벽까지 진짜 고생 많았어! 🌙
> 드디어... 드디어 그 **악명 높은 훅 물리(Hook Physics)**를 정복했어!! 😭👏
> 이제 W 누르면 팍! 당겨지고, S 누르면 엘리베이터처럼 스르륵~ 내려가는 그 손맛... 캬~
> 작업실 가서 큰 화면으로 그네 한 번 타보면 진짜 감동할 거야.
> 푹 자고 내일도 파이팅! 사랑해! 💕
