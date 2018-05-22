#region using

using System;
using UnityEngine;

#endregion

public class BrightnessController : MonoBehaviour
{
    public GameObject BlockingLayer;
    private DateTime mLowLightTime;
    private readonly float mLowLightTimeInterval = 60.0f;
    private readonly float mLowValue = 0.1f;
    private bool mOrignalHdr;
    private bool mOrignalPostEffect;
	private bool mColorAdjustEffect;
    private BrightnessState mState = BrightnessState.Normal;
    // Use this for initialization
    private float mSysValue;

    public enum BrightnessState
    {
        Low,
        Normal
    }

    public void CreateBlockingLayer()
    {

        var root = GameUtils.GetUiRoot();

        if (null == root)
        {
            return;
        }

        var blockinglayer = root.transform.Find("BrightControllerBlockLayer(Clone)");
        if (null != blockinglayer)
        {
            return;
        }

        var blockRes = ResourceManager.PrepareResourceSync<GameObject>("UI/BrightControllerBlockLayer.prefab");
        if (null == blockRes)
        {
            Logger.Log2Bugly(
                "ResourceManager.PrepareResourceSync<GameObject>(UI/BrightControllerBlockLayer.prefab) return value is null!");
            return;
        }

        BlockingLayer = Instantiate(blockRes) as GameObject;

        if (null == BlockingLayer)
        {
            Logger.Log2Bugly(" BlockingLayer = GameObject.Instantiate(blockRes) as GameObject; return value is null!");
            return;
        }

         var t = BlockingLayer.transform;
         t.parent = root.transform;
         t.localScale = Vector3.one;
         t.localPosition = Vector3.zero;
         var panel = BlockingLayer.GetComponent<UIPanel>();
         panel.depth = 1000000;
         var eventTrigger = BlockingLayer.GetComponentInChildren<UIEventTrigger>();
        // eventTrigger.onRelease.Add(new EventDelegate(OnTouchOrMouseRelease));
         var boxCollider = BlockingLayer.GetComponentInChildren<BoxCollider>();
         boxCollider.center = Vector3.zero;
         boxCollider.isTrigger = true;
         var s = (float)root.activeHeight / Screen.height;
         boxCollider.size = new Vector3(Screen.width * s, Screen.height * s, 0f);

        var mask = t.FindChild("Mask");
        if (mask)
        {
#if UNITY_EDITOR
            mask.gameObject.SetActive(true);
#else
            mask.gameObject.SetActive(false);
#endif
        }
    }

    public BrightnessState GetBrightnessState()
    {
        return mState;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            SetLight();
        }
        else
        {
            mLowLightTime = DateTime.Now.AddSeconds(mLowLightTimeInterval);
        }
    }

    public void OnTouchOrMouseRelease()
    {
        SetLight();
    }

    public void RemoveBlockingLayer()
    {
        var root = GameUtils.GetUiRoot();

        var blockinglayer = root.transform.Find("BrightControllerBlockLayer(Clone)");
        if (blockinglayer)
        {
            Destroy(blockinglayer.gameObject);
            BlockingLayer = null;
        }
    }

    public void ResetTimer()
    {
        mLowLightTime = DateTime.Now.AddSeconds(mLowLightTimeInterval);
    }

    private void SetDark()
    {
        mLowLightTime = DateTime.MaxValue;
        if (mState == BrightnessState.Normal)
        {
            PlatformHelper.SetScreenBrightness(mLowValue);
            mState = BrightnessState.Low;

            // 屏幕变暗的时候，把后期效果去掉
            mOrignalPostEffect = GameSetting.Instance.EnablePostEffect;
			mColorAdjustEffect = GameSetting.Instance.EnableColorAdjuestEffect;
            mOrignalHdr = GameSetting.Instance.EnableHDR;
            GameSetting.Instance.EnablePostEffect = false;
			GameSetting.Instance.EnableColorAdjuestEffect = false;
            GameSetting.Instance.EnableHDR = false;
            Application.targetFrameRate = 30;
#if UNITY_EDITOR
            CreateBlockingLayer();
#endif
        }
    }

    private void SetLight()
    {
        if (mState == BrightnessState.Low)
        {
            PlatformHelper.SetScreenBrightness(mSysValue);
            mState = BrightnessState.Normal;

            var lowFps = PlayerPrefs.GetInt(GameSetting.LowFpsKey, 60);
            if (lowFps == 60)
            {
                Application.targetFrameRate = 60;
            }

            // 屏幕变亮的时候，把后期效果恢复
            GameSetting.Instance.EnablePostEffect = mOrignalPostEffect;
			GameSetting.Instance.EnableColorAdjuestEffect = mColorAdjustEffect;
            GameSetting.Instance.EnableHDR = mOrignalHdr;
        }
#if UNITY_EDITOR
        RemoveBlockingLayer();
#endif
    }

    private void Start()
    {
#if !UNITY_EDITOR
try
{
#endif

        mSysValue = PlatformHelper.GetScreenBrightness();

        mLowLightTime = DateTime.Now.AddSeconds(mLowLightTimeInterval);

        mOrignalPostEffect = GameSetting.Instance.EnablePostEffect;
		mColorAdjustEffect = GameSetting.Instance.EnableColorAdjuestEffect;
        mOrignalHdr = GameSetting.Instance.EnableHDR;

#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
    }

    private void Update()
    {
#if !UNITY_EDITOR
try
{
#endif

        if (Input.touchCount != 0 || Input.anyKey)
        {
            mLowLightTime = DateTime.Now.AddSeconds(mLowLightTimeInterval);
            SetLight();
        }

        if (!GameSetting.Instance.PowerSaveEnabe ||
			LoginLogic.State != LoginLogic.LoginState.InGaming)
        {
            return;
        }

        if (DateTime.Now > mLowLightTime)
        {
            SetDark();
        }

#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
    }
}