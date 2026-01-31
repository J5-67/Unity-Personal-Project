# 📔 Yuni & Oppa's Development History

이 문서는 프로젝트의 모든 **개발 기록**을 날짜별로 누적해서 정리하는 곳이야! 📜✍️

---

## 📅 2026-01-31 (오늘의 작업)
### 1. 🖥️ UI - 메인 메뉴 & 설정 (Settings) - [완료]
*   **메인 메뉴 구조화**:
    *   `MainMenuController` 개선: Play/Settings 버튼과 서브 메뉴(Start/Settings) 연결.
    *   **New Game**: '1.GameTest' 씬 로드 기능 구현.
*   **설정(Settings) 시스템 구축**:
    *   `SettingsMenuController` 제작.
    *   **설정 저장**: `PlayerPrefs`를 활용해 Mouse Sensitivity, Audio Volume, Graphic 설정 저장/로드.
    *   **양방향 동기화**: Slider ↔ InputField 값 연동 (입력 시 Placeholder 업데이트 방식 적용).
    *   **그래픽 설정**: FullScreen / Windowed 모드 전환 Dropdown 구현.

### 2. ⏯️ 인게임 UI (Pause Menu) - [완료]
*   **GameManager & PauseUI**:
    *   **일시정지**: `ESC` 키로 `Time.timeScale` 조절 및 메뉴 호출.
    *   **Singleton & Input**: `GameManager`를 싱글톤으로 구성하고, New Input System의 `Pause` 액션 연결.
    *   **버그 수정**: 씬 전환 시 `GameManager` 중복 파괴 과정에서 `NullReference` 발생하는 **초기화 순서 문제 해결** (OnEnable/Disable null 체크).

### 3. 🤐 대화 중 행동 제어 (Dialogue Input Blocking) - [완료]
*   **GameManager**: `IsDialogueActive` 상태 관리 추가.
*   **DialogueUI**: 대화창 `Show`/`Hide` 시 `GameManager`에 상태 전달.
*   **PlayerMovement**:
    *   `IsDialogueActive` 상태일 때 **이동, 점프, 대시** 입력 완전 무시.
    *   물리 연산에서도 이동 벡터를 0으로 강제하여 미끄러짐 방지.

### 4. 🧶 훅 & 조준선 비주얼 개선 (Hook Visuals) - [완료]
*   **타겟별 시각적 구별**:
    *   **벽/허공/얼음**: 정적인 점선 (`-`, Dash) + 느린 흐름.
    *   **Light Enemy (당겨옴)**: 플레이어 쪽으로 흐르는 초록색 역방향 화살표 (`<`, Pull).
    *   **Heavy Enemy (날아감)**: 적 쪽으로 빠르게 흐르는 빨간색 정방향 화살표 (`>`, Zip).
*   **애니메이션 & 밀도 최적화**:
    *   **Stretch 모드 도입**: 거리에 상관없이 일정한 무늬 간격을 유지하도록 `LineTextureMode.Stretch`와 스크립트 기반 Tiling 계산 적용.
    *   **밀도 분리 제어**: 점선(`Dash Tiling`)과 화살표(`Arrow Tiling`)의 밀도를 각각 조절 가능하도록 변수 분리.
    *   **쉐이더 교체**: `Legacy Shaders/Particles/Alpha Blended`를 사용하여 색상(`TintColor`)과 UV 애니메이션 모두 지원.

---

## 📅 2026-01-30
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
*   **트러블슈팅**:
    *   **드롭다운 짤림 문제**: `Dropdown` -> `Template` -> `Viewport`의 앵커 및 크기 설정 문제 확인.

---
