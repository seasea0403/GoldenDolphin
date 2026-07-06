//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------
// 此文件由工具自动生成，请勿直接修改。
//------------------------------------------------------------

using GameFramework;
using GameFramework.DataTable;
using System.IO;
using System.Text;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 天数配置表。
    /// </summary>
    public class DRDay : DataRowBase
    {
        private int m_Id = 0;

        /// <summary>
        /// 获取天数（DayId）。
        /// </summary>
        public override int Id
        {
            get
            {
                return m_Id;
            }
        }

        /// <summary>
        /// 获取当天的顾客总数。
        /// </summary>
        public int CustomerCount
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取当天解锁的食材 Id 列表。
        /// </summary>
        public int[] UnlockIngIds
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取当天解锁的菜品 Id 列表。
        /// </summary>
        public int[] UnlockDishIds
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取剧情顾客对话 Id。
        /// </summary>
        public int PlotId
        {
            get;
            private set;
        }

        public override bool ParseDataRow(string dataRowString, object userData)
        {
            string[] columnStrings = dataRowString.Split(DataTableExtension.DataSplitSeparators);
            for (int i = 0; i < columnStrings.Length; i++)
            {
                columnStrings[i] = columnStrings[i].Trim(DataTableExtension.DataTrimSeparators);
            }

            int index = 0;
            index++;
            m_Id = int.Parse(columnStrings[index++]);
            CustomerCount = int.Parse(columnStrings[index++]);
            UnlockIngIds = DataTableExtension.ParseInt32Array(columnStrings[index++]);
            UnlockDishIds = DataTableExtension.ParseInt32Array(columnStrings[index++]);
            PlotId = string.IsNullOrEmpty(columnStrings[index]) ? 0 : int.Parse(columnStrings[index++]);

            GeneratePropertyArray();
            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    CustomerCount = binaryReader.Read7BitEncodedInt32();
                    UnlockIngIds = binaryReader.ReadInt32Array();
                    UnlockDishIds = binaryReader.ReadInt32Array();
                    PlotId = binaryReader.Read7BitEncodedInt32();
                }
            }

            GeneratePropertyArray();
            return true;
        }

        private void GeneratePropertyArray()
        {

        }
    }
}
