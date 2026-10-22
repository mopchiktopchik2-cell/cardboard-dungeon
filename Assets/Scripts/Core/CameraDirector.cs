using UnityEngine;

public class CameraDirector : MonoBehaviour
{
    public Camera Cam;
    public Vector3 BasePosition = new Vector3(0f, 0.15f, -10f);
    public float BaseSize = 5.35f;

    private float shakeAmount;
    private float shakeTime;
    private float shakeDuration;
    private float zoom = 1f;
    private float zoomTarget = 1f;
    private Vector3 focusOffset;
    private Vector3 focusTarget;
    private float focusBlend;
    private float seed;

    public static CameraDirector Create()
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        var cam = go.AddComponent<Camera>();
        var director = go.AddComponent<CameraDirector>();
        director.Cam = cam;
        go.AddComponent<AudioListener>();
        return director;
    }

    public void Init()
    {
        Cam.orthographic = true;
        Cam.orthographicSize = BaseSize;
        Cam.clearFlags = CameraClearFlags.SolidColor;
        Cam.backgroundColor = new Color32(9, 7, 14, 255);
        Cam.nearClipPlane = 0.1f;
        Cam.farClipPlane = 100f;
        Cam.transform.position = BasePosition;
        seed = Random.value * 10f;
        Fit();
    }

    private void LateUpdate()
    {
        Fit();
        zoom = Mathf.Lerp(zoom, zoomTarget, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
        focusOffset = Vector3.Lerp(focusOffset, focusTarget, 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime));
        Vector3 pos = BasePosition + focusOffset;
        if (shakeTime > 0f)
        {
            shakeTime -= Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(shakeTime / Mathf.Max(0.001f, shakeDuration));
            float n1 = Mathf.PerlinNoise(seed + Time.unscaledTime * 28f, 0.3f) * 2f - 1f;
            float n2 = Mathf.PerlinNoise(0.7f, seed + Time.unscaledTime * 31f) * 2f - 1f;
            pos += new Vector3(n1, n2, 0f) * shakeAmount * k;
        }
        transform.position = pos;
        Cam.orthographicSize = BaseSize * zoom;
    }

    private void Fit()
    {
        if (Cam == null) return;
        float aspect = Mathf.Max(0.5f, Cam.aspect);
        float size = Mathf.Max(BaseSize, 9.35f / aspect);
        Cam.orthographicSize = size * zoom;
    }

    public void Shake(float amount, float duration)
    {
        shakeAmount = Mathf.Max(shakeAmount, amount);
        shakeDuration = Mathf.Max(0.01f, duration);
        shakeTime = shakeDuration;
    }

    public void Zoom(float amount, float speed = 1f)
    {
        zoomTarget = amount;
        zoom = Mathf.Lerp(zoom, zoomTarget, Mathf.Clamp01(speed * 0.1f));
    }

    public void Focus(Vector3 offset)
    {
        focusTarget = offset;
    }

    public void ResetFocus()
    {
        focusTarget = Vector3.zero;
        focusOffset = Vector3.zero;
        zoomTarget = 1f;
    }
}
