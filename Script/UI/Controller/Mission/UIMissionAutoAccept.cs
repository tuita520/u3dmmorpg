using System;
using UnityEngine;
using System.Collections;
using ScriptManager;
using EventSystem;
using Shared;

public class UIMissionAutoAccept : MonoBehaviour
{
    public UILabel Label;
    private float WaitTime = 9.0f;

    private float _tickTime = 9.0f;
    private int _showTime = 0;

    public void StartAccept(bool isAccept)
    {
        _tickTime = WaitTime;
        _showTime = -1;
    }

    public void OkClick()
    {
        _tickTime = -1;
        _showTime = -1;
    }

    void OnEnable()
    {
        bPause = false;
        EventDispatcher.Instance.AddEventListener(TipsShowEvent.EVENT_TYPE, OnRecvShowTipsEvent);
    }
    void OnDisable()
    {
#if !UNITY_EDITOR
	        try
	        {
#endif
        _tickTime = -1;
        _showTime = -1;
        EventDispatcher.Instance.RemoveEventListener(TipsShowEvent.EVENT_TYPE, OnRecvShowTipsEvent);
#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
#endif
    }

    private bool bPause = false;
    void OnRecvShowTipsEvent(IEvent ievent)
    {
        TipsShowEvent e = ievent as TipsShowEvent;
        if (e != null)
        {
            bPause = e.b;
        }
    }
    void Update()
    {
#if !UNITY_EDITOR
	        try
	        {
#endif
        if (_tickTime < 0 || bPause)
            return;

        _tickTime -= Time.deltaTime;
        var time = (int)Math.Ceiling(_tickTime);
        if (time != _showTime)
        {
            _showTime = time;
            if (time == 0)
            {
                _tickTime = -1;
                _showTime = -1;
                MissionManager.Instance.OperateCurrentMission();
                return;
            }
            Label.text = "(" + _showTime + GameUtils.GetDictionaryText(1045) + ")";
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
