using GameFramework;
using GameFramework.Event;
using GameFramework.Resource;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace LingBoCanteen
{
    /// <summary>
    /// 预加载流程。
    /// 职责：依次加载所有 DataTable（Ingredient/Dish/Day/Guest/Scene/UIForm/Music/Sound/UISound），
    ///    
    /// </summary>
    public class ProcedurePreload : ProcedureBase
    {
        // 需要加载的所有数据表名称
        private static readonly string[] DataTableNames = new string[]
        {
            // "Ingredient",
             "Dish",
             "Day",
             "Guest",
             "Scene",
             "UIForm",
            // "Music",
            // "Sound",
            // "UISound",
        };

        private readonly Dictionary<string, bool> m_LoadedFlag = new Dictionary<string, bool>();

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);

            GameEntry.Event.Subscribe(LoadConfigSuccessEventArgs.EventId, OnLoadConfigSuccess);
            GameEntry.Event.Subscribe(LoadConfigFailureEventArgs.EventId, OnLoadConfigFailure);
            GameEntry.Event.Subscribe(LoadDataTableSuccessEventArgs.EventId, OnLoadDataTableSuccess);
            GameEntry.Event.Subscribe(LoadDataTableFailureEventArgs.EventId, OnLoadDataTableFailure);

            m_LoadedFlag.Clear();

            Log.Info("Enter ProcedurePreload, cleared load flags.");
            UnityEngine.Debug.Log("ProcedurePreload.OnEnter");

            PreloadResources();
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            GameEntry.Event.Unsubscribe(LoadConfigSuccessEventArgs.EventId, OnLoadConfigSuccess);
            GameEntry.Event.Unsubscribe(LoadConfigFailureEventArgs.EventId, OnLoadConfigFailure);
            GameEntry.Event.Unsubscribe(LoadDataTableSuccessEventArgs.EventId, OnLoadDataTableSuccess);
            GameEntry.Event.Unsubscribe(LoadDataTableFailureEventArgs.EventId, OnLoadDataTableFailure);

            base.OnLeave(procedureOwner, isShutdown);
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            foreach (KeyValuePair<string, bool> loadedFlag in m_LoadedFlag)
            {
                if (!loadedFlag.Value)
                {
                    return;
                }
            }

            int nextSceneId = 1;
            try
            {
                nextSceneId = GameEntry.Config.GetInt("Scene.Menu");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning("Config 'Scene.Menu' not found. Falling back to scene id 1. Msg: " + ex.Message);
            }

            procedureOwner.SetData<VarInt32>("NextSceneId", nextSceneId);
            UnityEngine.Debug.Log("ProcedurePreload complete! Changing state to ProcedureChangeScene with NextSceneId: " + nextSceneId);
            ChangeState<ProcedureChangeScene>(procedureOwner);
        }

        private void PreloadResources()
        {
            Log.Info("PreloadResources start.");
            UnityEngine.Debug.Log("ProcedurePreload.PreloadResources start");
            Log.Info("Will load {0} data tables.", DataTableNames.Length);
            // Preload configs
            LoadConfig("DefaultConfig");

            // Preload data tables
            foreach (string dataTableName in DataTableNames)
            {
                LoadDataTable(dataTableName);
            }


            // Preload fonts
            Log.Info("Start loading fonts.");
            LoadFont("MainFont");
        }

        private void LoadConfig(string configName)
        {
            string configAssetName = AssetUtility.GetConfigAsset(configName, false);
            m_LoadedFlag.Add(configAssetName, false);
            Log.Info("Start loading config '{0}' as asset '{1}'.", configName, configAssetName);
            GameEntry.Config.ReadData(configAssetName, this);
        }

        private void LoadDataTable(string dataTableName)
        {
            string dataTableAssetName = AssetUtility.GetDataTableAsset(dataTableName, false);
            m_LoadedFlag.Add(dataTableAssetName, false);
            Log.Info("Start loading data table '{0}' as asset '{1}'.", dataTableName, dataTableAssetName);
            GameEntry.DataTable.LoadDataTable(dataTableName, dataTableAssetName, this);
        }

        private void LoadFont(string fontName)
        {
            m_LoadedFlag.Add(Utility.Text.Format("Font.{0}", fontName), false);
            Log.Info("Start loading font asset for '{0}'.", fontName);
            GameEntry.Resource.LoadAsset(AssetUtility.GetFontAsset(fontName), Constant.AssetPriority.FontAsset, new LoadAssetCallbacks(
                (assetName, asset, duration, userData) =>
                {
                    m_LoadedFlag[Utility.Text.Format("Font.{0}", fontName)] = true;
                    UGuiForm.SetMainFont((Font)asset);
                    Log.Info("Load font '{0}' OK.", fontName);
                    UnityEngine.Debug.Log("OnLoadFontSuccess: " + fontName);
                },

                (assetName, status, errorMessage, userData) =>
                {
                    Log.Error("Can not load font '{0}' from '{1}' with error message '{2}'.", fontName, assetName, errorMessage);
                    UnityEngine.Debug.LogError("OnLoadFontFailure: " + fontName + " from " + assetName + " => " + errorMessage);
                }));
        }

        private void OnLoadConfigSuccess(object sender, GameEventArgs e)
        {
            LoadConfigSuccessEventArgs ne = (LoadConfigSuccessEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            m_LoadedFlag[ne.ConfigAssetName] = true;
            Log.Info("Load config '{0}' OK.", ne.ConfigAssetName);
            UnityEngine.Debug.Log("OnLoadConfigSuccess: " + ne.ConfigAssetName);
        }

        private void OnLoadConfigFailure(object sender, GameEventArgs e)
        {
            LoadConfigFailureEventArgs ne = (LoadConfigFailureEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            Log.Error("Can not load config '{0}' from '{1}' with error message '{2}'.", ne.ConfigAssetName, ne.ConfigAssetName, ne.ErrorMessage);
            UnityEngine.Debug.LogError("OnLoadConfigFailure: " + ne.ConfigAssetName + " => " + ne.ErrorMessage);
        }

        private void OnLoadDataTableSuccess(object sender, GameEventArgs e)
        {
            LoadDataTableSuccessEventArgs ne = (LoadDataTableSuccessEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            m_LoadedFlag[ne.DataTableAssetName] = true;
            Log.Info("Load data table '{0}' OK.", ne.DataTableAssetName);
            UnityEngine.Debug.Log("OnLoadDataTableSuccess: " + ne.DataTableAssetName);
        }

        private void OnLoadDataTableFailure(object sender, GameEventArgs e)
        {
            LoadDataTableFailureEventArgs ne = (LoadDataTableFailureEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            Log.Error("Can not load data table '{0}' from '{1}' with error message '{2}'.", ne.DataTableAssetName, ne.DataTableAssetName, ne.ErrorMessage);
            UnityEngine.Debug.LogError("OnLoadDataTableFailure: " + ne.DataTableAssetName + " => " + ne.ErrorMessage);
        }

    
    }
}
