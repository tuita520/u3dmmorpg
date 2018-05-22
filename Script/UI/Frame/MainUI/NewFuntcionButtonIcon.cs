using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DataTable;
using EventSystem;


public class NewFuntcionButtonIcon : MonoBehaviour
{
	private Transform Lock;
	private Transform NoticeIcon;
    private UIButton lockButton;
	void Awake()
	{
#if !UNITY_EDITOR
try
{
#endif

		Lock = transform.Find("Lock");
		NoticeIcon = transform.Find("NoticeIcon");
		LockBtn(false);
        if (null == Lock)
            return;
        lockButton = Lock.gameObject.GetComponent<UIButton>();
        if (null == lockButton)
        {
            lockButton = Lock.gameObject.AddComponent<UIButton>();
            lockButton.soundId = 9999;   
        }
        UIEventListener.Get(Lock.gameObject).onClick = OnClickFunctionLock;

#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

	public void LockBtn(bool flag)
	{
		if (null != Lock)
		{
			Lock.gameObject.SetActive(flag);				
		}
		if (null != NoticeIcon)
		{
			NoticeIcon.gameObject.SetActive(!flag);	
		}
		
	}

    public void OnClickFunctionLock(GameObject obj)
    {
        Table.ForeachGuidance(table =>
        {
            if (table.Name.Equals(gameObject.name))
            {
                EventDispatcher.Instance.DispatchEvent(new ShowUIHintBoard(table.OpenTips));
                return true;
            }
            return true;
        });
    }  

}
