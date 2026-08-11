using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AspectKeeper : MonoBehaviour
{
    // Unity上で基準（ピッタリに見えている）にした解像度の幅と高さ
    [SerializeField] private float targetWidth = 1080f;
    [SerializeField] private float targetHeight = 1920f;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        UpdateCameraSize();
    }

    void UpdateCameraSize()
    {
        // 基準とするアスペクト比と、現在の画面のアスペクト比を計算
        float targetAspect = targetWidth / targetHeight;
        float currentAspect = (float)Screen.width / Screen.height;

        // 端末が基準より細長ければ、横幅が収まるようにカメラのOrthographic Sizeを拡大する
        if (currentAspect < targetAspect)
        {
            cam.orthographicSize = cam.orthographicSize * (targetAspect / currentAspect);
        }
    }
}
