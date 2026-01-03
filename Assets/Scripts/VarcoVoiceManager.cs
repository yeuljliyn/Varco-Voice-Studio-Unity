using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor; 
#endif

[RequireComponent(typeof(AudioSource))]
public class VarcoVoiceManager : MonoBehaviour
{
    [Header("🔐 API 설정")]
    public string apiKey = ""; 
    private const string BaseUrl = "https://openapi.ai.nc.com/tts/standard/v1/api";

    [HideInInspector] public int selectedVoiceIndex = 0;
    [HideInInspector] public List<VoiceData> voiceList = new List<VoiceData>();
    [HideInInspector] public string[] voiceNames;

    [Header("📝 대사 및 설정")]
    [TextArea(3, 5)] public string textToSpeak = "안녕하세요, 바르코 보이스입니다.";
    
    [HideInInspector] public string language = "korean";

    [Range(0.5f, 1.5f)] public float speed = 1.0f;
    [Range(0.5f, 1.5f)] public float pitch = 1.0f;
    [Range(8, 20)] public int qualitySteps = 20; 
    
    public int seed = -1; 
    [HideInInspector] public int lastUsedSeed = 0; 

    private AudioSource audioSource;

    void Start()
    {
        CheckAudioSource();
        if (audioSource != null) audioSource.playOnAwake = false;
    }

    private void CheckAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void FetchVoiceList()
    {
        if (string.IsNullOrEmpty(apiKey)) { Debug.LogError("⛔ API Key가 비어있습니다!"); return; }
        StartCoroutine(GetVoicesRoutine());
    }

    IEnumerator GetVoicesRoutine()
    {
        string url = $"{BaseUrl}/voices/varco";
        Debug.Log("🔄 성우 목록 갱신 중...");

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader("OPENAPI_KEY", apiKey);
            yield return www.SendWebRequest();

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
                voiceNames = voiceList.Select(v => $"{v.speaker_name} ({v.saas_name})").ToArray();
                Debug.Log($"✅ 성우 {voiceList.Count}명 로드 완료!");
            }
            else
            {
                Debug.LogError($"❌ 목록 갱신 실패: {www.error}\n응답: {www.downloadHandler.text}");
            }
        }
    }

    public void GenerateVoice(bool isPreview = false)
    {
        CheckAudioSource();
        if (voiceList == null || voiceList.Count == 0) 
        {
            Debug.LogError("⚠️ 성우 목록이 없습니다. [목록 갱신]을 먼저 해주세요.");
            return;
        }
        
        VoiceData currentVoice = voiceList[selectedVoiceIndex];
        string targetUuid = currentVoice.speaker_uuid;
        string fileSaveName = currentVoice.speaker_name; 

        string pureName = currentVoice.speaker_name.Split('(')[0].Trim();
        string displayName = pureName;

        switch (language)
        {
            case "japanese": displayName = HangulToKatakana(pureName); break;
            case "english": displayName = ConvertToRoman(pureName); break;
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
        
        if (!isPreview && language != "korean" && IsKorean(finalText))
        {
            Debug.LogWarning($"⚠️ 주의: 언어는 '{language}'인데 텍스트에 한글이 포함되어 있습니다. 서버 에러가 발생할 수 있습니다.");
        }

        StartCoroutine(PostRequest(targetUuid, finalText, isPreview, fileSaveName));
    }

    IEnumerator PostRequest(string uuid, string text, bool isPreview, string speakerName)
    {
        VarcoRequestData data = new VarcoRequestData();
        data.text = text; 
        data.voice = uuid; 
        data.language = language; 
        data.properties = new VoiceProperties { speed = speed, pitch = pitch };
        data.n_fm_steps = qualitySteps; 
        
        int actualSeed = seed;
        if (actualSeed == -1) actualSeed = UnityEngine.Random.Range(1, 999999); 
        data.seed = actualSeed;      
        lastUsedSeed = actualSeed;   

        string jsonBody = JsonUtility.ToJson(data);
        
        Debug.Log($"📤 [요청 데이터] {jsonBody}");

        using (UnityWebRequest www = new UnityWebRequest($"{BaseUrl}/synthesize", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("OPENAPI_KEY", apiKey);

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                VarcoResponse res = JsonUtility.FromJson<VarcoResponse>(www.downloadHandler.text);
                if (!string.IsNullOrEmpty(res.audio))
                {
                    byte[] bytes = Convert.FromBase64String(res.audio);
                    string tempPath = Path.Combine(Application.persistentDataPath, "temp_preview.wav");
                    File.WriteAllBytes(tempPath, bytes);
                    StartCoroutine(LoadAndPlay(tempPath));

                    if (!isPreview) SaveAudioFile(bytes, speakerName);
                    Debug.Log($"🎉 성공! (Seed: {lastUsedSeed})");
                }
            }
            else
            {
                Debug.LogError($"❌ 통신 에러: {www.downloadHandler.text}\n(설정된 언어: {language} / 보낸 텍스트: {text})");
            }
        }
    }

    private bool IsKorean(string str)
    {
        foreach (char c in str) if (c >= 0xAC00 && c <= 0xD7A3) return true;
        return false;
    }

    private string ConvertToRoman(string koreanName)
    {
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

    private string HangulToKatakana(string koreanName)
    {
        StringBuilder result = new StringBuilder();
        foreach (char c in koreanName) {
            if (c >= 0xAC00 && c <= 0xD7A3) {
                int code = c - 0xAC00;
                int cho = (code / 28) / 21; int jung = (code / 28) % 21; int jong = code % 28
                result.Append("・");
            } else result.Append(c);
        }
        return result.ToString();
    }

    void SaveAudioFile(byte[] bytes, string speakerName)
    {
#if UNITY_EDITOR
        string folderPath = Path.Combine(Application.dataPath, "VarcoOutput");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        string fileName = $"{speakerName}_Seed{lastUsedSeed}_{DateTime.Now:MMdd_HHmmss}.wav";
        string fullPath = Path.Combine(folderPath, fileName);
        File.WriteAllBytes(fullPath, bytes);
        AssetDatabase.Refresh();
        Debug.Log($"💾 저장됨: {fileName}");
#endif
    }

    IEnumerator LoadAndPlay(string path)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.WAV))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (audioSource != null) { audioSource.clip = clip; audioSource.Play(); }
            }
        }
    }

    [Serializable] public class VoiceListWrapper { public List<VoiceData> items; }
    [Serializable] public class VoiceData { public string speaker_uuid, speaker_name, saas_name, description, emotion, genderTag, ageTag, pitchTag, toneTag, styleTag; }
    [Serializable] public class VarcoRequestData { public string text, language, voice; public VoiceProperties properties; public int n_fm_steps, seed; }
    [Serializable] public class VoiceProperties { public float speed, pitch; }
    [Serializable] public class VarcoResponse { public string audio; }
}
