//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.DataTable;
using System;
using System.IO;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LingBoCanteen
{
    public static class DataTableExtension
    {
        private const string DataRowClassPrefixName = "LingBoCanteen.DR";
        internal static readonly char[] DataSplitSeparators = new char[] { '\t' };
        internal static readonly char[] DataTrimSeparators = new char[] { '\"' };

        public static void LoadDataTable(this DataTableComponent dataTableComponent, string dataTableName, string dataTableAssetName, object userData)
        {
            if (string.IsNullOrEmpty(dataTableName))
            {
                Log.Warning("Data table name is invalid.");
                return;
            }

            string[] splitedNames = dataTableName.Split('_');
            if (splitedNames.Length > 2)
            {
                Log.Warning("Data table name is invalid.");
                return;
            }

            string dataRowClassName = DataRowClassPrefixName + splitedNames[0];
            Type dataRowType = Type.GetType(dataRowClassName);
            if (dataRowType == null)
            {
                Log.Warning("Can not get data row type with class name '{0}'.", dataRowClassName);
                return;
            }

            string name = splitedNames.Length > 1 ? splitedNames[1] : null;
            DataTableBase dataTable = dataTableComponent.CreateDataTable(dataRowType, name);
            dataTable.ReadData(dataTableAssetName, Constant.AssetPriority.DataTableAsset, userData);
        }

        public static Color32 ParseColor32(string value)
        {
            string[] splitedValue = value.Split(',');
            return new Color32(byte.Parse(splitedValue[0]), byte.Parse(splitedValue[1]), byte.Parse(splitedValue[2]), byte.Parse(splitedValue[3]));
        }

        public static Color ParseColor(string value)
        {
            string[] splitedValue = value.Split(',');
            return new Color(float.Parse(splitedValue[0]), float.Parse(splitedValue[1]), float.Parse(splitedValue[2]), float.Parse(splitedValue[3]));
        }

        public static Quaternion ParseQuaternion(string value)
        {
            string[] splitedValue = value.Split(',');
            return new Quaternion(float.Parse(splitedValue[0]), float.Parse(splitedValue[1]), float.Parse(splitedValue[2]), float.Parse(splitedValue[3]));
        }

        public static Rect ParseRect(string value)
        {
            string[] splitedValue = value.Split(',');
            return new Rect(float.Parse(splitedValue[0]), float.Parse(splitedValue[1]), float.Parse(splitedValue[2]), float.Parse(splitedValue[3]));
        }

        public static Vector2 ParseVector2(string value)
        {
            string[] splitedValue = value.Split(',');
            return new Vector2(float.Parse(splitedValue[0]), float.Parse(splitedValue[1]));
        }

        public static Vector3 ParseVector3(string value)
        {
            string[] splitedValue = value.Split(',');
            return new Vector3(float.Parse(splitedValue[0]), float.Parse(splitedValue[1]), float.Parse(splitedValue[2]));
        }

        public static Vector4 ParseVector4(string value)
        {
            string[] splitedValue = value.Split(',');
            return new Vector4(float.Parse(splitedValue[0]), float.Parse(splitedValue[1]), float.Parse(splitedValue[2]), float.Parse(splitedValue[3]));
        }

        /// <summary>
        /// 解析形如 "1001,1002,1003" 的英文逗号分隔整数列表，空字符串返回长度为 0 的数组。
        /// </summary>
        public static int[] ParseInt32Array(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new int[0];
            }

            // 处理可能包含双引号包裹的情况（如 Excel 导出 CSV 带引号）
            if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
            {
                value = value.Substring(1, value.Length - 2);
            }

            string[] splitedValue = value.Split(',');
            System.Collections.Generic.List<int> tempList = new System.Collections.Generic.List<int>();
            
            for (int i = 0; i < splitedValue.Length; i++)
            {
                string s = splitedValue[i].Trim();
                if (string.IsNullOrEmpty(s))
                {
                    continue;
                }

                if (int.TryParse(s, out int parsedVal))
                {
                    tempList.Add(parsedVal);
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"ParseInt32Array：无法将值 '{s}' 解析为整数，已忽略。完整输入为：'{value}'");
                }
            }

            return tempList.ToArray();
        }

        /// <summary>
        /// 从二进制流中读取一个整型数组（长度前缀 + 逐个 7 位编码整数），与 ParseInt32Array 的字符串格式对应。
        /// </summary>
        public static int[] ReadInt32Array(this BinaryReader reader)
        {
            int count = reader.Read7BitEncodedInt32();
            int[] result = new int[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = reader.Read7BitEncodedInt32();
            }

            return result;
        }
    }
}
