using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FinalProfileManager : MonoBehaviour
{
    [Header("참조할 매니저들")]
    public UserProfileManager userProfileManager;
    public SimpleWebcamPreview webcamPreview;
    public SimpleQrManager qrManager;

    [Header("패널들")]
    public GameObject[] panelsToHide;   // 기존 입력/웹캠/QR 패널들
    public GameObject finalPanel;       // 최종 프로필 패널

    [Header("최종 출력 UI")]
    public TMP_Text finalNameText;
    public TMP_Text finalAgeText;
    public RawImage finalPhotoImage;
    public RawImage finalQrImage;
    public TMP_Text finalStatusText;

    private const string PlayerNameKey = "UserName";
    private const string PlayerAgeKey = "UserAge";

    // "최종 출력" 버튼 OnClick에 연결
    public void OnClickShowFinalProfile()
    {
        // 1. 저장된 이름/나이 읽기 (UserProfileManager와 같은 키 사용)
        string name = PlayerPrefs.GetString(PlayerNameKey, "");
        int age = PlayerPrefs.GetInt(PlayerAgeKey, -1);

        // 2. 현재 웹캠에서 스냅샷 한 장 가져오기
        Texture2D photoTexture = null;
        if (webcamPreview != null)
        {
            photoTexture = webcamPreview.CaptureSnapshot();
        }

        // 3. 현재 QR 텍스처 가져오기
        Texture2D qrTexture = null;
        if (qrManager != null)
        {
            qrTexture = qrManager.GetCurrentQrTexture();
        }

        // 4. 기존 패널 비활성화
        if (panelsToHide != null)
        {
            foreach (GameObject panel in panelsToHide)
            {
                if (panel != null)
                {
                    panel.SetActive(false);
                }
            }
        }

        // 5. 최종 패널 활성화
        if (finalPanel != null)
        {
            finalPanel.SetActive(true);
        }

        // 6. 최종 패널에 값 채우기
        if (finalNameText != null)
        {
            finalNameText.text = string.IsNullOrEmpty(name) ? "이름 정보 없음" : name;
        }

        if (finalAgeText != null)
        {
            finalAgeText.text = (age < 0) ? "나이 정보 없음" : age.ToString();
        }

        if (finalPhotoImage != null)
        {
            if (photoTexture != null)
            {
                finalPhotoImage.texture = photoTexture;
            }
            else
            {
                finalPhotoImage.texture = null;   // 이미지 없음
            }
        }

        if (finalQrImage != null)
        {
            if (qrTexture != null)
            {
                finalQrImage.texture = qrTexture;
            }
            else
            {
                finalQrImage.texture = null;
            }
        }

        // 7. 상태 메시지 (어떤 데이터가 빠졌는지 알려주기)
        if (finalStatusText != null)
        {
            string status = "";

            if (string.IsNullOrEmpty(name))
            {
                status += "이름이 저장되어 있지 않습니다.\n";
            }
            if (age < 0)
            {
                status += "나이가 저장되어 있지 않습니다.\n";
            }
            if (photoTexture == null)
            {
                status += "웹캠 스냅샷이 없습니다. 웹캠을 켜고 다시 시도하세요.\n";
            }
            if (qrTexture == null)
            {
                status += "QR 코드가 생성되어 있지 않습니다.\n";
            }

            if (string.IsNullOrEmpty(status))
            {
                status = "모든 데이터가 준비되었습니다.";
            }

            finalStatusText.text = status;
        }
    }
}
