﻿using UnityEngine;
using System;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class ExportAtlasReference
{
    public UnityEngine.Object Res;


	[MenuItem("Assets/Export Atlas Reference")]

	public static void Find()
	{
		Dictionary<string, List<string>> dictLog = new Dictionary<string, List<string>>();

		var cs = EnumAssets.EnumComponentRecursiveInCurrentSelection<UISprite>();
		EditorUtility.DisplayProgressBar("Export Atlas Reference", "Collecting Component", 0);

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
					EditorUtility.DisplayProgressBar("Export Atlas Reference", c.gameObject.name, i * 1.0f / count);

					var atlas = c.atlas;
					if (null == atlas)
					{
						continue;
					}

					List<string> listString = null;
					if (!dictLog.TryGetValue(atlas.name, out listString))
					{
						listString = new List<string>();
						dictLog.Add(atlas.name, listString);
					}
					
					

					n++;
					var temp = "(" + c.spriteName + ")   " + c.transform.FullPath();
					listString.Add(temp);
					
				}
			}
		}

		EditorUtility.ClearProgressBar();

		string log = string.Empty;
		foreach(var pair in dictLog)
		{
			foreach(var str in pair.Value)
			{
				log += "[" + pair.Key + "]" + str + "\n";
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
		//Debug.Log("Using [" + AtlasName + "] Total=" + n.ToString() + "------------------------------------------end");

	}

    

}
