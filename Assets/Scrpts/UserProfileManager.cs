using UnityEngine;
using TMPro;

public class UserProfileManager : MonoBehaviour
{
    [Header("입력 UI")]
    public TMP_InputField nameInput;
    public TMP_InputField ageInput;

    [Header("상태 표시")]
    public TMP_Text statusText;

    // PlayerPrefs 키
    private const string PlayerNameKey = "UserName";
    private const string PlayerAgeKey = "UserAge";

    private void Start()
    {
        // 저장된 이름 불러오기
        if (nameInput != null && PlayerPrefs.HasKey(PlayerNameKey))
        {
            string savedName = PlayerPrefs.GetString(PlayerNameKey);
            nameInput.text = savedName;
        }

        // 저장된 나이 불러오기
        if (ageInput != null && PlayerPrefs.HasKey(PlayerAgeKey))
        {
            int savedAge = PlayerPrefs.GetInt(PlayerAgeKey);
            ageInput.text = savedAge.ToString();
        }

        if (statusText != null)
        {
            statusText.text = "프로필 생성!";
        }
    }

    // 버튼 OnClick에 연결해서 사용
    public void OnClickSaveProfile()
    {
        if (nameInput == null || ageInput == null)
            return;

        string name = nameInput.text;
        string ageStr = ageInput.text;

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(ageStr))
        {
            if (statusText != null)
                statusText.text = "이름과 나이를 모두 입력하세요.";
            return;
        }

        int age;
        if (!int.TryParse(ageStr, out age))
        {
            if (statusText != null)
                statusText.text = "나이는 숫자로 입력해야 합니다.";
            return;
        }

        PlayerPrefs.SetString(PlayerNameKey, name);
        PlayerPrefs.SetInt(PlayerAgeKey, age);
        PlayerPrefs.Save();

        if (statusText != null)
        {
            statusText.text = "프로필 저장 완료.";
        }
    }
}
