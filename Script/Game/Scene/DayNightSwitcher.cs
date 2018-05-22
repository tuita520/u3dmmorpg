using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

public class DayNightSwitcher : MonoBehaviour
{
    // ������ͼ
    public Texture2D[] DayNear;
    public Texture2D[] DayFar;
    public Texture2D[] NightNear;
    public Texture2D[] NightFar;

    // RenderSetting
    public Color DayAmbientLight;
    public float DayFlareFadeSpeed;
    public float DayFlareStrength;
    public bool DayFog;
    public Color DayFogColor;
    public float DayFogDensity;
    public float DayFogEndDistance;
    public FogMode DayFogMode;
    public float DayFogStartDistance;
    public float DayHaloStrength;
    public Material DaySkybox;

    public Color NightAmbientLight;
    public float NightFlareFadeSpeed;
    public float NightFlareStrength;
    public bool NightFog;
    public Color NightFogColor;
    public float NightFogDensity;
    public float NightFogEndDistance;
    public FogMode NightFogMode;
    public float NightFogStartDistance;
    public float NightHaloStrength;
    public Material NightSkybox;

    // Perfabs
    public GameObject[] DayShowPerfabs;
    public GameObject[] NightShowPerfabs;


    private LightmapData[] dayLightMaps;
    private LightmapData[] nightLightMaps;

    void Start()
    {
#if !UNITY_EDITOR
try
{
#endif

        if ((DayNear.Length != DayFar.Length) || (NightNear.Length != NightFar.Length))
        {
            Debug.Log("In order for LightMapSwitcher to work, the Near and Far LightMap lists must be of equal length");
            return;
        }

        // Sort the Day and Night arrays in numerical order, so you can just blindly drag and drop them into the inspector
        //DayNear = DayNear.OrderBy(t2d => t2d.name, new NaturalSortComparer<string>()).ToArray();
        DayFar = DayFar.OrderBy(t2d => t2d.name, new NaturalSortComparer<string>()).ToArray();
        //NightNear = NightNear.OrderBy(t2d => t2d.name, new NaturalSortComparer<string>()).ToArray();
        NightFar = NightFar.OrderBy(t2d => t2d.name, new NaturalSortComparer<string>()).ToArray();

        // Put them in a LightMapData structure
        dayLightMaps = new LightmapData[DayNear.Length];
        for (int i = 0; i < DayNear.Length; i++)
        {
            dayLightMaps[i] = new LightmapData();
            dayLightMaps[i].lightmapNear = DayNear[i];
            dayLightMaps[i].lightmapFar = DayFar[i];
        }

        nightLightMaps = new LightmapData[NightNear.Length];
        for (int i = 0; i < NightNear.Length; i++)
        {
            nightLightMaps[i] = new LightmapData();
            nightLightMaps[i].lightmapNear = NightNear[i];
            nightLightMaps[i].lightmapFar = NightFar[i];
        }

        SetToDay();
    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

    #region Publics
    public void SetToDay()
    {
        LightmapSettings.lightmaps = dayLightMaps;

        //RenderSetting
        RenderSettings.ambientLight = DayAmbientLight;
        RenderSettings.flareFadeSpeed = DayFlareFadeSpeed;
        RenderSettings.flareStrength = DayFlareStrength;
        RenderSettings.fog = DayFog;
        RenderSettings.fogColor = DayFogColor;
        RenderSettings.fogDensity = DayFogDensity;
        RenderSettings.fogEndDistance = DayFogEndDistance;
        RenderSettings.fogMode = DayFogMode;
        RenderSettings.fogStartDistance = DayFogStartDistance;
        RenderSettings.haloStrength = DayHaloStrength;
        RenderSettings.skybox = DaySkybox;

        //Perfabs
        foreach (var data in DayShowPerfabs)
        {
            data.SetActive(true);
        }
        foreach (var data in NightShowPerfabs)
        {
            data.SetActive(false);
        }
    }

    public void SetToNight()
    {
        LightmapSettings.lightmaps = nightLightMaps;

        //RenderSetting
        RenderSettings.ambientLight = NightAmbientLight;
        RenderSettings.flareFadeSpeed = NightFlareFadeSpeed;
        RenderSettings.flareStrength = NightFlareStrength;
        RenderSettings.fog = NightFog;
        RenderSettings.fogColor = NightFogColor;
        RenderSettings.fogDensity = NightFogDensity;
        RenderSettings.fogEndDistance = NightFogEndDistance;
        RenderSettings.fogMode = NightFogMode;
        RenderSettings.fogStartDistance = NightFogStartDistance;
        RenderSettings.haloStrength = NightHaloStrength;
        RenderSettings.skybox = NightSkybox;

        //Perfabs
        foreach (var data in DayShowPerfabs)
        {
            data.SetActive(false);
        }
        foreach (var data in NightShowPerfabs)
        {
            data.SetActive(true);
        }
    }

    private void OnDestroy()
    {
#if !UNITY_EDITOR
	        try
	        {
#endif
        this.CancelInvoke("SetToDay");
        this.CancelInvoke("SetToNight");
#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
#endif
    }

    #endregion

    #region Debug
    [ContextMenu("Set to Night")]
    void Debug00()
    {
        SetToNight();
    }

    [ContextMenu("Set to Day")]
    void Debug01()
    {
        SetToDay();
    }
    #endregion
}
 
// From http://zootfroot.blogspot.dk/2009/09/natural-sort-compare-with-linq-orderby.html
public class NaturalSortComparer<T> : IComparer<string>, IDisposable
{
    private readonly bool isAscending;
 
    public NaturalSortComparer(bool inAscendingOrder = true)
    {
        this.isAscending = inAscendingOrder;
    }
 
    #region IComparer<string> Members
    public int Compare(string x, string y)
    {
        throw new NotImplementedException();
    }
    #endregion
 
    #region IComparer<string> Members
    int IComparer<string>.Compare(string x, string y)
    {
        if (x == y)
            return 0;
 
        string[] x1, y1;
 
        if (!table.TryGetValue(x, out x1))
        {
            x1 = Regex.Split(x.Replace(" ", ""), "([0-9]+)");
            table.Add(x, x1);
        }
 
        if (!table.TryGetValue(y, out y1))
        {
            y1 = Regex.Split(y.Replace(" ", ""), "([0-9]+)");
            table.Add(y, y1);
        }
 
        int returnVal;
 
        for (int i = 0; i < x1.Length && i < y1.Length; i++)
        {
            if (x1[i] != y1[i])
            {
                returnVal = PartCompare(x1[i], y1[i]);
                return isAscending ? returnVal : -returnVal;
            }
        }
 
        if (y1.Length > x1.Length)
        {
            returnVal = 1;
        }
        else if (x1.Length > y1.Length)
        {
            returnVal = -1;
        }
        else
        {
            returnVal = 0;
        }
 
        return isAscending ? returnVal : -returnVal;
    }
 
    private static int PartCompare(string left, string right)
    {
        int x, y;
        if (!int.TryParse(left, out x))
            return left.CompareTo(right);
 
        if (!int.TryParse(right, out y))
            return left.CompareTo(right);
 
        return x.CompareTo(y);
    }
    #endregion
 
    private Dictionary<string, string[]> table = new Dictionary<string, string[]>();
 
    public void Dispose()
    {
        table.Clear();
        table = null;
    }
}