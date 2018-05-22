
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Linq;
using System.Threading;
using Assets.Script.Utility;
using FileMode = System.IO.FileMode;

namespace DataTable
{

    public static class Table_Tamplet
    {
        public static int Convert_Int(string _str)
        {
            int temp;
            if (Int32.TryParse(_str, out temp))
            {
                return temp;
            }
            //Logger.Error("Convert_Int Error!  {0}", _str);
            return 0;
        }

        public static float Convert_Float(string _str)
        {
            float temp;
            if (Single.TryParse(_str, out temp))
            {
                return temp;
            }
            //Logger.Error("Convert_Float Error!  {0}", _str);
            return 0;
        }

        public static double Convert_Double(string _str)
        {
            double temp;
            if (Double.TryParse(_str, out temp))
            {
                return temp;
            }
            //Logger.Error("Convert_Double Error!  {0}", _str);
            return 0;
        }
        public static string Convert_String(string _str)
        {
            return _str.Replace("\\n", "\n");
        }
        public static void Convert_Value(List<int> _col_name, string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return;
            }

            string[] temp = str.Split('|');
            {
                var __array1 = temp;
                var __arrayLength1 = __array1.Length;
                for (int __i1 = 0; __i1 < __arrayLength1; ++__i1)
                {
                    var s = __array1[__i1];
                    {
                        int temp_int = Convert.ToInt32(s);
                        _col_name.Add(temp_int);
                    }
                }
            }
        }
        public static void Convert_Value(List<List<int>> _col_name, string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return;
            }

