//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    /// <summary>
    /// 顾客配置表。
    /// </summary>
    public class DRGuest : DataRowBase
    {
        private int m_Id = 0;

        /// <summary>
        /// 获取顾客 ID。
        /// </summary>
        public override int Id
        {
            get
            {
                return m_Id;
            }
        }

        /// <summary>
        /// 获取顾客类型（是否为剧情顾客，1 为剧情顾客，其他为普通顾客）。
        /// </summary>
        public int GuestType
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取顾客所属分区（角色区域）。
        /// </summary>
        public int Region
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取资源名称。
        /// </summary>
        public string AssetName
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
            index++; // 跳过首列注释/空格
            m_Id = int.Parse(columnStrings[index++]);
            GuestType = int.Parse(columnStrings[index++]);
            Region = int.Parse(columnStrings[index++]);
            AssetName = columnStrings[index++];

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
                    GuestType = binaryReader.Read7BitEncodedInt32();
                    Region = binaryReader.Read7BitEncodedInt32();
                    AssetName = binaryReader.ReadString();
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
