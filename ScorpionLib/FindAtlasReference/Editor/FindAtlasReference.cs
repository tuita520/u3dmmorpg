using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class FindAtlasReference : ScriptableWizard
{
    [Tooltip("Atlas Name")]
    public string AtlasName;

    [Tooltip("Sprite Name")] 
    public string SpriteName;

    void Start()
    {

    }

	[MenuItem("Assets/Find Atlas Reference")]
    public static void OpenDialog()
    {
        DisplayWizard<FindAtlasReference>("Find object using this atlas", "Find", "Cancel");
    }

    void OnWizardCreate()
    {
	    int n = 0;
	    string log = "";
		n += Find1(ref log);
		n += Find2(ref log);
		n += Find3(ref log);

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

			FileStream fs = new FileStream(resultFile, FileMode.Create);
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
		EditorUtility.DisplayDialog("Find Atlas Reference：" + n, resultFile, "OK");
    }
    void OnWizardOtherButton()
    {
        Close();
    }


    public int Find1(ref string log)
    {
        if (string.IsNullOrEmpty(AtlasName))
        {
            return 0;
        }
        var temp = AtlasName.ToLower();
        var spriteNameTemp = SpriteName.ToLower();
        var cs = EnumAssets.EnumComponentRecursiveInCurrentSelection<UISprite>();
        EditorUtility.DisplayProgressBar("FindAtlasReference", "Collecting Component", 0);

        int count = cs.Count();
        int i = 0;
        int n = 0;
        
        {
            // foreach(var c in cs)
            var __enumerator1 = (cs).GetEnumerator();
            while (__enumerator1.MoveNext())
            {
                var c = __enumerator1.Current;
                {
                    i++;
                    EditorUtility.DisplayProgressBar(AtlasName, c.gameObject.name, i * 1.0f / count);
                    var atlas = c.atlas;
                    if (null == atlas)
                    {
                        continue;
                    }
                    var atlasName = atlas.name.ToLower();
                    if (atlasName.Contains(temp))
                    {
                        if (string.IsNullOrEmpty(SpriteName))
                        {
                            n++;
                            log += "(" + atlas.name + "/" + c.spriteName + ")   " + c.transform.FullPath() + "\n"; 
                        }
                        else
                        {
                            var spritename = c.spriteName.ToLower();
                            if (spritename.Equals(spriteNameTemp))
                            {
                                n++;
                                log += "(" + atlas.name + "/" + c.spriteName + ")   " + c.transform.FullPath() + "\n"; 
                            }
                        }
                    }
                }
            }
        }      
        
	    return n;
	    

    }

	public int Find2(ref string log)
    {
        if (string.IsNullOrEmpty(AtlasName))
        {
            return 0;
        }
        var temp = AtlasName.ToLower();

        var cs = EnumAssets.EnumComponentRecursiveInCurrentSelection<Particle2D>();
        EditorUtility.DisplayProgressBar("FindAtlasReference", "Collecting Component", 0);

        int count = cs.Count();
        int i = 0;
        int n = 0;
        {
            // foreach(var c in cs)
            var __enumerator3 = (cs).GetEnumerator();
            while (__enumerator3.MoveNext())
            {
                var c = __enumerator3.Current;
                {
                    i++;
                    EditorUtility.DisplayProgressBar(AtlasName, c.gameObject.name, i * 1.0f / count);
                    var atlas = c.atlas;
                    if (null == atlas)
                    {
                        continue;
                    }
                    var atlasName = atlas.name.ToLower();
                    if (atlasName.Contains(temp))
                    {
                        n++;
                        foreach (var str in c.sprites)
                        {
                            log += "(" + atlas.name + "/" + str + ")   " + c.transform.FullPath() + "\n";
                        }

                    }
                }
            }
        }

	    return n;
	    //Debug.Log("Using [" + AtlasName + "] Total=" + n.ToString() + "------------------------------------------end");

    }

	public int Find3(ref string log)
	{
		if (string.IsNullOrEmpty(AtlasName))
		{
			return 0;
		}
		var temp = AtlasName.ToLower();

		var cs = EnumAssets.EnumComponentRecursiveInCurrentSelection<UILabel>();
		EditorUtility.DisplayProgressBar("FindAtlasReference", "Collecting Component", 0);

		int count = cs.Count();
		int i = 0;
		int n = 0;
		{
			// foreach(var c in cs)
			var __enumerator3 = (cs).GetEnumerator();
			while (__enumerator3.MoveNext())
			{
				var c = __enumerator3.Current;
				{
					i++;
					EditorUtility.DisplayProgressBar(AtlasName, c.gameObject.name, i * 1.0f / count);
					var font = c.font;
					if (null == font)
					{
						continue;
					}
					var atlas = font.atlas;
					if (null == atlas)
					{
						continue;
					}
					var atlasName = font.atlas.name.ToLower();
					if (atlasName.Contains(temp))
					{
						n++;
						
						log += "(" + atlas.name  + ")   " + c.transform.FullPath() + "\n";
					}
				}
			}
		}

		return n;
	}

}
