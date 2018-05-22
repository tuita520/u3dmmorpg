using System;
#region using

using EventSystem;
using SignalChain;
using UnityEngine;


#endregion

namespace GameUI
{
	public class AnimalInfo : MonoBehaviour, IChainRoot, IChainListener
	{
	    public BindDataRoot Binding;
	    public StackLayout Layout;
	    public GameObject ModelRoot;
	    public GameObject Model;
	    public UIDragRotate Drag;
	    private bool mFlag;
	
	    private void LateUpdate()
	    {
	#if !UNITY_EDITOR
	try
	{
	#endif
	
	        if (mFlag)
	        {
	            if (Layout)
	            {
	                Layout.ResetLayout();
	            }
	            mFlag = false;
	        }
	
	#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
	#endif
	    }
	
	    public void OnClickClose()
	    {
	        var e = new Close_UI_Event(UIConfig.ElfInfoUI);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnClickShowElf()
	    {
	        if (!GuideTrigger.IsFunctionOpen("BtnElf"))
	        {
                GameUtils.ShowHintTip(1726);
                return;
	        }
	        var e = new Show_UI_Event(UIConfig.ElfUI);
	        EventDispatcher.Instance.DispatchEvent(e);
	        var e1 = new Close_UI_Event(UIConfig.ElfInfoUI);
	        EventDispatcher.Instance.DispatchEvent(e1);
	    }
	
	    private void OnDisable()
	    {
	#if !UNITY_EDITOR
	try
	{
	#endif

            EventDispatcher.Instance.RemoveEventListener(ItemInfoMountModelDisplay_Event.EVENT_TYPE, ShowMountModel);
	        Binding.RemoveBinding();
	
	#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
	#endif
	    }
	
	    private void OnEnable()
	    {
	#if !UNITY_EDITOR
	try
	{
#endif
            EventDispatcher.Instance.AddEventListener(ItemInfoMountModelDisplay_Event.EVENT_TYPE, ShowMountModel);
	        var controllerBase = UIManager.Instance.GetController(UIConfig.ElfInfoUI);
	        if (controllerBase == null)
	        {
	            return;
	        }
	        Binding.SetBindDataSource(controllerBase.GetDataModel(""));
	        mFlag = true;
	#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
	#endif
	    }

        private void ShowMountModel(IEvent ievent)
        {
            var e = ievent as ItemInfoMountModelDisplay_Event;
            if (e != null && !string.IsNullOrEmpty(e.PerfabPath))
            {
                var prefabPath = e.PerfabPath;
                var animationPath = e.AnimationPath;
                if (Model != null)
                {
                    ComplexObjectPool.Release(Model);
                    Model = null;
                }
                ComplexObjectPool.NewObject(prefabPath, go =>
                {
                    if (ModelRoot == null)
                    {
                        return;
                    }
                    Model = go;
                    go.transform.parent = ModelRoot.transform;
                    go.transform.localPosition = Vector3.zero;
                    go.gameObject.SetLayerRecursive(LayerMask.NameToLayer(GAMELAYER.UI));
                    ModelRoot.GetComponent<ChangeRenderQueue>().RefleshRenderQueue();
                    Drag.Target = go.transform;
                    go.transform.localRotation = Quaternion.Euler(0, e.ShowAngle, 0);
                    go.transform.localScale = Vector3.one * e.Scale;

                    if (!string.IsNullOrEmpty(animationPath))
                    {
                        var ani = go.GetComponent<Animation>();
                        if (null == ani)
                        {
                            ani = go.AddComponent<Animation>();
                        }
                        ani.enabled = true;
                        var s = animationPath.LastIndexOf("/");
                        var l = animationPath.LastIndexOf(".anim");
                        var aniName = animationPath.Substring(s + 1, l - s - 1);
                        if (!string.IsNullOrEmpty(aniName))
                        {
                            var clip = ani.GetClip(aniName);
                            if (null == clip)
                            {
                                ResourceManager.PrepareResource<AnimationClip>(animationPath, aniClip =>
                                {
                                    aniClip.wrapMode = WrapMode.Loop;
                                    ani.AddClip(aniClip, aniName);
                                    ani.Play(aniName);
                                });
                            }
                            else
                            {
                                //clip.wrapMode = WrapMode.Loop;
                                //ani.wrapMode = WrapMode.Loop;
                                ani[aniName].wrapMode = WrapMode.Loop;
                                ani.Play(aniName, PlayMode.StopAll);
                            }
                        }
                    }

                });
            }
        }

	    private void Start()
	    {
	#if !UNITY_EDITOR
	try
	{
	#endif
	
	
	#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
	#endif
	    }
	
	    // Update is called once per frame
	    private void Update()
	    {
	#if !UNITY_EDITOR
	try
	{
	#endif
	
	
	#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
	#endif
	    }
	
	    public void Listen<T>(T message)
	    {
	        mFlag = true;
	    }
	}
}