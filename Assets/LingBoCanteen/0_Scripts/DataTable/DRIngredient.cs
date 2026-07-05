//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------
// 此文件本应由工具自动生成，但目前与 Ingredient.txt 的最新列结构（新增 EnName /
// MoveAssetName，去掉 ProcessType 列）不一致，已手工同步修正，若之后重新用生成工具
// 导出，请确认生成结果包含 EnName 字段与本文件末尾的 ProcessType 计算属性，
// 否则备菜区相关代码会因为找不到 EnName / ProcessType 而报错。
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
    /// 食材配置表。
    /// </summary>
    public class DRIngredient : DataRowBase
    {
        private int m_Id = 0;

        /// <summary>
        /// 获取食材编号。
        /// </summary>
        public override int Id
        {
            get
            {
                return m_Id;
            }
        }

        /// <summary>
        /// 获取食材名。
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取食材英文名（对应美术资源文件夹命名 "{Id}_{EnName}"）。
        /// </summary>
        public string EnName
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取初始资源名称。
        /// </summary>
        public string InitialAssetName
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取能否被切。
        /// </summary>
        public bool CanCut
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取能否被榨汁。
        /// </summary>
        public bool CanSqueeze
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取移动态资源名称（拾取拖拽时显示）。
        /// </summary>
        public string MoveAssetName
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取中间产物资源名称。
        /// </summary>
        public string MidAssetName
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取二次处理产物资源。
        /// </summary>
        public string CutKnobName
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取产出物资源名称。
        /// </summary>
        public string FinalAssetName
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取产出物编号。
        /// </summary>
        public int OutputId
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取产出物名。
        /// </summary>
        public string OutputName
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取购买食材所需金币（冰箱/抽屉食材不可采购，表里留空，读取为 0）。
        /// </summary>
        public int ConsumeMoney
        {
            get;
            private set;
        }

        /// <summary>
        /// 食材的处理方式（表里已去掉这一列，改由 CanCut/CanSqueeze 组合推算，
        /// 取值含义与旧版保持一致：0=不可加工 1=仅可切 2=仅可榨汁 3=可切可榨汁）。
        /// </summary>
        public int ProcessType
        {
            get
            {
                if (CanCut && CanSqueeze)
                {
                    return 3;
                }

                if (CanCut)
                {
                    return 1;
                }

                if (CanSqueeze)
                {
                    return 2;
                }

                return 0;
            }
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
            Name = columnStrings[index++];
            EnName = columnStrings[index++];
            InitialAssetName = columnStrings[index++];
            CanCut = ParseBool(columnStrings[index++]);
            CanSqueeze = ParseBool(columnStrings[index++]);
            MoveAssetName = columnStrings[index++];
            MidAssetName = columnStrings[index++];
            CutKnobName = columnStrings[index++];
            FinalAssetName = columnStrings[index++];
            OutputId = ParseInt(columnStrings[index++]);
            OutputName = columnStrings[index++];
            ConsumeMoney = ParseInt(columnStrings[index++]);

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
                    EnName = binaryReader.ReadString();
                    InitialAssetName = binaryReader.ReadString();
                    CanCut = binaryReader.ReadBoolean();
                    CanSqueeze = binaryReader.ReadBoolean();
                    MoveAssetName = binaryReader.ReadString();
                    MidAssetName = binaryReader.ReadString();
                    CutKnobName = binaryReader.ReadString();
                    FinalAssetName = binaryReader.ReadString();
                    OutputId = binaryReader.Read7BitEncodedInt32();
                    OutputName = binaryReader.ReadString();
                    ConsumeMoney = binaryReader.Read7BitEncodedInt32();
                }
            }

            GeneratePropertyArray();
            return true;
        }

        /// <summary>
        /// 表里"不可采购/不可加工"的食材会把对应数值列留空（例如冰箱/抽屉食材没有 ConsumeMoney），
        /// 空字符串按 0 处理，而不是让 int.Parse 直接抛异常导致整张表加载失败。
        /// </summary>
        private static int ParseInt(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            return int.TryParse(value, out int result) ? result : 0;
        }

        private static bool ParseBool(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return bool.TryParse(value, out bool result) && result;
        }

        private void GeneratePropertyArray()
        {

        }
    }
}

