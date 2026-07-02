using GameFramework.Event;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 主菜单流程。
    /// 职责：打开主菜单场景和 UI，等待玩家点击"开始游戏"后跳转 ProcedureGame。
    /// </summary>
    public class ProcedureMenu : ProcedureBase
    {
        private bool m_StartGame = false;

        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);

            m_StartGame = false;

            // 订阅场景加载完成事件，加载完后再开 UI
            GameEntry.Event.Subscribe(LoadSceneSuccessEventArgs.EventId, OnSceneLoaded);
            GameEntry.Event.Subscribe(LoadSceneFailureEventArgs.EventId, OnSceneFailed);

            // 订阅来自主菜单 UI 的"开始游戏"自定义事件
            GameEntry.Event.Subscribe(EventId.MenuStartGame, OnStartGame);

            // 加载主菜单场景（会替换当前场景）
            GameEntry.Scene.LoadScene(AssetUtility.GetSceneAsset("Menu"), this);
        }

        protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (m_StartGame)
            {
                ChangeState<ProcedureGame>(procedureOwner);
            }
        }

        protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
        {
            GameEntry.Event.Unsubscribe(LoadSceneSuccessEventArgs.EventId, OnSceneLoaded);
            GameEntry.Event.Unsubscribe(LoadSceneFailureEventArgs.EventId, OnSceneFailed);
            GameEntry.Event.Unsubscribe(EventId.MenuStartGame, OnStartGame);

            // 关闭主菜单 UI
            GameEntry.UI.CloseAllLoadedUIForms();

            base.OnLeave(procedureOwner, isShutdown);
        }

        private void OnSceneLoaded(object sender, GameEventArgs e)
        {
            LoadSceneSuccessEventArgs args = (LoadSceneSuccessEventArgs)e;
            if (args.UserData != this) return;

            Log.Info("Menu scene loaded.");

            // 打开主菜单界面
            GameEntry.UI.OpenUIForm(AssetUtility.GetUIFormAsset(UIFormId.UIMenu), "Menu");
        }

        private void OnSceneFailed(object sender, GameEventArgs e)
        {
            LoadSceneFailureEventArgs args = (LoadSceneFailureEventArgs)e;
            if (args.UserData != this) return;

            Log.Fatal("Load menu scene failure: {0}", args.ErrorMessage);
        }

        private void OnStartGame(object sender, GameEventArgs e)
        {
            m_StartGame = true;
        }
    }
}
