#region using

using System;
using System.Collections.Generic;
using DataContract;
using DataTable;
using EventSystem;
using UnityEngine;

#endregion

public class DamageBoardManager : MonoBehaviour
{
    public int ActiveCount;
    private readonly List<IEvent> healEventList = new List<IEvent>();
    private readonly List<IEvent> reboundEventList = new List<IEvent>();
    private float timeInterval;
	private Camera uiCamera;

	public bool IsWorking
	{
		get; private set; 
	}

	public Camera UiCamera
	{
		get
		{
			if (null == uiCamera)
			{
				uiCamera = UIManager.Instance.UICamera;
			}
			return uiCamera;
		}
	}
    public static DamageBoardManager Instance { get; private set; }

    public static Vector2 MyPlayerCenterPos;

    private static readonly Queue<Action> ActionQueue = new Queue<Action>();
    private UIPanel panel;

	void Awake()
	{
#if !UNITY_EDITOR
try
{
#endif

		Instance = this;
		IsWorking = false;
	
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}
    private void IsShowMainUIEventCallBack(IEvent iEvent)
    {
        var v = iEvent as IsShowMainUIEvent;
        if (v != null)
		{
			IsShowDamageBoardPanel(v.IsShow);
		}
        
    }

    //�Ƿ���ʾ�˺�����
    public void IsShowDamageBoardPanel(bool isShow)
    {
        if (panel == null) panel = transform.GetComponent<UIPanel>();
        if (panel != null)
        {
            panel.enabled = isShow;
        }        
    }

    private void ComposeEvent()
    {
        var c = healEventList.Count;
        var c2 = reboundEventList.Count;
        var damage = 0;

        if (c != 0)
        {
            for (var i = 0; i < c; i++)
            {
                var e = healEventList[i] as ShowDamageBoardEvent;
                if (null != e)
                {
                    damage += e.Result.Damage;
                }
            }
            var ee = healEventList[0] as ShowDamageBoardEvent;
            ee.Result.Damage = damage;
            OnShowBuffBoardImpl(ee);
            healEventList.Clear();
        }

        if (c2 != 0)
        {
            for (var i = 0; i < c2; i++)
            {
                var e = reboundEventList[i] as ShowDamageBoardEvent;
                if (null != e)
                {
                    damage += e.Result.Damage;
                }
            }
            var ee = reboundEventList[0] as ShowDamageBoardEvent;
            ee.Result.Damage = damage;
            OnShowBuffBoardImpl(ee);
            reboundEventList.Clear();
        }
    }

    private void OnShowBuffBoard(IEvent ievent)
    {
        var e = ievent as ShowDamageBoardEvent;
        var result = e.Result;
        var targetCharacter = ObjManager.Instance.FindCharacterById(result.TargetObjId);

        if (targetCharacter != null && targetCharacter.GetObjType() == OBJ.TYPE.MYPLAYER)
        {
            if (e.Result.Type == BuffType.HT_HEALTH)
            {
                healEventList.Add(ievent);
                return;
            }
            if (e.Result.Type == BuffType.HT_REBOUND)
            {
                reboundEventList.Add(ievent);
                return;
            }
        }
        OnShowBuffBoardImpl(ievent);
    }

    private void OnShowBuffBoardImpl(IEvent ievent)
    {
        if (ActiveCount >= 256)
        {
            return;
        }


        var e = ievent as ShowDamageBoardEvent;
        var result = e.Result;

        var tableId = -1;
        var strValue = result.Damage.ToString();

        var targetCharacter = ObjManager.Instance.FindCharacterById(result.TargetObjId);
        var casterCharacter = ObjManager.Instance.FindCharacterById(result.SkillObjId);

        var isShow = false;
        if (targetCharacter != null)
        {
            if (targetCharacter.GetObjType() == OBJ.TYPE.MYPLAYER)
            {
                isShow = true;
            }
            else if (targetCharacter.GetObjType() == OBJ.TYPE.RETINUE)
            {
                var retinue = targetCharacter as ObjRetinue;
                if (retinue != null && retinue.GetIsMe())
                {
                    isShow = true;
                }
            }
        }

        if (isShow == false && casterCharacter != null)
        {
            if (casterCharacter.GetObjType() == OBJ.TYPE.MYPLAYER)
            {
                isShow = true;
            }
            else if (casterCharacter.GetObjType() == OBJ.TYPE.RETINUE)
            {
                var retinue = casterCharacter as ObjRetinue;
                if (retinue != null && retinue.GetIsMe())
                {
                    isShow = true;
                }
            }
        }

        if (isShow == false)
        {
            return;
        }


        var directivityPos = false;
        switch (result.Type)
        {
            case BuffType.HT_NORMAL:
            {
                tableId = 6;
                if (casterCharacter != null && casterCharacter.GetObjType() == OBJ.TYPE.RETINUE)
                {
                    var retinue = casterCharacter as ObjRetinue;
                    if (retinue != null && retinue.GetIsMe())
                    {
                        tableId = 13;
                    }
                    else
                    {
                        tableId = 17;
                    }
                }
                else if (casterCharacter != null && casterCharacter.GetObjType() == OBJ.TYPE.MYPLAYER)
                {
                    if (targetCharacter != null && targetCharacter.GetObjType() == OBJ.TYPE.MYPLAYER)
                    {
                        tableId = 6;
                    }
                    else
                    {
                        directivityPos = true;
                        tableId = 3;
                    }
                }
                else
                {
                    if (targetCharacter != null && targetCharacter.GetObjType() == OBJ.TYPE.MYPLAYER)
                    {
                        tableId = 6;
                    }
                }
            }
                break;
            case BuffType.HT_CRITICAL:
            {
               // ignorePostion = true;
                tableId = 5;
                if (casterCharacter != null && casterCharacter.GetObjType() == OBJ.TYPE.RETINUE)
                {
                    var retinue = casterCharacter as ObjRetinue;
                    if (retinue != null && retinue.GetIsMe())
                    {
                        tableId = 13;
                    }
                    else
                    {
                        tableId = 17;
                    }
                }
                else if (casterCharacter != null && casterCharacter.GetObjType() == OBJ.TYPE.MYPLAYER)
                {
                    directivityPos = true;
                    tableId = 2;
                }
                else
                {
                    if (targetCharacter != null && targetCharacter.GetObjType() == OBJ.TYPE.MYPLAYER)
                    {
                        tableId = 5;
                    }
                }
            }
                break;
            case BuffType.HT_EXCELLENT:
            {
               // ignorePostion = true;
                tableId = 4;
                if (casterCharacter != null && casterCharacter.GetObjType() == OBJ.TYPE.RETINUE)
                {
                    var retinue = casterCharacter as ObjRetinue;
                    if (retinue != null && retinue.GetIsMe())
                    {
                        tableId = 13;
                    }
                    else
                    {
                        tableId = 17;
                    }
                }
                else if (casterCharacter != null && casterCharacter.GetObjType() == OBJ.TYPE.MYPLAYER)
                {
                    directivityPos = true;
                    tableId = 1;
                }
                else
                {
                    if (targetCharacter != null && targetCharacter.GetObjType() == OBJ.TYPE.MYPLAYER)
                    {
                        tableId = 4;
                    }
                }
            }
                break;
            case BuffType.HT_MISS:
            {
                tableId = 10;
                strValue = "";
            }
                break;
            case BuffType.HT_NODAMAGE:
            {
                tableId = 11;
                strValue = "";
            }
                break;
            case BuffType.HT_HEALTH:
            {
                tableId = 7;
            }
                break;
            case BuffType.HT_MANA:
            {
                tableId = 8;
            }
                break;
            case BuffType.HT_REBOUND:
            {
                tableId = 9;
            }
                break;
            case BuffType.HT_Fire_DAMAGE:
            {
                directivityPos = true;
                tableId = 20;
                if (casterCharacter != null && casterCharacter.GetObjType() == OBJ.TYPE.RETINUE)
                {
                    var retinue = casterCharacter as ObjRetinue;
                    if (retinue != null && retinue.GetIsMe())
                    {
                        tableId = 20;
                    }
                    else
                    {
                        tableId = 23;
                    }
                }
                else if (casterCharacter != null && casterCharacter.GetObjType() == OBJ.TYPE.MYPLAYER)
                {
                    if (targetCharacter != null && targetCharacter.GetObjType() == OBJ.TYPE.MYPLAYER)
                    {
                        tableId = 23;
                    }
                    else
                    {
                        tableId = 20;
                    }
                }
                else
                {
                    if (targetCharacter != null && targetCharacter.GetObjType() == OBJ.TYPE.MYPLAYER)
                    {
                        tableId = 23;
                    }
                }
            }
                break;
            case BuffType.HT_Ice_DAMAGE:
            {
                directivityPos = true;
                if (casterCharacter != null && casterCharacter.GetObjType() == OBJ.TYPE.RETINUE)
                {
                    var retinue = casterCharacter as ObjRetinue;
                    if (retinue != null && retinue.GetIsMe())
                    {
                        tableId = 21;
                    }
                    else
                    {
                        tableId = 24;
                    }
                }
                else if (casterCharacter != null && casterCharacter.GetObjType() == OBJ.TYPE.MYPLAYER)
                {
                    if (targetCharacter != null && targetCharacter.GetObjType() == OBJ.TYPE.MYPLAYER)
                    {
                        tableId = 24;
                    }
                    else
                    {
                        tableId = 21;
                    }
                }
                else
                {
                    if (targetCharacter != null && targetCharacter.GetObjType() == OBJ.TYPE.MYPLAYER)
                    {
                        tableId = 24;
                    }
                }
            }
                break;
            case BuffType.HT_Poison_DAMAGE:
            {
                directivityPos = true;
                if (casterCharacter != null && casterCharacter.GetObjType() == OBJ.TYPE.RETINUE)
                {
                    var retinue = casterCharacter as ObjRetinue;
                    if (retinue != null && retinue.GetIsMe())
                    {
                        tableId = 22;
                    }
                    else
                    {
                        tableId = 25;
                    }
                }
                else if (casterCharacter != null && casterCharacter.GetObjType() == OBJ.TYPE.MYPLAYER)
                {
                    if (targetCharacter != null && targetCharacter.GetObjType() == OBJ.TYPE.MYPLAYER)
                    {
                        tableId = 25;
                    }
                    else
                    {
                        tableId = 22;
                    }
                }
                else
                {
                    if (targetCharacter != null && targetCharacter.GetObjType() == OBJ.TYPE.MYPLAYER)
                    {
                        tableId = 25;
                    }
                }
            }
                break;
            case BuffType.HT_ADDBUFF:
            case BuffType.HT_DIE:
            case BuffType.HT_EFFECT:
            case BuffType.HT_RELIVE:
            case BuffType.HT_DELBUFF:
            case BuffType.HT_CHANGE_SCENE:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        if (tableId == -1)
        {
            return;
        }

		//�ж϶����ʾ
	    int showTimes = 1;
	    int total = 0;
		var tableBuff = Table.GetBuff(result.BuffTypeId);
		if (null != tableBuff && tableBuff.DamageNumShowTimes > 0 && !string.IsNullOrEmpty(strValue))
	    {
			if (int.TryParse(strValue, out total))
			{
				showTimes = tableBuff.DamageNumShowTimes;
			}
	    }

        var position = e.Position;
        if (directivityPos)
        {
            var mountPoint = targetCharacter.GetMountPoint((int) MountPoint.Center);
            if (null != mountPoint)
            {
                position = mountPoint.position;
            }
            else
            {
                Logger.Warn("model does not have center mount point !model:{0}", targetCharacter.Name);
            }
        }


		if (showTimes <= 1 || total < showTimes)
	    {
		    ActiveCount++;
		    ComplexObjectPool.NewObject("UI/DamageBoard.prefab", o =>
		    {
				if (null == Instance)
				{
					ComplexObjectPool.Release(o);
					return;
				}
				if (false == IsWorking)
				{
					ComplexObjectPool.Release(o);
					return;
				}

			    var oTransform = o.transform;
			    oTransform.parent = transform;

			    //oTransform.SetParent(transform);
		        if (!o.activeSelf)
		        {
                    o.SetActive(false);
                    o.SetActive(true);
		        }

			    oTransform.localScale = Vector3.one;
			    oTransform.localPosition = Vector3.zero;
			    var logic = o.GetComponent<DamageBoardLogic>();
		        logic.BackGround.enabled = false;
		        logic.Label.text = string.Empty;

		        var delay = 0.0f;
		        if (tableId > 0)
		        {
		             var tbCombat = Table.GetCombatText(tableId);
                     if (null != tbCombat && tbCombat.DelayTime > 0)
		             {
                         delay = tbCombat.DelayTime / 1000.0f;
		             }
		        }
		      //  Action action = () =>
		      //  {
		            logic.StartAction(position, tableId, strValue, DamageBoardLogic.BoardShowType.Fight, false, delay,
		                directivityPos);
		      //  };

              //  ActionQueue.Enqueue(action);

              
		    }, null, null, false, false, false, tableId.ToString());
	    }
	    else
		{
			int remain = total;
			float per =  total*1.0f/showTimes;
		    for (int i = 0; i < showTimes; i++)
		    {
			    float frac = 0 == i%2 ? 1 : -1;
			    int damage = 0;
			    if (i == showTimes - 1)
			    {
					damage = remain;
			    }
			    else
			    {
					damage =(int)(per + frac * per * UnityEngine.Random.RandomRange(0.0f, 0.1f));
				    remain -= damage;
			    }

				ActiveCount++;
				ComplexObjectPool.NewObject("UI/DamageBoard.prefab", o =>
				{
					if (false == IsWorking)
					{
						ComplexObjectPool.Release(o);
						return;
					}
					var oTransform = o.transform;
					oTransform.parent = transform;
					//oTransform.SetParent(transform);
                    if (!o.activeSelf)
                    {
                        o.SetActive(false);
                        o.SetActive(true);
                    }
					oTransform.localScale = Vector3.one;
					oTransform.localPosition = Vector3.zero;
					var logic = o.GetComponent<DamageBoardLogic>();
				    var delay = 0.0f;
                    if (tableBuff != null)
                        delay = i * (tableBuff.DamageDeltaTime / 1000.0f);
                    else
                        delay = i * 0.38f;

                    if (tableId > 0)
                    {
                        var tbCombat = Table.GetCombatText(tableId);
                        if (null != tbCombat && tbCombat.DelayTime > 0)
                        {
                            delay += tbCombat.DelayTime / 1000.0f;
                        }
                    }
				//    Action action = () =>
				//    {
				        logic.StartAction(position, tableId, damage.ToString(), DamageBoardLogic.BoardShowType.Fight,
				            false, delay, directivityPos);
				//    };

                //    ActionQueue.Enqueue(action);

				}, null, null, false, false, false, tableId.ToString());
		    }
	    }
    }

	public void Init()
	{
		EventDispatcher.Instance.AddEventListener(ShowDamageBoardEvent.EVENT_TYPE, OnShowBuffBoard);
		EventDispatcher.Instance.AddEventListener(IsShowMainUIEvent.EVENT_TYPE, IsShowMainUIEventCallBack);
		IsWorking = true;
	}

	public void Cleanup()
	{
		healEventList.Clear();
		reboundEventList.Clear();
		ActionQueue.Clear();
		EventDispatcher.Instance.RemoveEventListener(ShowDamageBoardEvent.EVENT_TYPE, OnShowBuffBoard);
		
		ActiveCount = 0;
		var tf = gameObject.transform;
		for (int i = 0; i < tf.childCount; i++)
		{
			GameObject.Destroy(tf.GetChild(i).gameObject);	
		}
		IsWorking = false;
	}

    private void Start()
    {
#if !UNITY_EDITOR
try
{
#endif


        Instance = this;
        MyPlayerCenterPos = new Vector2(Screen.width/2.0f , Screen.height/2.0f);
        panel = transform.GetComponent<UIPanel>();
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
    }

    private void OnDestroy()
    {
#if !UNITY_EDITOR
try
{
#endif
		Instance = null;
        EventDispatcher.Instance.RemoveEventListener(IsShowMainUIEvent.EVENT_TYPE, IsShowMainUIEventCallBack);
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

        timeInterval += Time.deltaTime;
        if (timeInterval > 1)
        {
            ComposeEvent();
            timeInterval -= 1;
        }

        var count = 0;

        while (ActionQueue.Count > 0)
        {
            if (count > 3)
            {
                break;
            }
            var act = ActionQueue.Dequeue();
            act();
            count ++;
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