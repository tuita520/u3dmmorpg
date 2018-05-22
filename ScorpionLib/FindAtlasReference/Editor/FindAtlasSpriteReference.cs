using System;
using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class FindAtlasSpriteReference : ScriptableWizard
{
	public string SpriteName;
    // Use this for initialization
    void Start()
    {

    }

	[MenuItem("Assets/Find Sprite Reference")]
    public static void OpenDialog()
    {
		DisplayWizard<FindAtlasSpriteReference>("Find Sprite", "Find", "Cancel");
    }

    void OnWizardCreate()
    {
        Find1();
    }
    void OnWizardOtherButton()
    {
        Close();
    }


    public void Find1()
	{
		if (string.IsNullOrEmpty(SpriteName))
        {
            return;
        }
		var temp = SpriteName.ToLower();

        var cs = EnumAssets.EnumComponentRecursiveInCurrentSelection<UISprite>();
        EditorUtility.DisplayProgressBar("FindAtlasReference", "Collecting Component", 0);

        int count = cs.Count();
        int i = 0;
        int n = 0;
	    
        string log = string.Empty;
        {
            // foreach(var c in cs)
            var __enumerator1 = (cs).GetEnumerator();
            while (__enumerator1.MoveNext())
            {
                var c = __enumerator1.Current;
                {
                    i++;
					EditorUtility.DisplayProgressBar(SpriteName, c.gameObject.name, i * 1.0f / count);

					var spriteName = c.spriteName.ToLower();
	                if (null == c.atlas)
	                {
						Debug.LogError("Error Atlas " + c.transform.FullPath());
						continue;
	                }
					if (-1!=spriteName.IndexOf(temp,StringComparison.OrdinalIgnoreCase))
                    {
                        n++;
						log += "(" + c.atlas.name + "/" + c.spriteName + ")   " + c.transform.FullPath() + "\n";
                    }
                }
            }
        }

	    if (!string.IsNullOrEmpty(log))
	    {
		    Debug.Log(log);
	    }
	    else
	    {
			Debug.Log("No result");
	    }
		string resultFile = Application.dataPath + "/" + "Result.txt";
		try
		{
				
			FileStream fs = new FileStream(resultFile,  FileMode.Create);
			//获得字节数组
			byte[] data = System.Text.Encoding.Default.GetBytes(log);
			//开始写入
			fs.Write(data, 0, data.Length);
			//清空缓冲区、关闭流
			fs.Flush();
			fs.Close();
				
			Debug.Log(resultFile);
		}
		catch (IOException e)
		{
			Debug.Log(e.ToString());
		}
		EditorUtility.DisplayDialog("Find Sprite Reference：" + n, resultFile, "OK");

    }

}
