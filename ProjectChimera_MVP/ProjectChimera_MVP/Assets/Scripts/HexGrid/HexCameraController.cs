using UnityEngine;

/// <summary>
/// 六边形地图摄像机控制器 — WASD/方向键平移 + 鼠标滚轮缩放
/// </summary>
[RequireComponent(typeof(Camera))]
public class HexCameraController : MonoBehaviour
{
    [Header("移动")]
    public float moveSpeed = 15f;
    public float smoothTime = 0.15f;

    [Header("缩放")]
    public float zoomSpeed = 5f;
    public float minZoom = 3f;
    public float maxZoom = 25f;

    [Header("边界（世界坐标）")]
    public float minX = -50f;
    public float maxX = 50f;
    public float minY = -50f;
    public float maxY = 50f;

    Camera _cam;
    Vector3 _targetPos;
    float _targetZoom;
    Vector3 _velocity;

    void Start()
    {
        _cam = GetComponent<Camera>();
        _cam.orthographic = true;
        _targetPos = transform.position;
        _targetZoom = _cam.orthographicSize;
    }

    void Update()
    {
        HandleMovement();
        HandleZoom();
        ApplySmooth();
    }

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right
        float v = Input.GetAxisRaw("Vertical");   // W/S or Up/Down

        Vector3 dir = new Vector3(h, v, 0f).normalized;
        _targetPos += dir * moveSpeed * Time.deltaTime;

        // Clamp to bounds
        _targetPos.x = Mathf.Clamp(_targetPos.x, minX, maxX);
        _targetPos.y = Mathf.Clamp(_targetPos.y, minY, maxY);
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            _targetZoom -= scroll * zoomSpeed;
            _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
        }
    }

    void ApplySmooth()
    {
        transform.position = Vector3.SmoothDamp(transform.position, _targetPos, ref _velocity, smoothTime);
        _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetZoom, Time.deltaTime / smoothTime);
    }

    /// <summary>设置地图边界（由 HexWorldMap 调用）</summary>
    public void SetBounds(float xMin, float xMax, float yMin, float yMax)
    {
        minX = xMin;
        maxX = xMax;
        minY = yMin;
        maxY = yMax;
    }
}
