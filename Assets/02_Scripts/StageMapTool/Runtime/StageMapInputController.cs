using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 스테이지 맵 툴의 포인터 입력을 그리드 페인팅 요청으로 변환합니다.
/// </summary>
public class StageMapInputController : MonoBehaviour
{
    /// <summary> 입력을 전달할 툴 컨트롤러입니다. </summary>
    private StageMapToolController _controller;

    /// <summary> 좌표 변환에 사용할 그리드 뷰입니다. </summary>
    private StageMapGridView _gridView;

    /// <summary> 입력에 사용할 카메라입니다. </summary>
    private Camera _camera;

    /// <summary>
    /// 입력 컨트롤러 참조를 초기화합니다.
    /// </summary>
    /// <param name="controller">입력을 전달할 툴 컨트롤러입니다.</param>
    /// <param name="gridView">좌표 변환에 사용할 그리드 뷰입니다.</param>
    public void Initialize(StageMapToolController controller, StageMapGridView gridView)
    {
        _controller = controller;
        _gridView = gridView;
        ResolveSharedCamera();
    }

    /// <summary>
    /// 매 프레임 포인터 입력을 처리합니다.
    /// </summary>
    private void Update()
    {
        ResolveSharedCamera();

        if (_controller == null || _gridView == null)
        {
            return;
        }

        if (!IsPointerHeld())
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (_camera == null)
        {
            return;
        }

        Vector3 worldPosition = _camera.ScreenToWorldPoint(GetPointerPosition());
        if (_gridView.TryWorldToGrid(worldPosition, out int x, out int y))
        {
            _controller.PaintCell(x, y);
        }
    }

    /// <summary>
    /// SharedScene의 공용 카메라를 찾아 캐시합니다.
    /// </summary>
    /// <returns>공용 카메라를 찾았으면 true입니다.</returns>
    private bool ResolveSharedCamera()
    {
        if (_camera != null)
        {
            return true;
        }

        _camera = CameraController.MainCamera;
        return _camera != null;
    }

    /// <summary>
    /// 현재 포인터 입력이 유지 중인지 확인합니다.
    /// </summary>
    /// <returns>마우스 또는 터치 입력이 유지 중이면 true입니다.</returns>
    private bool IsPointerHeld()
    {
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            return true;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 현재 포인터의 화면 좌표를 반환합니다.
    /// </summary>
    /// <returns>현재 포인터 화면 좌표입니다.</returns>
    private Vector2 GetPointerPosition()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }

        return Vector2.zero;
    }
}
