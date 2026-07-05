using UnityEngine;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 抽屉特殊食材（鸡蛋/面粉）专属操作面板：点击食材弹出，玩家通过连续点击屏幕推进制作进度，
    /// 每点击一次播放一段对应操作动画，达到指定次数后关闭面板并产出结果。
    /// 鸡蛋 -&gt; 杯槽(蛋液)，面粉 -&gt; 碗槽(面团)；对应槽位已满时滞留等待，每帧重试直到有空位。
    /// 场景内为鸡蛋/面粉各放一个实例（或共用一个面板但分别配置 TargetGroup），
    /// 面板对应的根节点默认关闭，脚本所在物体本身保持激活以便滞留期间 Update 能继续轮询。
    /// </summary>
    public class DrawerOperationPanel : MonoBehaviour
    {
        [SerializeField] private GameObject m_Root;
        [SerializeField] private int m_RequiredClickCount = Constant.GameConstant.DEFAULT_DRAWER_CLICK_COUNT;
        [SerializeField] private Animator m_Animator;
        [SerializeField] private string m_StepAnimTrigger = "Step";
        [Tooltip("鸡蛋填杯槽，面粉填碗槽")]
        [SerializeField] private OutputSlotGroup m_TargetGroup;

        private int m_IngredientId;
        private int m_ClickCount;
        private bool m_IsOpen;

        private bool m_HasPendingOutput;
        private DRIngredient m_PendingRow;

        public void Open(int ingredientId)
        {
            m_IngredientId = ingredientId;
            m_ClickCount = 0;
            m_IsOpen = true;
            m_Root?.SetActive(true);
        }

        /// <summary>
        /// 绑定到面板内"点击屏幕"按钮的 OnClick。
        /// </summary>
        public void OnClickAdvance()
        {
            if (!m_IsOpen)
            {
                return;
            }

            m_ClickCount++;
            m_Animator?.SetTrigger(m_StepAnimTrigger);

            if (m_ClickCount >= m_RequiredClickCount)
            {
                Finish();
            }
        }

        private void Finish()
        {
            m_IsOpen = false;
            m_Root?.SetActive(false);

            DRIngredient row = GameEntry.DataTable.GetDataTable<DRIngredient>().GetDataRow(m_IngredientId);
            if (row == null)
            {
                Log.Error("DrawerOperationPanel 找不到 Id 为 {0} 的 DRIngredient 配置。", m_IngredientId);
                return;
            }

            m_PendingRow = row;
            m_HasPendingOutput = true;
            TryFlushPendingOutput();
        }

        private void Update()
        {
            if (m_HasPendingOutput)
            {
                TryFlushPendingOutput();
            }
        }

        private void TryFlushPendingOutput()
        {
            if (m_TargetGroup == null || PrepAreaManager.Instance == null)
            {
                return;
            }

            Sprite sprite = IngredientUtility.GetSprite(m_PendingRow, m_PendingRow.FinalAssetName);
            if (m_TargetGroup.TryAddItem(m_PendingRow.OutputId, m_PendingRow.OutputName, sprite))
            {
                m_HasPendingOutput = false;
                m_PendingRow = null;
            }
        }
    }
}
