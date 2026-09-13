using GameFramework.Event;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;
using System.Collections;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace LingBoCanteen
{
    /// <summary>
    /// 主游戏流程。
    /// 职责：加载主场景，进入实际玩法。
    /// </summary>
    public class ProcedureGame : ProcedureBase
    {
        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);

            GameEntry.Event.Subscribe(LoadSceneSuccessEventArgs.EventId, OnLoadSceneSuccess);
            GameEntry.Event.Subscribe(LoadSceneFailureEventArgs.EventId, OnLoadSceneFailure);
            GameEntry.Event.Subscribe(LoadSceneUpdateEventArgs.EventId, OnLoadSceneUpdate);

            Log.Info("进入主游戏流程。");

            // 加载主场景
            GameEntry.Scene.LoadScene(AssetUtility.GetSceneAsset("Main"), Constant.AssetPriority.SceneAsset, this);
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            GameEntry.Event.Unsubscribe(LoadSceneSuccessEventArgs.EventId, OnLoadSceneSuccess);
            GameEntry.Event.Unsubscribe(LoadSceneFailureEventArgs.EventId, OnLoadSceneFailure);
            GameEntry.Event.Unsubscribe(LoadSceneUpdateEventArgs.EventId, OnLoadSceneUpdate);

            // 卸载主场景
            string mainSceneAssetName = AssetUtility.GetSceneAsset("Main");
            if (GameEntry.Scene.SceneIsLoaded(mainSceneAssetName))
            {
                GameEntry.Scene.UnloadScene(mainSceneAssetName);
            }

            base.OnLeave(procedureOwner, isShutdown);
        }

        private void OnLoadSceneSuccess(object sender, GameEventArgs e)
        {
            LoadSceneSuccessEventArgs ne = (LoadSceneSuccessEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            Log.Info("Load main scene OK.");

            // 初始化游戏BGM管理器（根据当前Region和TimePhase自动播放BGM）
            SoundManager.Instance?.PlayMusicForCurrentGameState();

            // 隐藏过场动画（由 ProcedureMenu.TransitionToGameProcedure 在点击开始游戏时显示）
            TransitionCutsceneView.Instance?.SetProgress(1f);
            if (TransitionCutsceneView.Instance != null)
            {
                CoroutineExecutor.Instance?.ExecuteCoroutine(TransitionCutsceneView.Instance.HideAsync());
            }
        }

        private void OnLoadSceneFailure(object sender, GameEventArgs e)
        {
            LoadSceneFailureEventArgs ne = (LoadSceneFailureEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            Log.Error("Load main scene failure with error message '{0}'.", ne.ErrorMessage);
        }

        private void OnLoadSceneUpdate(object sender, GameEventArgs e)
        {
            LoadSceneUpdateEventArgs ne = (LoadSceneUpdateEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            // 喂入场景加载的真实进度，让过场里的进度条/切菜画面跟着实际加载走，而不是一直停在 0 直到结束才跳变到 1
            TransitionCutsceneView.Instance?.SetProgress(ne.Progress);
        }
    }
}