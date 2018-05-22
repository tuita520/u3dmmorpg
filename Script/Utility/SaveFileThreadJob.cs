using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Assets.Script.Utility
{
    public class SaveFileThreadJob : ThreadedJob
    {
        public Dictionary<string, byte[]> SaveFiles;

        public SaveFileThreadJob()
        {
            SaveFiles = new Dictionary<string, byte[]>();
        }

        protected override void ThreadFunction()
        {
            foreach (var saveFile in SaveFiles)
            {
                try
                {
                    var content = saveFile.Value;
                    var destName = string.Format("TableCache/{0}.txt", saveFile.Key);
                    var destPath = Path.Combine(UpdateHelper.DownloadRoot, destName);
                    UpdateHelper.CheckTargetPath(destPath);
                    using (var fs = new FileStream(destPath, FileMode.Create))
                    {
                        DataTable.TableManager.Encrypte(content);
                        fs.Write(content, 0, content.Length);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("save table error! tablename :{0}, ex:{1}", saveFile.Key, ex);
                }
            }
        }

        protected override void OnFinished()
        {
            var cacheKey = DataTable.TableManager.CacheKey;
            if (File.Exists(cacheKey)) return;
            UpdateHelper.CheckTargetPath(cacheKey);
            File.WriteAllText(cacheKey, @"
                    -----BEGIN PUBLIC KEY-----
                    MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQC+3jnZ6NqvRxSh7t0AN3hAPlC1
                    sGh9NTNIgGzd+3uWTYnRY8ns6k//NoQl34PqVdyd+IUrwz/n73opOEtHs7esZgRg
                    vL78d0M306kidjcPvthUIVUKy1P1O3n1YOd3Wo+R1lMq/wH2Z2dlhb8cl7K5ykKH
                    clMlNLhsh+Ay+wmLYwIDAQAB
                    -----END PUBLIC KEY-----");
        }
    }
}