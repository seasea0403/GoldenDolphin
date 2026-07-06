using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 备菜区中心注册点：
    /// 1. 持有碗槽/杯槽两个收纳槽组的引用，供菜板/榨汁机/抽屉操作面板查找；
    /// 2. 食材美术资源按 "{Id}_{EnName}" 文件夹命名约定自动加载（见 <see cref="IngredientUtility.GetSprite"/>），
    ///    无需在 Inspector 里逐个配置映射表；
    /// 3. 维护场景内所有 <see cref="Ingredient"/> 的注册表，供解锁天数/库存变化后统一刷新显示。
    /// </summary>
    public class PrepAreaManager : MonoBehaviour
    {
        public static PrepAreaManager Instance { get; private set; }

        [SerializeField] private OutputSlotGroup m_BowlGroup;
        [SerializeField] private OutputSlotGroup m_GlassGroup;

        private readonly List<Ingredient> m_RegisteredIngredients = new List<Ingredient>();

        public OutputSlotGroup BowlGroup => m_BowlGroup;
        public OutputSlotGroup GlassGroup => m_GlassGroup;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            RefreshAllShelfIngredients();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// 按食材配置表里的资源名查找 Sprite（InitialAssetName / MoveAssetName / MidAssetName / CutKnobName / FinalAssetName）。
        /// 资源路径由 row.Id + row.EnName 自动拼出，无需手动配置。
        /// </summary>
        public Sprite GetIngredientSprite(DRIngredient row, string assetName)
        {
            return IngredientUtility.GetSprite(row, assetName);
        }

        public void RegisterIngredient(Ingredient ingredient)
        {
            if (!m_RegisteredIngredients.Contains(ingredient))
            {
                m_RegisteredIngredients.Add(ingredient);
            }
        }

        public void UnregisterIngredient(Ingredient ingredient)
        {
            m_RegisteredIngredients.Remove(ingredient);
        }

        /// <summary>
        /// 刷新所有货架/冰箱食材的锁定/置灰显示，天数变化或库存变化（超市采购）后调用。
        /// </summary>
        public void RefreshAllShelfIngredients()
        {
            for (int i = 0; i < m_RegisteredIngredients.Count; i++)
            {
                m_RegisteredIngredients[i]?.Refresh();
            }
        }
    }
}
