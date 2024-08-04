using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using UnityEngine;
using System;
using System.Linq;

public class CsvReader
{
    public Dictionary<int, List<string>> csvData = new Dictionary<int, List<string>>();
    private Dictionary<string, int> columnMapping = new Dictionary<string, int>();
    private Dictionary<int, string> dataTypes = new Dictionary<int, string>();
    private static byte[] Key = Encoding.UTF8.GetBytes("ABCDEFGHIJKLMNOP"); // 密钥，长度必须是16、24或32字节
    private static byte[] IV = Encoding.UTF8.GetBytes("1234567890123456"); // 初始化向量，长度必须是16字节

    public void ReadFile(string fileName, bool encrypted = false)
    {
        // if (!fileName.EndsWith(".csv"))
        //     fileName += ".csv";
        TextAsset csvFile = Resources.Load<TextAsset>("Table/" + fileName);
        if (csvFile == null)
        {
            Debug.LogError("CSV file not found: " + "Table/" + fileName);
            return;
        }// 
        else
        {
            Debug.Log("CSV file loaded: " + "Table/" + fileName);
        }

        string fileContent = Encoding.UTF8.GetString(csvFile.bytes);
        

        if (encrypted)
        {
            fileContent = Decrypt(fileContent);
        }
        
        

        using (StringReader reader = new StringReader(fileContent))
        {
            //read first row as column name
            string[] _rowData = reader.ReadLine().Split(',');
            List<string> _rowList = new List<string>(_rowData);
            for (int i = 0; i < _rowList.Count; i++)
            {
                columnMapping.Add(_rowList[i], i);
            }
            
            //skip second row
            reader.ReadLine();

            //read third row as data type
            _rowData = reader.ReadLine().Split(',');
            _rowList = new List<string>(_rowData);
            for (int i = 0; i < _rowList.Count; i++)
            {
                dataTypes.Add(i, _rowList[i]);
            }

            //read other rows
            string line;
            int rowIndex = 0;
            while ((line = reader.ReadLine()) != null)
            {
                string[] rowData = line.Split(',');
                List<string> rowList = new List<string>(rowData);
                int key = int.Parse(rowList[0]);
                csvData.Add(key, rowList);
                rowIndex++;
            }
        }
    }

    private object ConvertDataByType(string rawData, string dataType)
    {
        // Ignore leading and trailing whitespace and read in lowercase to reduce configuration table writing errors.
        switch (dataType.Trim().ToLowerInvariant())
        {
            case "string":
                return rawData;
            case "int":
                if (float.TryParse(rawData, out float intValue))
                    return (int)intValue;
                else
                    return 0;
            case "uint":
                if (float.TryParse(rawData, out float uintValue))
                    return (uint)uintValue;
                else
                    return 0u;
            case "float":
                if (float.TryParse(rawData, out float floatValue))
                    return floatValue;
                else
                    return 0f;
            case "bool":
                return rawData.ToLowerInvariant() == "true" || rawData == "1";
            case "stringlist":
                return new List<string>(rawData.Split('|'));
            case "intlist":
                var intList = rawData.Split('|').Select(str => float.TryParse(str, out float value) ? (int)value : 0).ToList();
                return intList;
            case "uintlist":
                var uintList = rawData.Split('|').Select(str => float.TryParse(str, out float value) ? (uint)value : 0u).ToList();
                return uintList;
            case "floatlist":
                var floatList = rawData.Split('|').Select(str => float.TryParse(str, out float value) ? value : 0f).ToList();
                return floatList;
            case "boollist":
                var boolList = rawData.Split('|').Select(item => item.ToLowerInvariant() == "true" || item == "1").ToList();
                return boolList;
            default:
                return null;
        }
    }


    public object GetData(uint id, string columnName)
    {
        if (csvData.ContainsKey((int)id) && columnMapping.ContainsKey(columnName))
        {
            int columnIndex = columnMapping[columnName];
            string rawData = csvData[(int)id][columnIndex];
            string dataType = dataTypes[columnIndex];

            return ConvertDataByType(rawData, dataType);
        }
        else
        {
            Debug.LogError($"Invalid input: id={id}, columnName={columnName}");
            return null;
        }
    }

    public string Encrypt(string data)
    {
        byte[] encrypted;
        using (Aes aes = Aes.Create())
        {
            aes.Key = Key;
            aes.IV = IV;

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter sw = new StreamWriter(cs))
                    {
                        sw.Write(data);
                    }
                    encrypted = ms.ToArray();
                }
            }
        }
        return Convert.ToBase64String(encrypted);
    }


    public string Decrypt(string data)
    {
        string decrypted = null;
        byte[] cipherText = Convert.FromBase64String(data);

        using (Aes aes = Aes.Create())
        {
            aes.Key = Key;
            aes.IV = IV;

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using (MemoryStream ms = new MemoryStream(cipherText))
            {
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader sr = new StreamReader(cs))
                    {
                        decrypted = sr.ReadToEnd();
                    }
                }
            }
        }
        return decrypted;
    }
}