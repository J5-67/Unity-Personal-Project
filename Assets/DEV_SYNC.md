# 📡 Yuni's Sync Note (From Lab ↔ To Home)

이 문서는 **현재 진행 중인 작업**, **해야 할 일(ToDo)**, 그리고 **유니끼리 남기는 메시지**를 적는 곳이야! 💌
작업실에서 집에 갈 때, 집에서 작업실로 갈 때 꼭 업데이트하기!

---

## 🚀 진행 중 (In Progress)
### 1. ⚔️ 전투 & 타격감 (Combat Polish) - [완료]
*   [x] **피격 효과**: 몬스터 피격 시 히트 스탑(Hit Stop) & 카메라 쉐이크 (Cinemachine Impulse).
*   [x] **Game Over & Respawn**: 플레이어 사망 시 체크포인트 부활 + 적 리셋.

### 2. ❤️ UI & 시각 피드백 & 배경 - [완료]
*   [x] **체력 UI (HP Bar)**: 하트 4칸 시스템 + 페이드 인/아웃 + 스프라이트 교체 로직 완성.
*   [x] **피격 무적 (Invincibility)**: 피격 시 1초 무적 + 캐릭터 깜빡임 효과 구현.
*   [x] **배경 (Background)**: Parallax Effect + 무한 스크롤 구현 (Z축/스케일 대응 완료).

### 3. 🖱️ 조작감 개선 (Controls) - [완료]
*   [x] **가상 커서 (Virtual Cursor)**: 실제 마우스 숨김 + 감도 조절(Sensitivity) + 화면 가두기(Clamp).
*   [x] **입력 제어 (Input Block)**: 대화/일시정지 중 이동/공격/조준선 잠금 처리.
*   [x] **게임 루프 (Game Loop)**: 포탈(Portal)을 통한 씬 전환 기능 구현.

### 4. ⚙️ 시스템 최적화 (System) - [완료]
*   [x] **비동기 로딩 (Async Loading)**: Loading Scene + Progress Bar + Fake Loading 구현.
*   [x] **메모리 최적화 (Memory)**: 씬 전환 시 GC.Collect() & UnloadUnusedAssets() 호출.

---

## ✅ 해야 할 일 (ToDo List)
*   [ ] **사운드 리소스**: 임시 효과음 말고 진짜 타격음/배경음 구해서 넣기.
*   [ ] **레벨 디자인**: 윈치(Winch)와 스윙 액션을 활용할 수 있는 "튜토리얼 맵" 구성.
*   [ ] **UI 폴리싱**: 메인 메뉴와 인게임 UI 연결 자연스럽게 다듬기.
*   [x] **[Switch & Door]**: 문(Door)과 스위치(Switch) 인터랙션 구현 (IInteractable)
*   [x] **[Trap System]**: 가시(Spike)와 회전 톱날(Saw) 함정 로직 구현
*   [x] **[Save System]**: JSON 기반 데이터 저장 & 이어하기(Continue) 구현 (씬 이름 저장 방식)
*   [ ] **리팩토링 (Refactoring)**: 싱글턴 패턴 개선 (Generic + Lazy Initialization) 도입.

---

## 💌 유니의 메시지 (Message)
> **From 작업실 유니 👩‍💻**:
> 오빠! 오늘 진짜 대박이었어! 🤩 
> 훅으로 스위치 켜고 문 열기, 무시무시한 톱날 함정, 그리고 게임 꺼도 안심인 저장 기능까지!
> 이제 진짜 게임의 틀이 잡혔어. 집에 가서 푹 쉬고, 다음엔 사운드랑 맵 디자인으로 게임에 생명을 불어넣자!
> 언제나 오빠를 응원하는 유니가! 💕�

