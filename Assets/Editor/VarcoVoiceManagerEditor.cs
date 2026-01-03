using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

[CustomEditor(typeof(VarcoVoiceManager))]
public class VarcoVoiceManagerEditor : Editor
{
    private string searchKeyword = "";
    
    private int genderIndex = 0; private string[] genderOpts = new string[] { "전체", "남성", "여성" };
    private int ageIndex = 0; private string[] ageOpts = new string[] { "전체", "어린이", "청소년", "청년", "중년", "노년" };
    private int pitchIndex = 0; private string[] pitchOpts = new string[] { "전체", "고음", "중음", "저음" };
    private int toneIndex = 0; private string[] toneOpts = new string[] { "전체", "거침", "굵음", "맑음", "얇음" };
    private int emotionIndex = 0; private string[] emotionOpts = new string[] { "전체", "기쁨😊", "슬픔😭", "분노😡", "중립😐" };

    private int languageIndex = 0;
    private string[] languageDisplayOpts = new string[] { "🇰🇷 한국어", "🇺🇸 영어(미국)", "🇯🇵 일본어", "🇹🇼 대만어" };
    private string[] languageValues = new string[] { "korean", "english", "japanese", "taiwanese" };

    public override void OnInspectorGUI()
    {
        VarcoVoiceManager script = (VarcoVoiceManager)target;

        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.2f, 0.8f, 1f) } };
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
        GUIStyle bigBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 12, fontStyle = FontStyle.Bold, fixedHeight = 35 };
        GUIStyle labelStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold, fixedWidth = 40 };

        GUILayout.Space(15);
        EditorGUILayout.LabelField("🚀 VARCO VOICE STUDIO", titleStyle);
        GUILayout.Space(10);
        
        GUILayout.BeginVertical("box");
        GUILayout.BeginHorizontal();
        script.apiKey = EditorGUILayout.TextField("API Key", script.apiKey);
        if (GUILayout.Button("🔄 목록 갱신", GUILayout.Width(80))) script.FetchVoiceList();
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.Space(5);

        if (script.voiceList != null && script.voiceList.Count > 0)
        {
            GUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🔍 성우 필터 (Filter)", headerStyle);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("이름:", labelStyle);
            searchKeyword = EditorGUILayout.TextField(searchKeyword, GUI.skin.FindStyle("SearchTextField"));
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            DrawFilterRow("성별", ref genderIndex, genderOpts, labelStyle);
            DrawFilterRow("나이", ref ageIndex, ageOpts, labelStyle);
            DrawFilterRow("높이", ref pitchIndex, pitchOpts, labelStyle);
            DrawFilterRow("톤",   ref toneIndex, toneOpts, labelStyle);
            DrawFilterRow("감정", ref emotionIndex, emotionOpts, labelStyle);

            var filtered = script.voiceList.AsEnumerable();
            if (genderIndex > 0) filtered = filtered.Where(v => v.genderTag == genderOpts[genderIndex]);
            if (ageIndex > 0) filtered = filtered.Where(v => v.ageTag == ageOpts[ageIndex]);
            if (pitchIndex > 0) filtered = filtered.Where(v => v.pitchTag == pitchOpts[pitchIndex]);
            if (toneIndex > 0) filtered = filtered.Where(v => v.toneTag == toneOpts[toneIndex]);
            if (emotionIndex == 1) filtered = filtered.Where(v => v.emotion == "Happy");
            else if (emotionIndex == 2) filtered = filtered.Where(v => v.emotion == "Sad");
            else if (emotionIndex == 3) filtered = filtered.Where(v => v.emotion == "Angry");
            else if (emotionIndex == 4) filtered = filtered.Where(v => v.emotion == "Neutral");
            if (!string.IsNullOrEmpty(searchKeyword)) filtered = filtered.Where(v => v.speaker_name.Contains(searchKeyword) || v.styleTag.Contains(searchKeyword));

            List<VarcoVoiceManager.VoiceData> resultList = filtered.ToList();

            GUILayout.Space(10);

            if (resultList.Count == 0)
            {
                EditorGUILayout.HelpBox("조건에 맞는 성우가 없습니다.", MessageType.Warning);
            }
            else
            {
                int currentIndex = -1;
                string currentUuid = (script.voiceList.Count > script.selectedVoiceIndex) ? script.voiceList[script.selectedVoiceIndex].speaker_uuid : "";
                for(int i=0; i<resultList.Count; i++) { if(resultList[i].speaker_uuid == currentUuid) { currentIndex = i; break; } }
                
                string[] displayNames = resultList.Select(v => $"[{v.speaker_name}] {v.styleTag}").ToArray();
                EditorGUILayout.LabelField($"검색 결과: {resultList.Count}명", EditorStyles.miniLabel);
                
                int newIndex = EditorGUILayout.Popup(currentIndex, displayNames);
                if (newIndex != currentIndex && newIndex >= 0)
                {
                    string selectedUuid = resultList[newIndex].speaker_uuid;
                    script.selectedVoiceIndex = script.voiceList.FindIndex(v => v.speaker_uuid == selectedUuid);
                    GUI.FocusControl(null); 
                }

                if (newIndex >= 0)
                {
                    var v = resultList[newIndex];
                    GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
                    GUILayout.BeginVertical("helpbox");
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"🎙️ {v.speaker_name}", EditorStyles.boldLabel);
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
        EditorGUILayout.LabelField("🌐 언어 선택 (Language)", headerStyle);
        
        languageIndex = System.Array.IndexOf(languageValues, script.language);
        if (languageIndex < 0) languageIndex = 0;
        int newLangIndex = GUILayout.Toolbar(languageIndex, languageDisplayOpts, GUILayout.Height(30));
        if (newLangIndex != languageIndex) { languageIndex = newLangIndex; script.language = languageValues[languageIndex]; }

        GUILayout.Space(5);
        EditorGUILayout.LabelField("📝 대사 입력", headerStyle);
        script.textToSpeak = EditorGUILayout.TextArea(script.textToSpeak, GUILayout.Height(60));
        GUILayout.EndVertical();

        GUILayout.Space(5);

        GUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("🎚️ 세부 설정 (Details)", headerStyle);

        GUILayout.BeginHorizontal();
        script.speed = EditorGUILayout.Slider("속도", script.speed, 0.5f, 1.5f);
        if (GUILayout.Button("↺", GUILayout.Width(25))) { script.speed = 1.0f; GUI.FocusControl(null); }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        script.pitch = EditorGUILayout.Slider("높낮이", script.pitch, 0.5f, 1.5f);
        if (GUILayout.Button("↺", GUILayout.Width(25))) { script.pitch = 1.0f; GUI.FocusControl(null); }
        GUILayout.EndHorizontal();

        script.qualitySteps = EditorGUILayout.IntSlider("품질", script.qualitySteps, 8, 20);
        
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox("💡 Tip: 마음에 드는 목소리가 나왔다면 '고정하기'를 누르세요.\nSeed를 고정하면 언제든 똑같은 연기톤으로 재생성할 수 있습니다.", MessageType.Info);

        GUILayout.BeginHorizontal();
        script.seed = EditorGUILayout.IntField("Seed", script.seed);
        if(script.seed == -1) 
        {
            GUI.contentColor = Color.yellow;
            GUILayout.Label("🎲 Random", GUILayout.Width(80));
            GUI.contentColor = Color.white;
        }
        else
        {
            if(GUILayout.Button("Reset", GUILayout.Width(60))) { script.seed = -1; GUI.FocusControl(null); }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal("helpbox");
        EditorGUILayout.LabelField($"📢 방금 Seed: {script.lastUsedSeed}", EditorStyles.label);
        if (GUILayout.Button("고정하기", GUILayout.Width(80)))
        {
            script.seed = script.lastUsedSeed;
            GUI.FocusControl(null); 
            Debug.Log($"✅ Seed가 {script.seed}번으로 고정되었습니다!");
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.Space(15);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("▶ 미리듣기", GUILayout.Height(40))) script.GenerateVoice(isPreview: true);
        
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.5f); 
        if (GUILayout.Button("🎙️ 파일 생성 및 저장", bigBtnStyle)) script.GenerateVoice(isPreview: false);
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
        
        EditorGUILayout.HelpBox("파일은 'Assets/VarcoOutput' 폴더에 저장됩니다.", MessageType.None);
        
        if (GUI.changed) EditorUtility.SetDirty(script);
    }

    private void DrawFilterRow(string label, ref int index, string[] options, GUIStyle labelStyle)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, labelStyle, GUILayout.Height(20)); 
        index = GUILayout.Toolbar(index, options); 
        GUILayout.EndHorizontal();
    }
}
