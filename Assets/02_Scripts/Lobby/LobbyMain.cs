using Puzzle.Core;
using UnityEngine;

/// <summary>
/// 로비 씬의 메인 로직을 담당하는 클래스입니다.
/// </summary>
public class LobbyMain : MonoBehaviour
{

    /// <summary>
    /// 스테이지 시작 버튼 클릭 시 호출되며, 데이터를 준비하고 게임 씬으로 이동합니다.
    /// </summary>
    public void OnClickStartStage()
    {
        // string rulePath = "LinkMatchRule";
        // string stagePath = "Stage";

        // StageInjection.Instance.MakeGameSpec(rulePath, stagePath);
        
        // GameSpec spec = StageInjection.Instance.GetGameSpec();
        // if (spec != null && 
        //         string.IsNullOrEmpty(spec.rule.ruleId) == false && 
        //             spec.stageData != null)
        // {
        //     Main.Instance.MoveScene(SceneEnum.LobbyScene,SceneEnum.GameScene);
        // }
        // else
        // {
        //     Debug.LogError("게임 씬으로 이동하기 전 GameSpec 준비에 실패했습니다.");
        // }
        DomainManager.Instance.OpenPopup("PopupReady");
    }
}
