using UnityEngine;
using System;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;


public class FindReference : ScriptableWizard
{
    public UnityEngine.Object Res;

    // Use this for initialization
    void Start()
    {

    }

	[MenuItem("Assets/Find Reference")]
    public static void OpenDialog()
    {
		DisplayWizard<FindReference>("Find Reference Object In Current Select", "Find", "Cancel");
    }

    void OnWizardCreate()
    {
        Find();
    }
    void OnWizardOtherButton()
    {
        Close();
    }


    public void Find()
    {
		EditorUtility.DisplayProgressBar("Find Reference", "Finding...", 0);

		var instanceId = Res.GetInstanceID();

		var gos = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);

	    string log = "";


	    int i = 0;
	    try
	    {
			foreach (var go in gos)
			{
				var assetPath = AssetDatabase.GetAssetPath(go.GetInstanceID());
				var paths = AssetDatabase.GetDependencies(new[] { assetPath });
				foreach (var path in paths)
				{
					var res = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
					if (res.GetInstanceID() == instanceId)
					{
						log+= assetPath;
						log += "\n";
					}
				}
				EditorUtility.DisplayProgressBar("Find Reference", assetPath, i / gos.Length);
				i++;
			}
	    }
	    catch (Exception e)
	    {
			Debug.Log(e.Message);
	    }
	    finally
	    {
			EditorUtility.ClearProgressBar();
	    }
		

		Debug.Log(log);
    }

    

}
