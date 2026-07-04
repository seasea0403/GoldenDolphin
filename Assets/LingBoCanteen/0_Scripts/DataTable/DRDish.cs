//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------
// 此文件由工具自动生成，请勿直接修改。
// 生成时间：2026-07-04 17:30:00.000
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
    /// 菜品配置表。
    /// </summary>
    public class DRDish : DataRowBase
    {
        private int m_Id = 0;

        /// <summary>
        /// 获取菜品编号。
        /// </summary>
        public override int Id
        {
            get
            {
                return m_Id;
            }
        }

        /// <summary>
        /// 获取菜品名称。
        /// </summary>
        public string Name
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

        /// <summary>
        /// 获取所需食材列表。
        /// </summary>
        public string IngList
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取所需调料列表。
        /// </summary>
        public string SeasoningList
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取所需锅具。
        /// </summary>
        public string Pot
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取关联配方资源。
        /// </summary>
        public string RecipeAsset
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取抽取权重。
        /// </summary>
        public int Weight
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取出售金币收益。
        /// </summary>
        public int EarnMoney
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
            index++;
            Name = columnStrings[index++];
            index++;
            AssetName = columnStrings[index++];
            index++;
            IngList = columnStrings[index++];
            index++;
            SeasoningList = columnStrings[index++];
            index++;
            Pot = columnStrings[index++];
            index++;
            RecipeAsset = columnStrings[index++];
            index++;
            Weight = int.Parse(columnStrings[index++]);
            index++;
            EarnMoney = int.Parse(columnStrings[index++]);

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
                    Name = binaryReader.ReadString();
                    AssetName = binaryReader.ReadString();
                    IngList = binaryReader.ReadString();
                    SeasoningList = binaryReader.ReadString();
                    Pot = binaryReader.ReadString();
                    RecipeAsset = binaryReader.ReadString();
                    Weight = binaryReader.Read7BitEncodedInt32();
                    EarnMoney = binaryReader.Read7BitEncodedInt32();
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