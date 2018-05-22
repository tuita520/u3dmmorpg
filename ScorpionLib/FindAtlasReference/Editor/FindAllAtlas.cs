using System;
using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class FindAllAtlas : ScriptableWizard
{

	[MenuItem("Assets/Find All Atlas")]
    public static void OpenDialog()
    {
        var cs = EnumAssets.EnumComponentRecursiveInCurrentSelection<UISprite>();
        string resultFile = Application.dataPath + "/" + "Result.txt";
	    FindImpl(cs, resultFile);
    }

    [MenuItem("Assets/Find All Atlas in Playing")]
    public static void OpenDialog2()
    {
        if (Application.isPlaying)
        {
            var sprites = UIManager.Instance.MainUIFrame.GetComponentsInChildren<UISprite>(true);
            string resultFile = Application.dataPath + "/" + "ResultInPlaying.txt";

            FindImpl(sprites, resultFile);
        }
    }

    
    public static void FindImpl(IEnumerable<UISprite> cs, string output)
	{


        var sb = new StringBuilder();


        var dic = new Dictionary<string, string>();
        var dic2 = new Dictionary<string, int>();
        foreach(var c in cs)
        {
            var key = c.atlas.name;
           
            if(!string.IsNullOrEmpty(key))
            {
                if (!dic.ContainsKey(key))
                {
                    dic.Add(key, c.transform.FullPath());
                    dic2.Add(key,1);
                }
                else
                {
                    dic2[key] = dic2[key] + 1;
                }
            }
        }

        foreach (var pair in dic)
        {
            sb.AppendLine(string.Format("----{0}----count:{2}-----,path:{1}", pair.Key, pair.Value, dic2[pair.Key]));
        }

        var result = sb.ToString();



        try
        {
            FileStream fs = new FileStream(output, FileMode.Create);
            //获得字节数组
            byte[] data = System.Text.Encoding.Default.GetBytes(result);
            //开始写入
            fs.Write(data, 0, data.Length);
            //清空缓冲区、关闭流
            fs.Flush();
            fs.Close();

            Debug.Log(output);
            EditorUtility.DisplayDialog("输出结果在", output, "OK");
        }
        catch (IOException e)
        {
            Debug.Log(e.ToString());
        }
       

    }

}
