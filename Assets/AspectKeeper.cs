using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AspectKeeper : MonoBehaviour
{
    // 基準解像度（横画面用）
    // デザインの基準に合わせて変更してください（例: 1920×1080, 2340×1080 など）
    [SerializeField] private float targetWidth = 1920f;
    [SerializeField] private float targetHeight = 1080f;

    private Camera cam;
    private float initialOrthographicSize; // 元のサイズを覚えておく

    void Awake()
    {
        cam = GetComponent<Camera>();
        initialOrthographicSize = cam.orthographicSize; // 初期値を保存
        UpdateCameraSize();
    }

    void Start()
    {
        // 念のためもう一度（端末によってはAwake時点でScreenが正しい値でない場合がある）
        UpdateCameraSize();
    }

    // 画面サイズが変わった時にも対応（実機での回転やマルチウィンドウ対策）
    void OnRectTransformDimensionsChange()
    {
        UpdateCameraSize();
    }

    void UpdateCameraSize()
    {
        if (cam == null || !cam.orthographic) return;

        float targetAspect = targetWidth / targetHeight;
        float currentAspect = (float)Screen.width / Screen.height;

        // 基準より「縦長」（アスペクト比が小さい）場合 → 縦が余るのでOrthographic Sizeを拡大
        // 基準より「横長」の場合はそのまま（左右に余白が出る想定）
        if (currentAspect < targetAspect)
        {
            cam.orthographicSize = initialOrthographicSize * (targetAspect / currentAspect);
        }
        else
        {
            cam.orthographicSize = initialOrthographicSize;
        }
    }
}
