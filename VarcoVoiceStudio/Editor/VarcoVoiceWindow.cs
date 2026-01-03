using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using System.IO;
using System.Linq;

public class VarcoVoiceWindow : EditorWindow
{
    [MenuItem("Window/VARCO Voice Studio")]
    public static void ShowWindow()
    {
        GetWindow<VarcoVoiceWindow>("Varco Voice");
    }
    private string apiKey = "";
    private bool isApiKeyHidden = true;
    private const string BaseUrl = "https://openapi.ai.nc.com/tts/standard/v1/api";

    private int selectedVoiceIndex = 0;
    private List<VoiceData> voiceList = new List<VoiceData>();
    private string searchKeyword = "";

    private int genderIndex = 0; private string[] genderOpts = new string[] { "전체", "남성", "여성" };
    private int ageIndex = 0; private string[] ageOpts = new string[] { "전체", "어린이", "청소년", "청년", "중년", "노년" };
    private int pitchIndex = 0; private string[] pitchOpts = new string[] { "전체", "고음", "중음", "저음" };
    private int toneIndex = 0; private string[] toneOpts = new string[] { "전체", "거침", "굵음", "맑음", "얇음" };
    private int emotionIndex = 0; private string[] emotionOpts = new string[] { "전체", "기쁨", "슬픔", "분노", "중립" };

    private int languageIndex = 0;
    private string[] languageDisplayOpts = new string[] { "한국어", "영어(미국)", "일본어", "대만어" };
    private string[] languageValues = new string[] { "korean", "english", "japanese", "taiwanese" };
    private string language = "korean";

    private string textToSpeak = "안녕하세요, 바르코 보이스입니다.";
    private float speed = 1.0f;
    private float pitch = 1.0f;
    private int qualitySteps = 20;
    
    private int seed = -1;
    private int lastUsedSeed = 0;

    private Vector2 scrollPosition; 

