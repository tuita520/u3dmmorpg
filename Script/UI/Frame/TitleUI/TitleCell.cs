using System.Collections;
using System;
using ClientDataModel;

#region using

using DataTable;
using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
	public class TitleCell : MonoBehaviour
	{
        public ListItemLogic ItemLogic;

        public void OnCliclMenuCell()
        {
            if (listItemLogic != null)
            {
                EventDispatcher.Instance.DispatchEvent(new UIEvent_TitleItemOption(5, listItemLogic.Index));//listItemLogic.Index
            }
            var menuData = ItemLogic.Item as TitleItemDataModel;
            if (menuData != null)
            {
                var e = new TitleMenuCellClick(menuData);
                EventDispatcher.Instance.DispatchEvent(e);
            }
          
        }

	    public void OnClickSubBranchCell()
	    {

            if (listItemLogic != null)
            {
                EventDispatcher.Instance.DispatchEvent(new TitleBranchCellClick(listItemLogic.Index));
            }
        }

	    public string AllianceName = "";
        public ListItemLogic listItemLogic;
        private bool mRefleshLayOut = true;
        public StackLayout mStackLayout;
        public OverheadTitleFrame titleLogic1;
	    public OverheadTitleFrame titleLogic2;

        public int TitleId
        {
            set
            {
                //Logger.Error("TitleId ------ {0}", value);
                var tbNameTitle = Table.GetNameTitle(value);
                var active = true;
               // if(transform.name!="TitleCell1(clone)")
                if (titleLogic1 != null)
                {
                    titleLogic1.SetTitle(tbNameTitle, AllianceName, ref active);
                }
                if (titleLogic2 != null)
                {
                    titleLogic2.SetTitle(tbNameTitle, AllianceName, ref active);
                }
                
                mRefleshLayOut = true;
                EventDispatcher.Instance.DispatchEvent(new UIEvent_GetTitleNum(value));
                if(this.isActiveAndEnabled == true)
                    StartCoroutine(DelayReposition());
            }
        }

        private void LateUpdate()
        {
#if !UNITY_EDITOR
    try
    {
#endif

            //if (mRefleshLayOut)
            //{
            //    if (mStackLayout != null)
            //    {
            //        mStackLayout.ResetLayout();
            //    }
            //    mRefleshLayOut = false;
            //}

#if !UNITY_EDITOR
    }
    catch (Exception ex)
    {
        Logger.Error(ex.ToString());
    }
#endif
        }

        public void OnClickPutOn()
        {
            if (listItemLogic != null)
            {
                EventDispatcher.Instance.DispatchEvent(new UIEvent_TitleItemOption(0, listItemLogic.Index));
            }
        }

        public void OnClickSelect()
        {
            if (listItemLogic != null)
            {
                EventDispatcher.Instance.DispatchEvent(new UIEvent_OnClickRankBtn(listItemLogic.Index));
            }
        }

        public void OnClickShowGet()
        {
            if (listItemLogic != null)
            {
                EventDispatcher.Instance.DispatchEvent(new UIEvent_TitleItemOption(1, listItemLogic.Index));
            }
        }

        // Use this for initialization
        private void OnEnable()
        {
#if !UNITY_EDITOR
    try
    {
#endif
            StartCoroutine(DelayReposition());
            //mRefleshLayOut = true;

#if !UNITY_EDITOR
    }
    catch (Exception ex)
    {
        Logger.Error(ex.ToString());
    }
#endif
        }

	    IEnumerator DelayReposition()
	    {
	        yield return new WaitForEndOfFrame();
            if (mStackLayout != null)
            {
                mStackLayout.ResetLayout();
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
    }
}