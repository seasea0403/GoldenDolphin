using GameFramework.Event;
using GameFramework.Fsm;
using GameFramework.Procedure;
using System.Collections.Generic;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 预加载流程。
    /// 职责：依次加载所有 DataTable（Ingredient/Dish/Day/Guest/Scene/UIForm/Music/Sound/UISound），
    ///       全部完成后跳转 Menu。
    /// </summary>
    public class ProcedurePreload : ProcedureBase
    {
        // 需要加载的所有数据表名称
        private static readonly string[] DataTableNames = new string[]
        {
            "Ingredient",
            "Dish",
            "Day",
            "Guest",
            "Scene",
            "UIForm",
            "Music",
            "Sound",
            "UISound",
        };

        private readonly Dictionary<string, bool> m_LoadedFlag = new Dictionary<string, bool>();

        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);

            GameEntry.Event.Subscribe(LoadDataTableSuccessEventArgs.EventId, OnDataTableLoaded);
            GameEntry.Event.Subscribe(LoadDataTableFailureEventArgs.EventId, OnDataTableFailed);

            m_LoadedFlag.Clear();
            foreach (string tableName in DataTableNames)
            {
                m_LoadedFlag[tableName] = false;
                GameEntry.DataTable.LoadDataTable(tableName, AssetUtility.GetDataTableAsset(tableName, false), this);
            }
        }

        protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (IsAllLoaded())
            {
                ChangeState<ProcedureMenu>(procedureOwner);
            }
        }

        protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
        {
            GameEntry.Event.Unsubscribe(LoadDataTableSuccessEventArgs.EventId, OnDataTableLoaded);
            GameEntry.Event.Unsubscribe(LoadDataTableFailureEventArgs.EventId, OnDataTableFailed);
            base.OnLeave(procedureOwner, isShutdown);
        }

        private bool IsAllLoaded()
        {
            foreach (var kv in m_LoadedFlag)
            {
                if (!kv.Value) return false;
            }
            return m_LoadedFlag.Count > 0;
        }

        private void OnDataTableLoaded(object sender, GameEventArgs e)
        {
            LoadDataTableSuccessEventArgs args = (LoadDataTableSuccessEventArgs)e;
            // UserData 为 ProcedurePreload 自身，排除其他流程触发的同名事件
            if (args.UserData != this) return;

            // 从路径提取表名（取不含扩展名的文件名末段）
            string tableName = GetTableNameFromAssetName(args.DataTableAssetName);
            if (m_LoadedFlag.ContainsKey(tableName))
            {
                m_LoadedFlag[tableName] = true;
                Log.Info("DataTable '{0}' loaded.", tableName);
            }
        }

        private void OnDataTableFailed(object sender, GameEventArgs e)
        {
            LoadDataTableFailureEventArgs args = (LoadDataTableFailureEventArgs)e;
            if (args.UserData != this) return;

            Log.Fatal("Load data table failure, asset name '{0}', error message '{1}'.",
                args.DataTableAssetName, args.ErrorMessage);
        }

        private static string GetTableNameFromAssetName(string assetName)
        {
            // assetName 格式例: "Assets/LingBoCanteen/Datatables/Ingredient.bytes"
            int lastSlash = assetName.LastIndexOf('/');
            string fileName = lastSlash >= 0 ? assetName.Substring(lastSlash + 1) : assetName;
            int dotIndex = fileName.LastIndexOf('.');
            return dotIndex >= 0 ? fileName.Substring(0, dotIndex) : fileName;
        }
    }
}
