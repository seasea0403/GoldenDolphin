using GameFramework.Event;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;
using System.Collections;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace LingBoCanteen
{
    /// <summary>
    /// 主菜单流程。
    /// 职责：加载菜单场景，打开主菜单界面，等待玩家点击「开始游戏」后进入主游戏流程。
    /// </summary>
    public class ProcedureMenu : ProcedureBase
    {
        private bool m_StartGame = false;
        private ProcedureOwner m_ProcedureOwner = null;

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);

            m_StartGame = false;
            m_ProcedureOwner = procedureOwner;

            GameEntry.Event.Subscribe(LoadSceneSuccessEventArgs.EventId, OnLoadSceneSuccess);
            GameEntry.Event.Subscribe(LoadSceneFailureEventArgs.EventId, OnLoadSceneFailure);

            Log.Info("进入主菜单流程。");

            // 初始化过渡管理器并隐藏黑屏
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.HideBlackScreenImmediate();
            }

            // 加载菜单场景
            GameEntry.Scene.LoadScene(AssetUtility.GetSceneAsset("Menu"), Constant.AssetPriority.SceneAsset, this);
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (m_StartGame)
            {
                m_StartGame = false;
                // 使用协程执行器执行黑屏过渡然后切换流程
                CoroutineExecutor.Instance?.ExecuteCoroutine(TransitionToGameProcedure(procedureOwner));
            }
        }

        private IEnumerator TransitionToGameProcedure(ProcedureOwner procedureOwner)
        {
            // 执行黑屏过渡
            if (SceneTransitionManager.Instance != null)
            {
                yield return SceneTransitionManager.Instance.TransitionScene(0.3f);
            }

            // 切换到主游戏流程
            ChangeState<ProcedureGame>(procedureOwner);
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            GameEntry.Event.Unsubscribe(LoadSceneSuccessEventArgs.EventId, OnLoadSceneSuccess);
            GameEntry.Event.Unsubscribe(LoadSceneFailureEventArgs.EventId, OnLoadSceneFailure);

            // 关闭主菜单UI
            if (GameEntry.UI.HasUIForm(UIFormId.MenuForm))
            {
                GameEntry.UI.CloseUIForm(GameEntry.UI.GetUIForm(UIFormId.MenuForm));
            }

            // 卸载菜单场景
            string menuSceneAssetName = AssetUtility.GetSceneAsset("Menu");
            if (GameEntry.Scene.SceneIsLoaded(menuSceneAssetName))
            {
                GameEntry.Scene.UnloadScene(menuSceneAssetName);
            }

            base.OnLeave(procedureOwner, isShutdown);
        }

        /// <summary>
        /// 供UI层调用：点击「开始游戏」按钮。
        /// </summary>
        public void StartGame()
        {
            m_StartGame = true;
        }

        private void OnLoadSceneSuccess(object sender, GameEventArgs e)
        {
            LoadSceneSuccessEventArgs ne = (LoadSceneSuccessEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            Log.Info("Load menu scene OK.");

            // 播放主菜单BGM，使用渐入效果（0.5秒）
            SoundManager.Instance?.PlayBackgroundMusic(30000, 0.5f);

            // 打开主菜单UI
            GameEntry.UI.OpenUIForm(UIFormId.MenuForm, this);
        }

        private void OnLoadSceneFailure(object sender, GameEventArgs e)
        {
            LoadSceneFailureEventArgs ne = (LoadSceneFailureEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            Log.Error("Load menu scene failure with error message '{0}'.", ne.ErrorMessage);
        }
    }
}