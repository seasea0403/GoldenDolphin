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
        /// 获取食材名称。
        /// </summary>
        public string Name
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
        /// 获取是否可切。
        /// </summary>
        public bool CanCut
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取是否可榨汁。
        /// </summary>
        public bool CanSqueeze
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
        /// 获取二次处理产物资源名称。
        /// </summary>
        public string CutKnobName
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取最终产出资源名称。
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
        /// 获取产出物名称。
        /// </summary>
        public string OutputName
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取购买消耗金币。
        /// </summary>
        public int ConsumeMoney
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取处理方式类型。
        /// </summary>
        public int ProcessType
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
            InitialAssetName = columnStrings[index++];
            index++;
            CanCut = bool.Parse(columnStrings[index++]);
            index++;
            CanSqueeze = bool.Parse(columnStrings[index++]);
            index++;
            MidAssetName = columnStrings[index++];
            index++;
            CutKnobName = columnStrings[index++];
            index++;
            FinalAssetName = columnStrings[index++];
            index++;
            OutputId = int.Parse(columnStrings[index++]);
            index++;
            OutputName = columnStrings[index++];
            index++;
            ConsumeMoney = int.Parse(columnStrings[index++]);
            index++;
            ProcessType = int.Parse(columnStrings[index++]);

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
                    InitialAssetName = binaryReader.ReadString();
                    CanCut = binaryReader.ReadBoolean();
                    CanSqueeze = binaryReader.ReadBoolean();
                    MidAssetName = binaryReader.ReadString();
                    CutKnobName = binaryReader.ReadString();
                    FinalAssetName = binaryReader.ReadString();
                    OutputId = binaryReader.Read7BitEncodedInt32();
                    OutputName = binaryReader.ReadString();
                    ConsumeMoney = binaryReader.Read7BitEncodedInt32();
                    ProcessType = binaryReader.Read7BitEncodedInt32();
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