    void OnGUI()
    {
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
        GUIStyle paddingButtonStyle = new GUIStyle(GUI.skin.button) { fixedHeight = 40, fontSize = 12 };

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Space(15);
        EditorGUILayout.LabelField("VARCO VOICE STUDIO", titleStyle);
        GUILayout.Space(10);

        GUILayout.BeginVertical("box");
        GUILayout.BeginHorizontal();
        
        if (isApiKeyHidden) apiKey = EditorGUILayout.PasswordField("API Key", apiKey);
        else apiKey = EditorGUILayout.TextField("API Key", apiKey);

        string iconName = isApiKeyHidden ? "d_scenevis_hidden" : "d_scenevis_visible";
        GUIContent iconContent = EditorGUIUtility.IconContent(iconName);
        iconContent.tooltip = isApiKeyHidden ? "키 보이기" : "키 숨기기";

        if (GUILayout.Button(iconContent, GUILayout.Width(30), GUILayout.Height(18)))
        {
            isApiKeyHidden = !isApiKeyHidden;
            GUI.FocusControl(null);
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("목록 갱신")) FetchVoiceList();
        GUILayout.EndVertical();

        GUILayout.Space(5);

        if (voiceList != null && voiceList.Count > 0)
        {
            GUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("성우 필터 (Filter)", headerStyle);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("이름:", GUILayout.Width(40));
            searchKeyword = EditorGUILayout.TextField(searchKeyword, GUI.skin.FindStyle("SearchTextField"));
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            DrawFilterRow("성별", ref genderIndex, genderOpts);
            DrawFilterRow("나이", ref ageIndex, ageOpts);
            DrawFilterRow("높이", ref pitchIndex, pitchOpts);
            DrawFilterRow("톤",   ref toneIndex, toneOpts);
            DrawFilterRow("감정", ref emotionIndex, emotionOpts);

            var filtered = voiceList.AsEnumerable();
            if (genderIndex > 0) filtered = filtered.Where(v => v.genderTag == genderOpts[genderIndex]);
            if (ageIndex > 0) filtered = filtered.Where(v => v.ageTag == ageOpts[ageIndex]);
            if (pitchIndex > 0) filtered = filtered.Where(v => v.pitchTag == pitchOpts[pitchIndex]);
            if (toneIndex > 0) filtered = filtered.Where(v => v.toneTag == toneOpts[toneIndex]);
            if (emotionIndex == 1) filtered = filtered.Where(v => v.emotion == "Happy");
            else if (emotionIndex == 2) filtered = filtered.Where(v => v.emotion == "Sad");
            else if (emotionIndex == 3) filtered = filtered.Where(v => v.emotion == "Angry");
            else if (emotionIndex == 4) filtered = filtered.Where(v => v.emotion == "Neutral");
            if (!string.IsNullOrEmpty(searchKeyword)) filtered = filtered.Where(v => v.speaker_name.Contains(searchKeyword) || v.styleTag.Contains(searchKeyword));

            List<VoiceData> resultList = filtered.ToList();

            GUILayout.Space(10);

            if (resultList.Count == 0)
            {
                EditorGUILayout.HelpBox("조건에 맞는 성우가 없습니다.", MessageType.Warning);
            }
            else
            {
                int currentIndex = -1;
                string currentUuid = (voiceList.Count > selectedVoiceIndex) ? voiceList[selectedVoiceIndex].speaker_uuid : "";
                for(int i=0; i<resultList.Count; i++) { if(resultList[i].speaker_uuid == currentUuid) { currentIndex = i; break; } }
                
                string[] displayNames = resultList.Select(v => $"[{v.speaker_name}] {v.styleTag}").ToArray();
                EditorGUILayout.LabelField($"검색 결과: {resultList.Count}명", EditorStyles.miniLabel);
                
                int newIndex = EditorGUILayout.Popup(currentIndex, displayNames);
                if (newIndex != currentIndex && newIndex >= 0)
                {
                    string selectedUuid = resultList[newIndex].speaker_uuid;
                    selectedVoiceIndex = voiceList.FindIndex(v => v.speaker_uuid == selectedUuid);
                    GUI.FocusControl(null); 
                }

                if (newIndex >= 0)
                {
                    var v = resultList[newIndex];
                    GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
                    GUILayout.BeginVertical("helpbox");
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{v.speaker_name}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"{v.genderTag} | {v.ageTag} | {v.pitchTag}", EditorStyles.miniLabel);
                    GUILayout.EndHorizontal();
                    EditorGUILayout.LabelField($"특징: {v.styleTag} ({v.toneTag})", EditorStyles.wordWrappedLabel);
                    GUILayout.EndVertical();
                    GUI.backgroundColor = Color.white;
                }
            }
            GUILayout.EndVertical();
        }

        GUILayout.Space(5);
        GUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("언어 선택 (Language)", headerStyle);
        
        languageIndex = System.Array.IndexOf(languageValues, language);
        if (languageIndex < 0) languageIndex = 0;
        int newLangIndex = GUILayout.Toolbar(languageIndex, languageDisplayOpts, GUILayout.Height(30));
        if (newLangIndex != languageIndex) { languageIndex = newLangIndex; language = languageValues[languageIndex]; }

        GUILayout.Space(5);
        EditorGUILayout.LabelField("대사 입력 (Text)", headerStyle);
        textToSpeak = EditorGUILayout.TextArea(textToSpeak, GUILayout.Height(60));
        GUILayout.EndVertical();

        GUILayout.Space(5);

        GUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("세부 설정 (Details)", headerStyle);
        
        GUILayout.BeginHorizontal();
        speed = EditorGUILayout.Slider("속도", speed, 0.5f, 1.5f);
        if (GUILayout.Button("↺", GUILayout.Width(25))) { speed = 1.0f; GUI.FocusControl(null); }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        pitch = EditorGUILayout.Slider("높낮이", pitch, 0.5f, 1.5f);
        if (GUILayout.Button("↺", GUILayout.Width(25))) { pitch = 1.0f; GUI.FocusControl(null); }
        GUILayout.EndHorizontal();

