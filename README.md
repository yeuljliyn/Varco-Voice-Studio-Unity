# Varco Voice Studio for Unity

![Unity](https://img.shields.io/badge/Unity-6000.2%2B-black?logo=unity) ![C#](https://img.shields.io/badge/C%23-Editor%20Scripting-239120?logo=c-sharp) ![Platform](https://img.shields.io/badge/Platform-PC%20%7C%20Mac-lightgrey)

> **NC AI의 VARCO TTS API를 유니티 에디터(Window)에 통합하여, 기획자와 아티스트가 게임 엔진 내부에서 즉시 고품질 음성 자산을 생성하고 관리할 수 있도록 돕는 도구입니다.**

<p align="center">
  <img src="https://github.com/user-attachments/assets/7b1fddaa-e9cf-496b-b152-58d9f36e74bf" alt="Varco Voice Studio Main UI">
</p>


---

## 설치 방법 (Installation)

이 툴은 `.unitypackage` 형태로 제공됩니.

1. **[최신 버전 다운로드 (클릭)](https://github.com/yeuljliyn/Varco-Voice-Studio-Unity/releases/latest)** 링크를 눌러 배포 페이지로 이동합니다.
2. `Assets` 항목에 있는 **`VarcoVoiceStudio_v0.1.0.unitypackage`** 파일을 클릭하여 다운로드합니다.
3. 유니티 프로젝트를 켜고, 다운로드한 파일을 더블클릭하거나 **[Assets] -> [Import Package] -> [Custom Package...]**를 통해 임포트합니다.
4. 상단 메뉴바에서 **[Window] -> [VARCO Voice Studio]**를 클릭하면 툴이 실행됩니다.

---

## 사용법 (Usage)

1. **API Key 설정:** 발급받은 VARCO API Key를 입력합니다. (눈 모양 아이콘으로 가리기/보이기 가능)
2. **성우 검색:** 필터(성별, 나이, 감정 등)를 사용해 원하는 목소리를 찾습니다.
3. **대사 입력:** 원하는 대사를 입력합니다.
4. **미리듣기 & 저장:**
   * `[미리듣기]`: 파일을 생성하기 전, 현재 설정된 목소리로 자기소개 멘트를 미리 들어볼 수 있습니다.
   * `[파일 생성 및 저장]`: 결과물을 `Assets/VarcoOutput` 폴더에 `.wav` 파일로 저장합니다.

---

## 개발 배경 (Background)

게임 개발 프로토타이핑 단계에서 **가이드 음성(Placeholder Voice)** 은 필수적입니다. 하지만 기존 워크플로우는 **[웹사이트 접속 → 생성 → 다운로드 → 유니티 임포트]** 라는 비효율적인 과정을 반복해야 했습니다.

이러한 반복 작업을 줄이고, **엔진 내부에서 클릭 한 번으로 음성 생성부터 파일 저장까지**완료할 수 있는 **One-Stop Tool**을 개발하게 되었습니다.

---

## 핵심 기능 (Key Features)

### 1. 전용 에디터 윈도우 (Editor Window)
* 컴포넌트 방식이 아닌, 유니티 **독립 윈도우(Dockable Window)** 방식으로 구현되어 작업 공간 어디에든 배치하여 편리하게 사용할 수 있습니다.

### 2. 직관적인 사용자 경험 (UX) 개선
* **Custom Inspector UI:** 수백 명의 성우 리스트를 복잡한 드롭다운 대신 **버튼형 툴바(Toolbar)** 필터로 개편하여 접근성을 높였습니다.
* **상세 필터링:** 성별, 연령, 톤(Pitch), 감정(Emotion) 등의 속성을 조합하여 원하는 목소리를 즉시 찾을 수 있습니다.

### 3. 생성 일관성 확보 (Seed Control)
* **Seed 고정:** 생성형 AI 특유의 무작위성을 제어하기 위해, 마음에 드는 연기 톤의 Seed 값을 고정(Lock)하여 **일관된 결과물**을 얻을 수 있습니다.
* **Random/Fixed 모드:** `-1`(랜덤)과 특정 값(고정)을 시각적으로 명확히 구분하여 제공합니다.

---

## 기술적 구현 (Technical Details)

* **Unity Editor Scripting:** `EditorWindow` 클래스를 상속받아 커스텀 UI를 구현했습니다.
* **Network:** `UnityWebRequest`를 사용하여 비동기적으로 REST API와 통신합니다.
* **Security:** `PasswordField`를 활용하여 API Key 노출을 방지했습니다.

---

## Developer

* **Name:** 이유진
* **Role:** Unity Developer / Technical Artist
* **Contact:** elly3385@gmail.com

---
> *이 프로젝트는 개인 학습 및 포트폴리오 목적으로 개발된 베타 버전(Beta)입니다.*
