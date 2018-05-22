using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ScriptManager;
using DataTable;
using EventSystem;
using GameUI;

public class NewFunctionControl : MonoBehaviour
{
	public List<NewFuntcionButtonIcon> NewFuntcionBtn;
	private MainSceenMenuControl MenuControl;
	private NewFunctionAnimation NewFunctionAni;
	void Awake()
	{
#if !UNITY_EDITOR
try
{
#endif
		MenuControl = gameObject.GetComponent<MainSceenMenuControl>();
		NewFunctionAni = gameObject.GetComponent<NewFunctionAnimation>();
		NewFuntcionBtn.AddRange(transform.GetComponentsInChildren<NewFuntcionButtonIcon>().ToList());

#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

	void OnEnable()
	{
#if !UNITY_EDITOR
try
{
#endif

		if (!GameSetting.Instance.EnableNewFunctionTip)
		{
			return;
		}


		if (!EventDispatcher.Instance.HasEventListener(UIEvent_OpenNewFunctionEvent.EVENT_TYPE, OnEvent_OpenNewFunction))
		{
			EventDispatcher.Instance.AddEventListener(UIEvent_OpenNewFunctionEvent.EVENT_TYPE, OnEvent_OpenNewFunction);	
		}

		RefreshBtnState();

	
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

	// Use this for initialization
	void Start () {
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

	private void OnEvent_OpenNewFunction(IEvent ievent)
	{
		if (!GameSetting.Instance.EnableNewFunctionTip)
		{
			return;
		}

		var e = ievent as UIEvent_OpenNewFunctionEvent;
		if (null == e)
		{
			return;
		}

		ShowNewFunction(e.BtnName, e.CallBack);
	}
	
	// Update is called once per frame
	void Update () {
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
	public string TestBtn = "BtnEquip";

	[ContextMenu("OpenNewFunction")]
	public void Test()
	{
		var call = new Action(() =>
		{
			GuidanceRecord tb;
			if (GuideTrigger.s_newFunctionList.TryGetValue(TestBtn, out tb))
			{
				RefreshBtnState();
				GuideManager.Instance.StartGuide(tb.StepId);	
			}
		});
		ShowNewFunction(TestBtn, call);
	}

	void OnDestroy()
	{
#if !UNITY_EDITOR
try
{
#endif

		EventDispatcher.Instance.RemoveEventListener(UIEvent_OpenNewFunctionEvent.EVENT_TYPE, OnEvent_OpenNewFunction);

#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

	private void ShowNewFunction(string name, Action call = null)
	{
		if (null == NewFunctionAni)
		{
			Logger.Error("Need component [NewFunctionAnimation]");
			return;
		}

        if (!name.Equals("BtnMaYaWeapon"))
	    {
            MenuControl.ShowDownButton(true, true);
	    }
        else
        {
            MenuControl.ShowDownButton(false, true);
        }

		var cal1 = new Action( () =>
		{
			if (null != call)
			{
				call();	
			}
			RefreshBtnState();
		});

		foreach (var btn in NewFuntcionBtn)
		{
			if (btn.name.Equals(name))
			{
				NewFunctionAni.ShowNewFunction(name, btn.transform, cal1);
				break;
			}
		}
		
	}

	public void RefreshBtnState()
	{
		foreach (var btn in NewFuntcionBtn)
		{
			if (GuideTrigger.IsFunctionOpen(btn.name))
			{
				btn.LockBtn(false);
			}
			else
			{
				btn.LockBtn(true);
			}
		}
	}

    public bool IsNoticeMainDown()
    {
        foreach (var btn in NewFuntcionBtn)
        {
            if (GuideTrigger.IsFunctionOpen(btn.name))
            {
                if (btn.name == "BtnEquip" && (PlayerDataManager.Instance.WeakNoticeData.AppendNotice))
                {
                    return true;
                }
                if (btn.name == "BtnHnadBook" && (PlayerDataManager.Instance.NoticeData.HandBookTotal))
                {
                    return true;
                }
                if (btn.name == "BtnElf" &&
                    (PlayerDataManager.Instance.NoticeData.ElfDraw ||
                     PlayerDataManager.Instance.WeakNoticeData.ElfTotal))
                {
                    return true;
                }
                if (btn.name == "BtnSkill" &&
                    (PlayerDataManager.Instance.NoticeData.SkillTotal ||
                     PlayerDataManager.Instance.WeakNoticeData.SkillTotal))
                {
                    return true;
                }
                if (btn.name == "BtnWing" && (PlayerDataManager.Instance.NoticeData.WingAdvance || 
                    PlayerDataManager.Instance.WeakNoticeData.WingTraining))
                {
                    return true;
                }
                if (btn.name == "BtnSailingHarbor" && (PlayerDataManager.Instance.NoticeData.SailingNotice))
                {
                    return true;
                }
                if (btn.name == "BtnAchievement" && (PlayerDataManager.Instance.NoticeData.HasAchievement))
                {
                    return true;
                }
                if (btn.name == "BtnBattleUnion" && (PlayerDataManager.Instance.NoticeData.BattleList))
                {
                    return true;
                }
            }
        }
        return false;
    }

}
