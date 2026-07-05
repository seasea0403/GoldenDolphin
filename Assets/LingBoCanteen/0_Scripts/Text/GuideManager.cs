using UnityEngine;
using System;

/// <summary>
/// 新手引导管理器
/// 做完调 Continue
/// </summary>
public class GuideManager : MonoBehaviour
{
    // ========== 场景UI ==========
    // public GameObject recipeBtn;
    // public GameObject prepArea;
    // public GameObject cookingArea;
    // public GameObject potSelectBtn;
    // public GameObject placemat;
    // public GameObject bellBtn;
    // public GameObject patienceBar;

    private void OnEnable()
    {
        TextManager.OnGuideCommand += HandleCommand;
    }

    private void OnDisable()
    {
        TextManager.OnGuideCommand -= HandleCommand;
    }

    /// <summary>
    /// 收到指令后分发处理
    /// </summary>
    void HandleCommand(string cmd)
    {
        Debug.Log("[Guide] 收到指令：" + cmd);

        // ========== 判断逻辑 ==========

        if (cmd.Contains("显化"))
        {
            // 举例：显化耐心值
            // 显化完自动继续（或者等玩家点确定后再调 Continue）
            Invoke("Continue", 1f);
        }
        else if (cmd.Contains("引导点击"))
        {
            string targetName = cmd.Replace("引导点击", "").Trim();
            Debug.Log("[Guide] 需要引导玩家点击：" + targetName);

            // 玩家点完之后，在点击事件里调 Continue()
        }
        else
        {
            // 不认识的指令直接继续
            Continue();
        }
    }

    /// <summary>
    /// 引导完成，通知剧情继续
    /// </summary>
    void Continue()
    {
        Debug.Log("[Guide] 引导完成，恢复剧情");
        TextManager.Instance.ResumeFromGuide();
    }
}