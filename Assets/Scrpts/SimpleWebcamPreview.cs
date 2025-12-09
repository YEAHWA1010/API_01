using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleWebcamPreview : MonoBehaviour
{
    [Header("출력 UI")]
    public RawImage webcamRawImage;
    public TMP_Text webcamStatusText;

    private WebCamTexture webcamTexture;

    // "웹캠 시작" 버튼에 연결
    public void OnClickStartWebcam()
    {
        // 이미 실행 중이면 무시
        if (webcamTexture != null && webcamTexture.isPlaying)
            return;

        if (WebCamTexture.devices.Length == 0)
        {
            if (webcamStatusText != null)
                webcamStatusText.text = "웹캠을 찾을 수 없습니다.";
            Debug.LogError("No webcam devices found.");
            return;
        }

        // 첫 번째 카메라 사용
        WebCamDevice device = WebCamTexture.devices[0];
        webcamTexture = new WebCamTexture(device.name);

        if (webcamRawImage != null)
        {
            webcamRawImage.texture = webcamTexture;
            webcamRawImage.material.mainTexture = webcamTexture;
        }

        webcamTexture.Play();

        if (webcamStatusText != null)
            webcamStatusText.text = "웹캠 ON: " + device.name;
    }

    // "웹캠 종료" 버튼에 연결
    public void OnClickStopWebcam()
    {
        if (webcamTexture != null)
        {
            if (webcamTexture.isPlaying)
            {
                webcamTexture.Stop();
            }

            if (webcamRawImage != null)
            {
                webcamRawImage.texture = null;
            }

            if (webcamStatusText != null)
                webcamStatusText.text = "웹캠 OFF";
        }
    }

    private void OnDestroy()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
    }

    public Texture2D CaptureSnapshot()
    {
        if (webcamTexture == null || !webcamTexture.isPlaying)
        {
            Debug.LogWarning("CaptureSnapshot 호출: 웹캠이 켜져 있지 않습니다.");
            return null;
        }

        Texture2D tex = new Texture2D(
            webcamTexture.width,
            webcamTexture.height,
            TextureFormat.RGBA32,
            false
        );

        tex.SetPixels32(webcamTexture.GetPixels32());
        tex.Apply();
        return tex;
    }
}
