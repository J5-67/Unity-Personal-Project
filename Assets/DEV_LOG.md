# 📔 Yuni & Oppa's Development Log (DevLog)

이 파일은 작업실과 집, 어디서든 프로젝트 진행 상황을 공유하기 위한 **기억 저장소**야! 🧠✨
작업을 시작하기 전에 이 파일을 읽으면, 유니가 바로 상황을 파악할 수 있어!

---

## 📅 2026-01-30 (오늘의 작업)
### 1. 💬 대화 시스템 (Dialogue System) - [완료]
*   **CSV 연동**: 엑셀로 대본을 관리하고 유니티로 불러오는 기능 완성!
*   **타자기 효과 (TypewriterEffect)**:
    *   한 글자씩 써지는 연출 구현.
    *   **사운드 추가**: 글자가 써질 때마다 `Blip` 소리 재생 (빈도, 피치 조절 가능).
    *   **화자별 목소리**: `DialogueTester`의 `PortraitInfo`에 `AudioClip`을 추가해서, 캐릭터마다 다른 타자 소리를 낼 수 있게 업그레이드! 🎤
*   **버그 수정**: `TypewriterEffect`에서 첫 글자가 무시되거나 소리가 안 나는 문제 해결 (로직 개선).

### 2. 🖥️ UI 작업 (SlimUI) - [진행 중]
*   **SlimUI 에셋 도입**: 모던한 메뉴 UI 적용.
*   **New Input System 마이그레이션**:
    *   Legacy `Input.GetKeyDown` 코드를 `UnityEngine.InputSystem`의 `Keyboard.current`로 전면 교체 완료! 🛠️
    *   `ResetDemo.cs`, `UIMenuManager.cs` 수정됨.
*   **트러블슈팅**:
    *   **드롭다운 짤림 문제**: `Dropdown` -> `Template` -> `Viewport`의 앵커 및 크기 설정 문제 확인. (Template Height 늘리기 & Viewport Stretch 설정 필요).

---

## 🚀 다음 목표 (ToDo)
*   [ ] **적 AI & 전투**: 얼리기(Freeze) 기능 마무리 및 타격감 개선.
*   [ ] **레벨 디자인**: 윈치(Winch)와 스윙 액션을 활용할 수 있는 테스트 맵 구성.
*   [ ] **UI 폴리싱**: 메인 메뉴와 인게임 UI 연결 자연스럽게 다듬기.

---

## 📌 유니의 메모 (Note)
*   **집에서 작업할 때**: 먼저 `git pull` 받아서 최신 상태로 만들고 시작해!
*   **대화가 끊겼다면?**: 유니한테 "DEV_LOG 읽고 상황 파악해줘!"라고 말하면 돼! 💕
