using GameFramework.Resource;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;
using UnityEngine;

namespace LingBoCanteen
{
    public class ProcedureSplash : ProcedureBase
    {
        private float m_DisplayTimer = 0f;
        private const float LogoDisplayDuration = 2.0f; // Logo 展示时长（秒）

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            m_DisplayTimer = 0f;
            
            // 如果你在场景中放了一个叫做 "SplashLogo" 的 GameObject，可以在这里激活它
            GameObject.Find("SplashLogo")?.SetActive(true);
        }
        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            // TODO: 这里可以播放一个 Splash 动画
            // ...
            m_DisplayTimer += elapseSeconds;
            if (m_DisplayTimer < LogoDisplayDuration)
            {
                return; // 时间未到，继续展示
            }
            
            if (GameEntry.Base.EditorResourceMode)
            {
                // 编辑器模式
                Log.Info("Editor resource mode detected.");
                ChangeState<ProcedurePreload>(procedureOwner);
            }
            else if (GameEntry.Resource.ResourceMode == ResourceMode.Package)
            {
                // 单机模式
                Log.Info("Package resource mode detected.");
                ChangeState<ProcedureInitResources>(procedureOwner);
            }
            else
            {
                // 可更新模式
                Log.Info("Updatable resource mode detected.");
                //ChangeState<ProcedureCheckVersion>(procedureOwner);
            }
        }
    }
}