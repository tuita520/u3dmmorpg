using UnityEngine;
using System.Collections;
using System;
using ClientDataModel;
using DataTable;
using EventSystem;

public class QuickAutoUseItem1 : MonoBehaviour
{    
    public GameObject EquipedBtn;

    private float _tickTime = 9f;
    private int _showTime = 0;

    private UILabel mlabel;

    public void Start()
    {
#if !UNITY_EDITOR
try
{
#endif

        mlabel = GetComponent<UILabel>();
    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}


    public void PutOnClick()
    {
        _tickTime = 9f;
    }



    public void OnDisable()
    {
#if !UNITY_EDITOR
	        try
	        {
#endif
        _tickTime = 9f;
        _showTime = -1;

#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
#endif
    }

    public void Update()
    {
#if !UNITY_EDITOR
	        try
	        {
#endif

        if (_tickTime < 0)
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
                if (EquipedBtn.activeSelf)
                {
                    EventDispatcher.Instance.DispatchEvent(new UIEvent_HintEquipEvent(1));
                    _tickTime = 9f;
                }                                
                return;
            }
            mlabel.text = "(" + _showTime + GameUtils.GetDictionaryText(1045) + ")";
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
