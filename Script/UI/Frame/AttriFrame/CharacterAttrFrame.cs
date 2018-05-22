using DataTable;
using ScriptManager;
using System;
#region using

using ClientDataModel;
using EventSystem;
using UnityEngine;

#endregion

namespace GameUI
{
	public class CharacterAttrFrame : MonoBehaviour
	{
	    public GameObject AttriBtn;
	    public GameObject AttributeInfo;
	    public BindDataRoot BindRoot;
	    public ObjFakeCharacter Character;
	    public UIDragRotate ModelDrag;
	    public CreateFakeCharacter ModelRoot;
	    public OverheadTitleFrame[] overhead;
	    public void AddEvent()
	    {

	        EventDispatcher.Instance.AddEventListener(MyEquipChangedEvent.EVENT_TYPE, OnSelfEquipChanged);
	        BindRoot.SetBindDataSource(PlayerDataManager.Instance.PlayerDataModel);
	        BindRoot.SetBindDataSource(UIManager.Instance.GetController(UIConfig.WingUI).GetDataModel(""));
	        BindRoot.SetBindDataSource(UIManager.Instance.GetController(UIConfig.ElfUI).GetDataModel(""));
	        BindRoot.SetBindDataSource(UIManager.Instance.GetController(UIConfig.RebornUi).GetDataModel(""));
            EventDispatcher.Instance.AddEventListener(ShowStarDetailEvent.EVENT_TYPE, ShowStarDetail);

	    }
	
	    public void CreateCharacterModel()
	    {
	        DestroyCharacterModel();
	
	        var player = ObjManager.Instance.MyPlayer;
	        var elfId = -1;
	        var elfColorId = -1;
            GameUtils.GetFightElfId(ref elfId, ref elfColorId);
	        ModelRoot.Create(player.GetDataId(), player.EquipList, character => { ModelDrag.Target = character.transform; },
                elfId, true, 5, false, -1, elfColorId);
	    }
	
	    public void DestroyCharacterModel()
	    {
	        ModelRoot.DestroyFakeCharacter();
	    }
	
	    public void OnClickElf()
	    {
	        var e = new AttriFrameOperate(2);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }

        public void ShowStarDetail(IEvent ie)
        {
            var sde = ie as ShowStarDetailEvent;
            var tbNameTitle = Table.GetNameTitle(sde.TitleId);
            if (overhead != null)
            {
                var active = true;
                overhead[sde.LbIndex].SetTitle(tbNameTitle, tbNameTitle.Name, ref active);
            }
	    }
	    public void OnClickEquip()
	    {
	    }
	
	    public void OnClickReborn()
	    {
	        var e = new Show_UI_Event(UIConfig.RebornUi);
	        EventDispatcher.Instance.DispatchEvent(e);
	    }
	
	    public void OnClickWing()
	    {
	        var e = new AttriFrameOperate(1);
	        EventDispatcher.Instance.DispatchEvent(e);
	        PlayerDataManager.Instance.WeakNoticeData.BagEquipWing = false;
	    }
        public void OnClickStar()
        {
            var e = new ClickStarEvent();
            EventDispatcher.Instance.DispatchEvent(e);

        }
	    private void OnDestroy()
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
	
	    public void OnDisable()
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
	
	    public void OnEnable()
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
	
	    public void OnSelfEquipChanged(IEvent ievent)
	    {
	        var evn = ievent as MyEquipChangedEvent;
	
	        if (null == ModelRoot.Character)
	        {
	            return;
	        }
	        ModelRoot.Character.GetComponent<ObjFakeCharacter>().ChangeEquip(evn.Part, evn.Item);
	    }
	
	    public void OpenAttributeInfo()
	    {
	        EventDispatcher.Instance.DispatchEvent(new Show_UI_Event(UIConfig.AttributeUI));
	    }

	
	    public void RemoveEvent()
	    {
	        BindRoot.RemoveBinding();
	        EventDispatcher.Instance.RemoveEventListener(MyEquipChangedEvent.EVENT_TYPE, OnSelfEquipChanged);
            EventDispatcher.Instance.RemoveEventListener(ShowStarDetailEvent.EVENT_TYPE, ShowStarDetail);
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
	}
}