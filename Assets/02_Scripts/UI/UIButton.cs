using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 유니티 버튼 컴포넌트를 기반으로 특정 객체(root)에 메시지를 전달하는 UI 클래스입니다.
/// 버튼 클릭 시 설정된 콜백 메서드를 실행합니다.
/// </summary>
public class UIButton : MonoBehaviour
{
    /// <summary> 메시지를 수신할 대상 스크립트 </summary>
    [SerializeField] 
    private MonoBehaviour _root;

    /// <summary> 유니티 버튼 컴포넌트 </summary>
    [SerializeField] 
    private Button _unityButton;

    /// <summary> 실행할 콜백 메서드의 이름 </summary>
    [SerializeField] 
    private string _callbackName;

    /// <summary> 콜백 메서드에 전달할 인자값 (선택 사항) </summary>
    [SerializeField] 
    private string _callbackValue;

    /// <summary>
    /// 버튼 클릭 이벤트가 발생했을 때 호출되어 root 객체에 메시지를 전송합니다.
    /// </summary>
    public void OnClickEvent()
    {
        if (_root == null || _unityButton == null || string.IsNullOrEmpty(_callbackName))
        {
            Debug.LogError($"UIButton_{gameObject.name}: Root, Unity Button, or Callback Name is not set.");
            return;
        }

        // 인자값 유무에 따라 SendMessage 호출 분기 (로직 보존)
        if (!string.IsNullOrEmpty(_callbackValue))
        {
            _root.SendMessage(_callbackName, _callbackValue);
        }
        else
        {
            _root.SendMessage(_callbackName);
        }
    }
}