            string[] temp1 = str.Split(';');
            Int16 i = 0;
            {
                var __array2 = temp1;
                var __arrayLength2 = __array2.Length;
                for (int __i2 = 0; __i2 < __arrayLength2; ++__i2)
                {
                    var ss = (string)__array2[__i2];
                    {
                        _col_name.Add(new List<int>());
                        string[] temp2 = ss.Split(',');
                        {
                            var __array3 = temp2;
                            var __arrayLength3 = __array3.Length;
                            for (int __i3 = 0; __i3 < __arrayLength3; ++__i3)
                            {
                                var s = (string)__array3[__i3];
                                {
                                    _col_name[i].Add(Convert.ToInt32(s));
                                }
                            }
                        }
                        ++i;
                    }
                }
            }
        }
        
        internal static long Convert_Long(string _str)
        {
            long temp;
            if (long.TryParse(_str, out temp))
            {
                return temp;
            }
            //Logger.Error("Convert_Int Error!  {0}", _str);
            return 0;
        }
    }
    public interface IRecord
    {
        void Init(string[] strs);
        object GetField(string name);
    }

    public static class TableInit
    {
        //加载表格
        public static void Table_Init(byte[] tableBytes, Dictionary<int, IRecord> _table_name, TableType type)
        {
            _table_name.Clear();
            var stream = new MemoryStream(tableBytes, false);

            TextReader tr = null;
            try
            {
                tr = new StreamReader(stream, Encoding.UTF8);
                Int32 state = 1;
                string str = tr.ReadLine();
                var NewFunc = Table.NewTableRecord(type);
                while (str != null)
                {
                    string[] strs = str.Split('\t');
                    string first = strs[0];
                    if (state == 1 && first == "INT")
                    {
                        state = 2;
                    }
                    else if (first.Substring(0, 1) == "#" || first == "" || first == " ") //跳过此行加载
                    {

                    }
                    else if (state == 2)
                    {
                        state = 3;
                    }
                    else if (state == 3)
                    {
                        var t = NewFunc();
                        t.Init(strs);
                        _table_name[Convert.ToInt32(first)] = t;
                    }
                    str = tr.ReadLine();
                }
            }
            catch (Exception ex)
            {
                //加入表格加载错误提示
                Debug.LogError("Load " + tableBytes + " Error!!");
                Debug.LogError(ex.Message);
                throw ex;
            }
            finally
            {
                if (tr != null)
                {
                    tr.Close();
                }
            }
        }
    }

    public class AsyncResult<T>
    {
        public T Result { get; set; }
    }

    public class TableManager
    {
        static RegisteredWaitHandle rhw;
        public static int LoadCount = 0;
        public static int MaxCount = 0;
        public static string CacheKey = Path.Combine(UpdateHelper.DownloadRoot, "TableCache/CacheKey.pkcs");

        public static int SaveCount = 0;
        public static int MaxSaveCount = 0;

        public static void InitTable(string tableName, Dictionary<int, IRecord> dictionary, TableType type)
        {
            var exist = CacheExist();
            if (GameSetting.Instance.ThreadLoadTable && exist)
            {
                if (MaxCount == 0)
                {
                    var max = GameSetting.Instance.ThreadCount;
                    ThreadPool.SetMaxThreads(max, max);
                    ThreadPool.SetMinThreads(2, 2);

                }
                MaxCount++;
                var helper = new LoadTableHelper(tableName, dictionary, type);
                ThreadPool.QueueUserWorkItem(helper.LoadTable);

                //----------------------------------sync test
//                 MaxCount++;
//                 var helper = new LoadTableHelper(tableName, dictionary, type);
//                 helper.LoadTable(0);
            }
            else
            {
                var path = "Table/" + tableName + ".txt";
                ResourceManager.PrepareResource<TextAsset>(path, asset =>
                {
                    if (asset == null)
                    {
                        Logger.Error("InitTable error! asset = null, assetname = {0}", path);
                        return;
                    }

                    TableInit.Table_Init((asset as TextAsset).bytes, dictionary, type);
                    ResourceManager.Instance.RemoveFromCache("Table/" + tableName + ".unity3d");
                }, true, false, true, true, true);
            }
        }

        public static IEnumerator SaveTableToCache()
        {
            if (!GameSetting.Instance.ThreadLoadTable)
            {
                yield break;
            }

            if (CacheExist())
            {
                yield break;
            }

            var tables = Table.GetTableNames();

            var tableNames = tables as string[] ?? tables.ToArray();
            MaxSaveCount = tableNames.Count();

            var job = new SaveFileThreadJob();
            for (var i = 0; i < MaxSaveCount; i++)
            {
                var tableName = tableNames[i];
                var path = string.Format("Table/{0}.txt", tableName);
                var bsync = i%20 != 0;
                ResourceManager.PrepareResource<TextAsset>(path, (res) =>
                {
                    var bytes = new byte[res.bytes.Length];
                    Buffer.BlockCopy(res.bytes, 0, bytes, 0, bytes.Length);
                    job.SaveFiles.Add(res.name, bytes);
                    SaveCount++;
                },true,false,bsync,true);
            }

            while (MaxSaveCount != SaveCount)
            {
                yield return new WaitForSeconds(0.1f);
            }

            job.Start();
            yield return ResourceManager.Instance.StartCoroutine(job.WaitFor());

        }

//         private static void WriteFileAsync(string tableName, byte[] content)
//         {
//             try
//             {
//                 var destName = string.Format("TableCache/{0}.txt", tableName);
//                 var destPath = Path.Combine(UpdateHelper.DownloadRoot, destName);
//                 UpdateHelper.CheckTargetPath(destPath);
//                 using (var fs = new FileStream(destPath, FileMode.Create))
//                 {
//                     Encrypte(content);
//                     fs.BeginWrite(content, 0, content.Length, EndWriteCallback, fs);
//                 }
//             }
//             catch (Exception ex)
//             {
//                 SaveCount++;
//                 Logger.Error("save table error! tablename :{0}, ex:{1}", tableName, ex);
//             }
//         }

//         private static void EndWriteCallback(IAsyncResult asr)
//         {
//             using (var str = (Stream) asr.AsyncState)
//             {
//                 str.EndWrite(asr);
//                 SaveCount++;
//                 if (MaxSaveCount != SaveCount || SaveCount <= 0) return;
//                 if (File.Exists(CacheKey)) return;
// 
//                 UpdateHelper.CheckTargetPath(CacheKey);
//                 File.WriteAllText(CacheKey, @"
//                     -----BEGIN PUBLIC KEY-----
//                     MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQC+3jnZ6NqvRxSh7t0AN3hAPlC1
//                     sGh9NTNIgGzd+3uWTYnRY8ns6k//NoQl34PqVdyd+IUrwz/n73opOEtHs7esZgRg
//                     vL78d0M306kidjcPvthUIVUKy1P1O3n1YOd3Wo+R1lMq/wH2Z2dlhb8cl7K5ykKH
//                     clMlNLhsh+Ay+wmLYwIDAQAB
//                     -----END PUBLIC KEY-----");
//             }
//         }

        public static bool IsFinish()
        {
            if (!GameSetting.Instance.ThreadLoadTable || !CacheExist())
            {
                return true;
            }
            return LoadCount == MaxCount && MaxCount != 0;
        }

        public static void Encrypte(byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= 0x38;
            }
        }

        public static void ClearTableCache()
        {
            if (File.Exists(CacheKey))
            {
                File.Delete(CacheKey);
            }
        }

        public static bool CacheExist()
        {
            return File.Exists(CacheKey);
        }
        

    }

    public class LoadTableHelper
    {
        private string tableName;
        private Dictionary<int, IRecord> dictionary;
        private TableType type;
        private static string basePath = Application.dataPath;
        public LoadTableHelper(string p, Dictionary<int, IRecord>  dic, TableType t)
        {
            tableName = p;
            dictionary = dic;
            type = t;
        }
        public void LoadTable(object state)
        {
            var path = string.Format("TableCache/{0}.txt", tableName);
            var realpath = Path.Combine(UpdateHelper.DownloadRoot, path);
            var threadid = Thread.CurrentThread.ManagedThreadId;
            try
            {
                var buffer = File.ReadAllBytes(realpath);
                TableManager.Encrypte(buffer);
                TableInit.Table_Init(buffer, dictionary, type);
            }
            catch (Exception e)
            {
                Debug.LogError("loadTable thread: " + threadid + "tablename:"+ tableName+ "Exception:" + e);
            }
            finally
            {
                Interlocked.Increment(ref TableManager.LoadCount);
            }
        }


    }
}

