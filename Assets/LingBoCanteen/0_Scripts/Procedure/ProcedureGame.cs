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

            Log.Info("进入主游戏流程。");

            // 加载主场景
            GameEntry.Scene.LoadScene(AssetUtility.GetSceneAsset("Main"), Constant.AssetPriority.SceneAsset, this);
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            GameEntry.Event.Unsubscribe(LoadSceneSuccessEventArgs.EventId, OnLoadSceneSuccess);
            GameEntry.Event.Unsubscribe(LoadSceneFailureEventArgs.EventId, OnLoadSceneFailure);

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

            // 执行淡入效果
            if (SceneTransitionManager.Instance != null)
            {
                CoroutineExecutor.Instance?.ExecuteCoroutine(SceneTransitionManager.Instance.FadeInScene());
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
    }
}