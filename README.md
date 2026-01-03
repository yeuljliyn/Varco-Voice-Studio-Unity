# 🎙️ Varco Voice Studio for Unity

![Unity](https://img.shields.io/badge/Unity-6000.2%2B-black?logo=unity) ![C#](https://img.shields.io/badge/C%23-Editor%20Scripting-239120?logo=c-sharp) ![Platform](https://img.shields.io/badge/Platform-PC%20%7C%20Mac-lightgrey)

> **Varco Voice TTS API를 유니티 에디터(Inspector)에 통합하여, 기획자와 아티스트가 게임 엔진 내부에서 즉시 음성 자산을 생성하고 관리할 수 있도록 돕는 생산성 도구입니다.**

<p align="center">
  <img src="https://github.com/user-attachments/assets/7fa066c9-8451-4afd-8fc4-ec2ca053911a" alt="Varco Voice Studio Main UI">
</p>

---

## 📦 설치 및 사용법 (Installation & Usage)

1. 이 리포지토리의 `Assets` 폴더 내 파일들을 다운로드하여, 내 유니티 프로젝트의 `Assets` 폴더에 넣습니다.
   * `Assets/Scripts/VarcoVoiceManager.cs`
   * `Assets/Editor/VarcoVoiceManagerEditor.cs`
2. Scene(씬)에 **빈 오브젝트(Empty Object)** 를 생성합니다.
3. 해당 오브젝트에 **`VarcoVoiceManager`** 컴포넌트를 추가합니다.
4. 인스펙터 창에서 발급받은 **API Key**를 입력하고 **[🔄 목록 갱신]** 버튼을 누릅니다.
5. 필터를 사용하여 원하는 성우를 선택하고 대사를 입력합니다.
6. **[▶ 미리듣기]** 로 목소리를 확인한 후, **[🎙️ 파일 생성 및 저장]** 버튼을 누르면 완료됩니다.

---

## 🎯 개발 배경 (Background)

게임 개발 프로토타이핑 단계에서 **가이드 음성(Placeholder Voice)** 은 필수적입니다. 하지만 기존 워크플로우는 **[웹사이트 접속 → 생성 → 다운로드 → 유니티 임포트]** 라는 비효율적인 과정을 반복해야 했습니다.

이러한 반복 작업을 줄이고, **엔진 내부에서 클릭 한 번으로 음성 생성부터 파일 저장까지**완료할 수 있는 **One-Stop Tool**을 개발하게 되었습니다.

---

## ✨ 핵심 기능 (Key Features)

### 1. 직관적인 사용자 경험 (UX) 개선
* **Custom Inspector UI:** 수백 명의 성우 리스트를 복잡한 드롭다운 대신 **버튼형 툴바(Toolbar)** 필터로 개편하여 접근성을 높였습니다.
* **상세 필터링:** 성별, 연령, 톤(Pitch), 감정(Emotion) 등의 속성을 조합하여 원하는 목소리를 즉시 찾을 수 있습니다.

### 2. 생성 일관성 확보 (Seed Control)
* **Seed 고정:** 생성형 AI 특유의 무작위성을 제어하기 위해, 마음에 드는 연기 톤의 Seed 값을 고정(Lock)하여 **일관된 결과물**을 얻을 수 있습니다.
* **Random/Fixed 모드:** `-1`(랜덤)과 특정 값(고정)을 시각적으로 명확히 구분하여 제공합니다.

### 3. 워크플로우 자동화
* **자동 저장 시스템:** 생성된 음성은 `Assets/VarcoOutput` 폴더에 `[성우명]_[Seed]_[타임스탬프].wav` 형식으로 자동 저장됩니다.
* **실시간 미리듣기:** 파일을 생성하기 전, 현재 설정된 목소리로 자기소개 멘트를 미리 들어볼 수 있습니다.

---

## 🛠️ 기술적 구현 (Technical Details)

### 🔹 유니티 에디터 확장 (Editor Scripting)
`CustomEditor` 클래스를 상속받아 기본 Inspector를 완전히 재구성했습니다. `GUILayout`과 `EditorGUILayout`을 활용하여 **런타임(게임 실행 중)이 아닌 개발 단계**에서의 생산성을 최적화했습니다.

### 🔹 REST API 통신 및 비동기 처리
`UnityWebRequest`를 사용하여 Varco Voice API와 비동기 통신(Coroutine)을 수행합니다. JSON 데이터의 직렬화/역직렬화를 통해 요청(Request)과 응답(Response)을 처리하며, 네트워크 예외 처리를 통해 안정성을 확보했습니다.

---

## 👨‍💻 Developer

* **Name:** 이유진
* **Role:** Unity Developer / Technical Artist (Aspiring)
* **Contact:** elly3385@gmail.com

---
> *이 프로젝트는 개인 학습 및 포트폴리오 목적으로 개발되었습니다.*