        qualitySteps = EditorGUILayout.IntSlider("품질", qualitySteps, 8, 20);
        
        GUILayout.Space(10);

        EditorGUILayout.HelpBox("Tip: 마음에 드는 목소리가 나왔다면 '고정하기'를 누르세요.\nSeed를 고정하면 언제든 똑같은 연기톤으로 재생성할 수 있습니다.", MessageType.Info);

        GUILayout.BeginHorizontal();
        seed = EditorGUILayout.IntField("Seed", seed);
        if(seed == -1) { 
            GUI.contentColor = Color.yellow; 
            GUILayout.Label("Random", GUILayout.Width(60)); 
            GUI.contentColor = Color.white; 
        }
        else { 
            if(GUILayout.Button("Reset", GUILayout.Width(50))) { seed = -1; GUI.FocusControl(null); } 
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal("helpbox");
        EditorGUILayout.LabelField($"Last Seed: {lastUsedSeed}", EditorStyles.label);
        if (GUILayout.Button("고정하기", GUILayout.Width(80))) { seed = lastUsedSeed; GUI.FocusControl(null); }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.Space(15);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("미리듣기", paddingButtonStyle, GUILayout.ExpandWidth(true))) GenerateVoice(true);
        
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.5f); 
        if (GUILayout.Button("파일 생성 및 저장", paddingButtonStyle, GUILayout.ExpandWidth(true))) GenerateVoice(false);
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        EditorGUILayout.HelpBox("파일은 'Assets/VarcoOutput' 폴더에 저장됩니다.", MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    private void DrawFilterRow(string label, ref int index, string[] options)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(40)); 
        index = GUILayout.Toolbar(index, options); 
        GUILayout.EndHorizontal();
    }


    private void StartEditorCoroutine(IEnumerator routine)
    {
        EditorApplication.CallbackFunction callback = null;
        callback = () =>
        {
            if (routine != null && routine.MoveNext()) { }
            else { EditorApplication.update -= callback; }
        };
        EditorApplication.update += callback;
    }

    private void FetchVoiceList()
    {
        if (string.IsNullOrEmpty(apiKey)) { Debug.LogError("[Error] API Key가 비어있습니다!"); return; }
        StartEditorCoroutine(GetVoicesRoutine());
    }

    IEnumerator GetVoicesRoutine()
    {
        string url = $"{BaseUrl}/voices/varco";
        Debug.Log("성우 목록 갱신 중...");

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader("OPENAPI_KEY", apiKey);
            var op = www.SendWebRequest();
            while (!op.isDone) yield return null;

            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = "{\"items\":" + www.downloadHandler.text + "}";
                VoiceListWrapper wrapper = JsonUtility.FromJson<VoiceListWrapper>(json);
                voiceList = wrapper.items;

                foreach(var v in voiceList)
                {
                    if (v.speaker_name.Contains("(분노)")) v.emotion = "Angry";
                    else if (v.speaker_name.Contains("(슬픔)")) v.emotion = "Sad";
                    else if (v.speaker_name.Contains("(행복)")) v.emotion = "Happy";
                    else if (v.speaker_name.Contains("(중립)")) v.emotion = "Neutral";
                    else v.emotion = "None";

                    if (!string.IsNullOrEmpty(v.description))
                    {
                        string[] parts = v.description.Split(',');
                        if (parts.Length >= 5)
                        {
                            v.genderTag = parts[0].Trim(); v.ageTag = parts[1].Trim();    
                            v.pitchTag = parts[2].Trim(); v.toneTag = parts[3].Trim(); v.styleTag = parts[4].Trim();  
                        }
                    }
                }
                Debug.Log($"[Success] 성우 {voiceList.Count}명 로드 완료!");
            }
            else Debug.LogError($"[Error] 목록 갱신 실패: {www.error}");
        }
    }

    private void GenerateVoice(bool isPreview)
    {
        if (voiceList == null || voiceList.Count == 0) { Debug.LogError("[Warning] 성우 목록 갱신 필요"); return; }
        
        VoiceData currentVoice = voiceList[selectedVoiceIndex];
        string pureName = currentVoice.speaker_name.Split('(')[0].Trim();
        string displayName = pureName;

        switch (language)
        {
            case "japanese": displayName = HangulToKatakana(pureName); break;
            case "english": displayName = ConvertToRoman(pureName); break;
            case "taiwanese": displayName = ConvertToRoman(pureName); break;
        }

        string finalText = textToSpeak;
        if (isPreview)
        {
            switch (language)
            {
                case "english": finalText = $"Hello. I am {displayName}."; break;
                case "japanese": finalText = $"こんにちは。私は{displayName}です。"; break;
                case "taiwanese": finalText = "你好。這是我聲音的預覽。"; break;
                default: finalText = $"안녕하세요. 저는 {currentVoice.description} 목소리의 {currentVoice.speaker_name}입니다."; break;
            }
        }
        
        StartEditorCoroutine(PostRequest(currentVoice.speaker_uuid, finalText, isPreview, currentVoice.speaker_name));
    }

    IEnumerator PostRequest(string uuid, string text, bool isPreview, string speakerName)
    {
        VarcoRequestData data = new VarcoRequestData();
        data.text = text; data.voice = uuid; data.language = language;
        data.properties = new VoiceProperties { speed = speed, pitch = pitch };
        data.n_fm_steps = qualitySteps; 
        
        int actualSeed = seed;
        if (actualSeed == -1) actualSeed = UnityEngine.Random.Range(1, 999999); 
        data.seed = actualSeed; lastUsedSeed = actualSeed;   

        string jsonBody = JsonUtility.ToJson(data);

        using (UnityWebRequest www = new UnityWebRequest($"{BaseUrl}/synthesize", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("OPENAPI_KEY", apiKey);

            var op = www.SendWebRequest();
            while (!op.isDone) yield return null;

            if (www.result == UnityWebRequest.Result.Success)
            {
                VarcoResponse res = JsonUtility.FromJson<VarcoResponse>(www.downloadHandler.text);
                if (!string.IsNullOrEmpty(res.audio))
                {
                    byte[] bytes = Convert.FromBase64String(res.audio);
                    if (isPreview) PlayPreviewAudio(bytes);
                    else SaveAudioFile(bytes, speakerName);
                    Debug.Log($"[Success] 생성 완료 (Seed: {lastUsedSeed})");
                }
            }
            else Debug.LogError($"[Error] 통신 에러: {www.downloadHandler.text}");
        }
    }

    private void PlayPreviewAudio(byte[] bytes)
    {
        string tempPath = Path.Combine(Application.temporaryCachePath, "temp_preview.wav");
        File.WriteAllBytes(tempPath, bytes);

        GameObject previewObj = GameObject.Find("VarcoAudioPreview");
        if (previewObj == null) 
        {
            previewObj = new GameObject("VarcoAudioPreview");
            previewObj.hideFlags = HideFlags.HideAndDontSave; 
        }
        
        AudioSource source = previewObj.GetComponent<AudioSource>();
        if (source == null) source = previewObj.AddComponent<AudioSource>();

        StartEditorCoroutine(LoadAndPlay(tempPath, source));
    }

    IEnumerator LoadAndPlay(string path, AudioSource source)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.WAV))
        {
            var op = www.SendWebRequest();
            while(!op.isDone) yield return null;

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (source != null) { source.clip = clip; source.Play(); }
            }
        }
    }

    void SaveAudioFile(byte[] bytes, string speakerName)
    {
        string folderPath = Path.Combine(Application.dataPath, "VarcoOutput");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        string fileName = $"{speakerName}_Seed{lastUsedSeed}_{DateTime.Now:MMdd_HHmmss}.wav";
        string fullPath = Path.Combine(folderPath, fileName);
        File.WriteAllBytes(fullPath, bytes);
        AssetDatabase.Refresh();
        Debug.Log($"[Saved] {fileName}");
    }

    private string ConvertToRoman(string koreanName) {
        string[] ArrCho = { "G", "K", "N", "D", "T", "R", "M", "B", "P", "S", "SS", "O", "J", "CH", "K", "T", "P", "H" };
        string[] ArrJung = { "a", "ae", "ya", "yae", "eo", "e", "yeo", "ye", "o", "wa", "wae", "oe", "yo", "u", "wo", "we", "wi", "yu", "eu", "ui", "i" };
        string[] ArrJong = { "", "k", "k", "ks", "n", "nj", "nh", "d", "l", "lg", "lm", "lb", "ls", "lt", "lp", "lh", "m", "b", "bs", "s", "ss", "ng", "j", "ch", "k", "t", "p", "h" };
        StringBuilder result = new StringBuilder();
        foreach (char c in koreanName) {
            if (c >= 0xAC00 && c <= 0xD7A3) {
                int code = c - 0xAC00;
                if (result.Length > 0) result.Append("-");
                result.Append(ArrCho[(code / 28) / 21]);
                result.Append(ArrJung[(code / 28) % 21]);
                if (code % 28 > 0) result.Append(ArrJong[code % 28]);
            } else result.Append(c);
        }
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(result.ToString().ToLower());
    }

    private string HangulToKatakana(string koreanName) {
        StringBuilder result = new StringBuilder();
        foreach (char c in koreanName) {
            if (c >= 0xAC00 && c <= 0xD7A3) {
                int code = c - 0xAC00;
                int jong = code % 28; int jung = (code / 28) % 21; int cho = (code / 28) / 21;

                string[,] kataTable = new string[19, 21];
                for(int i=0; i<19; i++) for(int j=0; j<21; j++) kataTable[i,j] = "ー";
                
                kataTable[11,0] = "ア"; kataTable[11,2] = "ヤ"; kataTable[11,4] = "オ"; kataTable[11,5] = "エ"; kataTable[11,8] = "オ"; kataTable[11,12] = "ヨ"; kataTable[11,13] = "ウ"; kataTable[11,17] = "ユ"; kataTable[11,18] = "ウ"; kataTable[11,20] = "イ";
                int[] kSeries = {0, 1, 15}; string[] kKana = {"カ", "キャ", "コ", "ケ", "コ", "キョ", "ク", "キュ", "ク", "キ"};
                foreach(int k in kSeries) { kataTable[k,0]=kKana[0]; kataTable[k,2]=kKana[1]; kataTable[k,4]=kKana[2]; kataTable[k,5]=kKana[3]; kataTable[k,8]=kKana[4]; kataTable[k,12]=kKana[5]; kataTable[k,13]=kKana[6]; kataTable[k,17]=kKana[7]; kataTable[k,18]=kKana[8]; kataTable[k,20]=kKana[9]; }
                kataTable[2,0]="ナ"; kataTable[2,2]="ニャ"; kataTable[2,4]="ノ"; kataTable[2,5]="ネ"; kataTable[2,8]="ノ"; kataTable[2,12]="ニョ"; kataTable[2,13]="ヌ"; kataTable[2,17]="ニュ"; kataTable[2,18]="ヌ"; kataTable[2,20]="ニ";
                int[] tSeries = {3, 4, 16}; string[] tKana = {"タ", "チャ", "ト", "テ", "ト", "チョ", "トゥ", "チュ", "トゥ", "ティ"};
                foreach(int t in tSeries) { kataTable[t,0]=tKana[0]; kataTable[t,2]=tKana[1]; kataTable[t,4]=tKana[2]; kataTable[t,5]=tKana[3]; kataTable[t,8]=tKana[4]; kataTable[t,12]=tKana[5]; kataTable[t,13]=tKana[6]; kataTable[t,17]=tKana[7]; kataTable[t,18]=tKana[8]; kataTable[t,20]=tKana[9]; }
                kataTable[5,0]="ラ"; kataTable[5,2]="リャ"; kataTable[5,4]="ロ"; kataTable[5,5]="レ"; kataTable[5,8]="ロ"; kataTable[5,12]="リョ"; kataTable[5,13]="ル"; kataTable[5,17]="リュ"; kataTable[5,18]="ル"; kataTable[5,20]="リ";
                kataTable[6,0]="マ"; kataTable[6,2]="ミャ"; kataTable[6,4]="モ"; kataTable[6,5]="メ"; kataTable[6,8]="モ"; kataTable[6,12]="ミョ"; kataTable[6,13]="ム"; kataTable[6,17]="ミュ"; kataTable[6,18]="ム"; kataTable[6,20]="ミ";
                int[] pSeries = {7, 8, 17}; string[] pKana = {"パ", "ピャ", "ポ", "ペ", "ポ", "ピョ", "プ", "ピュ", "プ", "ピ"};
                foreach(int p in pSeries) { kataTable[p,0]=pKana[0]; kataTable[p,2]=pKana[1]; kataTable[p,4]=pKana[2]; kataTable[p,5]=pKana[3]; kataTable[p,8]=pKana[4]; kataTable[p,12]=pKana[5]; kataTable[p,13]=pKana[6]; kataTable[p,17]=pKana[7]; kataTable[p,18]=pKana[8]; kataTable[p,20]=pKana[9]; }
                int[] sSeries = {9, 10}; string[] sKana = {"サ", "シャ", "ソ", "セ", "ソ", "ショ", "ス", "シュ", "ス", "シ"};
                foreach(int s in sSeries) { kataTable[s,0]=sKana[0]; kataTable[s,2]=sKana[1]; kataTable[s,4]=sKana[2]; kataTable[s,5]=sKana[3]; kataTable[s,8]=sKana[4]; kataTable[s,12]=sKana[5]; kataTable[s,13]=sKana[6]; kataTable[s,17]=sKana[7]; kataTable[s,18]=sKana[8]; kataTable[s,20]=sKana[9]; }
                int[] jSeries = {12, 13, 14}; string[] jKana = {"ジャ", "ジャ", "ジョ", "ジェ", "ジョ", "ジョ", "ジュ", "ジュ", "ジュ", "ジ"};
                foreach(int j in jSeries) { kataTable[j,0]=jKana[0]; kataTable[j,2]=jKana[1]; kataTable[j,4]=jKana[2]; kataTable[j,5]=jKana[3]; kataTable[j,8]=jKana[4]; kataTable[j,12]=jKana[5]; kataTable[j,13]=jKana[6]; kataTable[j,17]=jKana[7]; kataTable[j,18]=jKana[8]; kataTable[j,20]=jKana[9]; }
                kataTable[18,0]="ハ"; kataTable[18,2]="ヒャ"; kataTable[18,4]="ホ"; kataTable[18,5]="ヘ"; kataTable[18,8]="ホ"; kataTable[18,12]="ヒョ"; kataTable[18,13]="フ"; kataTable[18,17]="ヒュ"; kataTable[18,18]="フ"; kataTable[18,20]="ヒ";

                string baseKana = kataTable[cho, jung];
                if(baseKana == "ー") baseKana = "・";
                result.Append(baseKana);

                if (jong == 4) result.Append("ン"); 
                else if (jong == 16) result.Append("ム"); 
                else if (jong == 8) result.Append("ル"); 
                else if (jong == 1 || jong == 24) result.Append("ク"); 
                else if (jong == 17 || jong == 27) result.Append("プ"); 
                else if (jong == 21) result.Append("ン"); 
                else if (jong == 7) result.Append("ッ"); 
            } else result.Append(c);
        }
        return result.ToString();
    }

    [Serializable] public class VoiceListWrapper { public List<VoiceData> items; }
    [Serializable] public class VoiceData { public string speaker_uuid, speaker_name, saas_name, description, emotion, genderTag, ageTag, pitchTag, toneTag, styleTag; }
    [Serializable] public class VarcoRequestData { public string text, language, voice; public VoiceProperties properties; public int n_fm_steps, seed; }
    [Serializable] public class VoiceProperties { public float speed, pitch; }
    [Serializable] public class VarcoResponse { public string audio; }
}