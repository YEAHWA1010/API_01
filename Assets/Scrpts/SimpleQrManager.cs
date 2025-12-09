using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZXing;
using ZXing.QrCode;
using ZXing.Common;

public class SimpleQrManager : MonoBehaviour
{
    [Header("입력 UI (TMP)")]
    public TMP_InputField nameInputForQr;
    public TMP_InputField ageInputForQr;

    [Header("QR 표시용")]
    public RawImage qrRawImage;

    [Header("결과 표시용")]
    public TMP_Text qrStatusText;

    private Texture2D qrTexture;

    // PlayerPrefs 키 (UserProfileManager와 공유)
    private const string PlayerNameKey = "UserName";
    private const string PlayerAgeKey = "UserAge";

    private void Start()
    {
        // 저장된 프로필이 있으면 기본값으로 채워줌
        if (nameInputForQr != null && PlayerPrefs.HasKey(PlayerNameKey))
        {
            nameInputForQr.text = PlayerPrefs.GetString(PlayerNameKey);
        }

        if (ageInputForQr != null && PlayerPrefs.HasKey(PlayerAgeKey))
        {
            int savedAge = PlayerPrefs.GetInt(PlayerAgeKey);
            ageInputForQr.text = savedAge.ToString();
        }
    }

    // "QR 생성" 버튼에 연결
    public void OnClickGenerateQr()
    {
        if (nameInputForQr == null || ageInputForQr == null)
            return;

        string name = nameInputForQr.text;
        string ageStr = ageInputForQr.text;

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(ageStr))
        {
            if (qrStatusText != null)
                qrStatusText.text = "이름과 나이를 입력하세요.";
            return;
        }

        // 아주 단순한 인코딩 포맷: "name=샤인;age=25"
        string payload = "name=" + name + ";age=" + ageStr;

        int size = 256;
        qrTexture = GenerateQrTexture(payload, size, size);

        if (qrRawImage != null && qrTexture != null)
        {
            qrRawImage.texture = qrTexture;
        }

        if (qrStatusText != null)
            qrStatusText.text = "QR 생성 완료.";
    }

    // "QR 해독" 버튼에 연결
    public void OnClickDecodeQr()
    {
        if (qrTexture == null)
        {
            if (qrStatusText != null)
                qrStatusText.text = "먼저 QR을 생성하세요.";
            return;
        }

        string decoded = DecodeQrTexture(qrTexture);

        if (string.IsNullOrEmpty(decoded))
        {
            if (qrStatusText != null)
                qrStatusText.text = "QR 해독 실패.";
            return;
        }

        // "name=샤인;age=25" 문자열에서 다시 이름/나이 추출
        string name = "";
        string age = "";

        string[] parts = decoded.Split(';');
        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i];
            if (part.StartsWith("name="))
            {
                name = part.Substring("name=".Length);
            }
            else if (part.StartsWith("age="))
            {
                age = part.Substring("age=".Length);
            }
        }

        if (qrStatusText != null)
        {
            qrStatusText.text =
                "이름: " + name + "\n" +
                "나이: " + age;
        }
    }

    private Texture2D GenerateQrTexture(string text, int width, int height)
    {
        QRCodeWriter qrWriter = new QRCodeWriter();
        BitMatrix matrix = qrWriter.encode(text, BarcodeFormat.QR_CODE, width, height);

        Color32[] pixels = new Color32[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isBlack = matrix[x, y];
                byte colorValue = (byte)(isBlack ? 0 : 255);
                Color32 color = new Color32(colorValue, colorValue, colorValue, 255);
                int index = y * width + x;
                pixels[index] = color;
            }
        }

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        tex.Apply();

        return tex;
    }

    private string DecodeQrTexture(Texture2D tex)
    {
        Color32[] pixels = tex.GetPixels32();
        int width = tex.width;
        int height = tex.height;

        BarcodeReader reader = new BarcodeReader();
        Result result = reader.Decode(pixels, width, height);

        if (result != null)
        {
            return result.Text;
        }

        return null;
    }

    public Texture2D GetCurrentQrTexture()
    {
        return qrTexture;
    }
}
