using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

[Serializable]
public class GeminiRequestPart
{
    public string text;
}

[Serializable]
public class GeminiRequestContent
{
    public GeminiRequestPart[] parts;
}

[Serializable]
public class GeminiRequest
{
    public GeminiRequestContent[] contents;
}

[Serializable]
public class GeminiInlineData
{
    public string mimeType;   // "image/png" 등
    public string data;       // base64 인코딩된 이미지 (지금은 사용 안 함)
}

[Serializable]
public class GeminiResponsePart
{
    public string text;
    public GeminiInlineData inlineData;
}

[Serializable]
public class GeminiResponseContent
{
    public GeminiResponsePart[] parts;
}

[Serializable]
public class GeminiCandidate
{
    public GeminiResponseContent content;
}

[Serializable]
public class GeminiResponse
{
    public GeminiCandidate[] candidates;
}

public class GeminiChatClient : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField inputField;   // 유저 입력창
    public TMP_Text chatLogText;        // 메인 채팅 로그용 TMP_Text (ScrollView 안)
    
    [Header("History ScrollView")]
    public TMP_Text historyText;        // 저장된 채팅을 보여줄 TMP_Text (다른 ScrollView 안)

    [Header("Buttons")]
    public Button normalChatButton;     // 기본 대화 버튼
    public Button translateButton;      // 영어 번역 버튼
    public Button saveAndLoadButton;    // 지금까지 채팅 저장 + 히스토리 뷰에 로드 버튼

    [Header("Gemini Settings")]
    [SerializeField] private string apiKey = "YOUR_GEMINI_API_KEY";
    [SerializeField] private string textModelName = "gemini-2.5-flash";

    private bool _isRequesting = false;  // 요청 중일 때 중복 입력 막기

    // 텍스트 모델 엔드포인트
    private string TextEndpoint =>
        $"https://generativelanguage.googleapis.com/v1beta/models/{textModelName}:generateContent";

    // 채팅 기록 저장 경로 (로컬 파일)
    private string HistoryFilePath =>
        Path.Combine(Application.persistentDataPath, "gemini_chat_history.txt");

    private void Awake()
    {
        if (normalChatButton != null)
            normalChatButton.onClick.AddListener(OnClickNormalChat);

        if (translateButton != null)
            translateButton.onClick.AddListener(OnClickTranslate);

        if (saveAndLoadButton != null)
            saveAndLoadButton.onClick.AddListener(OnClickSaveAndLoadHistory);
    }

    private void OnDestroy()
    {
        if (normalChatButton != null)
            normalChatButton.onClick.RemoveListener(OnClickNormalChat);

        if (translateButton != null)
            translateButton.onClick.RemoveListener(OnClickTranslate);

        if (saveAndLoadButton != null)
            saveAndLoadButton.onClick.RemoveListener(OnClickSaveAndLoadHistory);
    }

    // (1) 기본 대화 버튼
    private void OnClickNormalChat()
    {
        if (_isRequesting) return;
        if (inputField == null || chatLogText == null) return;

        string userText = inputField.text;
        if (string.IsNullOrWhiteSpace(userText))
            return;

        AppendLog($"[You]\n{userText}\n");
        inputField.text = string.Empty;

        StartCoroutine(SendTextRequestInternal(userText, translateToEnglish: false));
    }

    // (2) 영어 번역 버튼
    private void OnClickTranslate()
    {
        if (_isRequesting) return;
        if (inputField == null || chatLogText == null) return;

        string userText = inputField.text;
        if (string.IsNullOrWhiteSpace(userText))
            return;

        AppendLog($"[You-Translate]\n{userText}\n");
        inputField.text = string.Empty;

        StartCoroutine(SendTextRequestInternal(userText, translateToEnglish: true));
    }

    // (3) 채팅 히스토리 저장 + 로드 버튼
    private void OnClickSaveAndLoadHistory()
    {
        // 1) 현재 chatLogText 내용을 파일로 저장
        SaveChatHistory();

        // 2) 파일에서 읽어서 historyText에 표시
        LoadChatHistoryToHistoryView();
    }

    // 메인 로그에 한 줄 추가
    private void AppendLog(string text)
    {
        if (chatLogText == null) return;
        chatLogText.text += text + "\n";
    }

    private IEnumerator SendTextRequestInternal(string userMessage, bool translateToEnglish)
    {
        _isRequesting = true;

        // 6-1) 프롬프트 구성
        string finalPrompt = BuildTextPrompt(userMessage, translateToEnglish);

        // 6-2) 요청 JSON 만들기
        var part = new GeminiRequestPart { text = finalPrompt };
        var content = new GeminiRequestContent
        {
            parts = new[] { part }
        };
        var req = new GeminiRequest
        {
            contents = new[] { content }
        };

        string json = JsonUtility.ToJson(req);
        Debug.Log("Gemini Text Request JSON: " + json);

        // 6-3) UnityWebRequest 설정
        using (var request = new UnityWebRequest(TextEndpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-goog-api-key", apiKey);

            // 6-4) 전송
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Gemini Text Error: {request.result}, {request.error}, {request.downloadHandler.text}");
                AppendLog($"[Error]\n{request.error}\n{request.downloadHandler.text}");
            }
            else
            {
                string responseJson = request.downloadHandler.text;
                Debug.Log("Gemini Text Response JSON: " + responseJson);

                GeminiResponse res = JsonUtility.FromJson<GeminiResponse>(responseJson);

                string answer = ExtractFirstText(res);
                if (!string.IsNullOrEmpty(answer))
                {
                    AppendLog($"[Gemini]\n{answer}\n");
                }
                else
                {
                    AppendLog("[Gemini]\n텍스트 응답 파싱 실패\nRaw: " + responseJson);
                }
            }
        }

        _isRequesting = false;
    }

    // 프롬프트 만들기
    private string BuildTextPrompt(string userMessage, bool translateToEnglish)
    {
        if (!translateToEnglish)
        {
            // 일반 대화 모드: 그대로 보내기
            return userMessage;
        }

        // 영어 번역 모드
        return
            "You are a professional English translator. " +
            "Translate the following sentence into natural and fluent English. " +
            "Only output the translated English sentence, no explanations.\n\n" +
            "Sentence:\n" + userMessage;
    }

    // 응답에서 첫 번째 text만 뽑기
    private string ExtractFirstText(GeminiResponse res)
    {
        if (res == null || res.candidates == null || res.candidates.Length == 0)
            return null;

        var cand = res.candidates[0];
        if (cand.content == null || cand.content.parts == null)
            return null;

        foreach (var p in cand.content.parts)
        {
            if (!string.IsNullOrEmpty(p.text))
                return p.text;
        }

        return null;
    }

    // (1) 현재 chatLogText 내용을 파일로 저장
    private void SaveChatHistory()
    {
        if (chatLogText == null) return;

        try
        {
            string content = chatLogText.text;
            File.WriteAllText(HistoryFilePath, content, Encoding.UTF8);
            Debug.Log($"Chat history saved to: {HistoryFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveChatHistory Error: {e.Message}");
            AppendLog($"[Error]\n히스토리 저장 중 오류: {e.Message}\n");
        }
    }

    // (2) 파일에서 읽어서 historyText에 로드
    private void LoadChatHistoryToHistoryView()
    {
        if (historyText == null)
        {
            Debug.LogWarning("HistoryText is not assigned.");
            return;
        }

        try
        {
            if (File.Exists(HistoryFilePath))
            {
                string content = File.ReadAllText(HistoryFilePath, Encoding.UTF8);
                historyText.text = content;
                Debug.Log($"Chat history loaded into history view from: {HistoryFilePath}");
            }
            else
            {
                historyText.text = "[System]\n저장된 채팅 기록이 없습니다.\n";
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"LoadChatHistory Error: {e.Message}");
            historyText.text = $"[Error]\n히스토리 로드 중 오류: {e.Message}\n";
        }
    }
}
