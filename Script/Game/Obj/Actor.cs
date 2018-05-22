#region using

using System;
using System.Collections.Generic;
using DataTable;
using UnityEngine;

#endregion

public class Actor : ObjFakeCharacter
{
    private bool mIsDynamicShadow;
    private bool mIsMoving;
    public bool SyncDirectionFlag { get; set; }
	private GameObject ShadowRoot;
    protected override void Awake()
    {
#if !UNITY_EDITOR
        try
        {
#endif

        mNavMeshAgent = GetComponent<NavMeshAgent>();
        base.Awake();

		ShadowRoot = new GameObject("ShadowRoot");
	    ShadowRoot.transform.parent = gameObject.transform;
	    ShadowRoot.transform.localPosition = Vector3.zero;
		ShadowRoot.transform.localScale = Vector3.one;
		ShadowRoot.transform.localRotation = Quaternion.identity;
#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }

    public static Actor Create(int dateId,
                               Dictionary<int, int> equipList = null,
                               Action<Actor> callback = null,
                               int layer = 0,
                               bool sync = false,
                               bool dynamicShadow = false)
    {
        var initData = new InitOtherPlayerData
        {
            ObjId = 0,
            DataId = dateId,
            X = Offset += 10,
            Y = -1000,
            Z = -2000,
            DirX = 1,
            DirZ = 0,
            Name = "",
            IsDead = false,
            IsMoving = false,
            MoveSpeed = 0,
            Camp = 0,
            RobotId = 0ul
        };

        if (null != equipList)
        {
            {
                // foreach(var pair in equipList)
                var __enumerator2 = (equipList).GetEnumerator();
                while (__enumerator2.MoveNext())
                {
                    var pair = __enumerator2.Current;
                    {
                        initData.EquipModel.Add(pair.Key, pair.Value);
                    }
                }
            }
        }

        var go = ComplexObjectPool.NewObjectSync(Resource.PrefabPath.Actor);
        go.layer = layer;
        var character = go.GetComponent<Actor>();

        character.mIsDynamicShadow = dynamicShadow;
        character.SyncLoadModel = sync;
#if UNITY_EDITOR
        go.name = "Actor" + "_" + initData.ObjId;
#endif
        character.Init(initData, () => { callback(character); });
        go.SetActive(true);
        return character;
    }

    public override void Destroy()
    {
	    if (null != mNavMeshAgent)
	    {
			mNavMeshAgent.enabled = false;    
	    }
        base.Destroy();
    }

    public override OBJ.TYPE GetObjType()
    {
        return OBJ.TYPE.ACTOR;
    }

    //初始化
    public override bool Init(InitBaseData initData, Action callback = null)
    {
        State = ObjState.LogicDataInited;

        var init = initData as InitOtherPlayerData;

        //mObjId = init.ObjId;
        mObjId = ulong.MaxValue;
        mDataId = init.DataId;
        EquipList = new Dictionary<int, int>(init.EquipModel);
        //mNavMeshAgent.enabled = true;
        mIsMoving = false;
        SyncDirectionFlag = true;
        Position = new Vector3(initData.X, initData.Y, initData.Z);
        Direction = new Vector3(initData.DirX, 0, initData.DirZ);
        InitTableData();
        InitAnimation();
        PrepareAnimation(OBJ.CHARACTER_ANI.STAND);

        LoadResourceAction = () =>
        {
            if (State == ObjState.Deleted)
            {
                return;
            }
            State = ObjState.LoadingResource;
            LoadModelAsync(() =>
            {
                State = ObjState.Normal;
                if (GetAnimationController() == null)
                {
                    return;
                }
                InitEquip(SyncLoadModel);
                InitShadow(mIsDynamicShadow,
                    mIsDynamicShadow ? LayerMask.GetMask(GAMELAYER.CGMainPlayer) : LayerMask.GetMask(GAMELAYER.CG));
                PlayAnimation(OBJ.CHARACTER_ANI.STAND);
                
                if (null != callback)
                {
                    callback();
                }
            });
        };

        return true;
    }

    protected override void InitTableData()
    {
        TableCharacter = Table.GetCharacterBase(mDataId);

        var modelId = TableCharacter.CharModelID;
        CharModelRecord = Table.GetCharModel(modelId);
    }

    protected override void LoadModelAsync(Action callback = null)
    {
        var tableModel = Table.GetCharModel(TableCharacter.CharModelID);
        var modelPath = Resource.GetModelPath(tableModel.ResPath);

        UniqueResourceId = GetNextUniqueResourceId();
        var resId = UniqueResourceId;
        if (null != mActorAvatar)
        {
            mActorAvatar.Layer = gameObject.layer;
            mActorAvatar.RenderQueue = RenderQueue;
        }
        ComplexObjectPool.NewObject(modelPath, model =>
        {
            if (resId != UniqueResourceId)
            {
                ComplexObjectPool.Release(model);
                return;
            }
            if (State == ObjState.Deleted)
            {
                ComplexObjectPool.Release(model);
                return;
            }
            SetModel(model);
            mAnimationController.Animation = mModel.animation;
            mAnimationController.Animation.Stop();
            SetScale((float) tableModel.Scale);
            if (null != callback)
            {
                callback();
            }
        }, null, o =>
        {
            if (IsPlayerModel())
            {
                OptList<Renderer>.List.Clear();
                o.GetComponentsInChildren(true, OptList<Renderer>.List);
                {
                    var __array1 = OptList<Renderer>.List;
                    var __arrayLength1 = __array1.Count;
                    for (var __i1 = 0; __i1 < __arrayLength1; ++__i1)
                    {
                        var renderer = __array1[__i1];
                        {
                            renderer.enabled = false;
                        }
                    }
                }                
            }

        });
    }

    public void Move(Vector3 des, float speed, bool walk)
    {
        if (speed > 0)
        {
            mNavMeshAgent.speed = speed;
        }
        des.y = GameLogic.GetTerrainHeight(des.x, des.z);
        MoveTo(des);
		if (0 == GetDataId()||
			1 == GetDataId() ||
			2 == GetDataId())
	    {
			if (walk)
			{
				PlayAnimation(OBJ.CHARACTER_ANI.Walk); //低速
			}
			else
			{
				PlayAnimation(OBJ.CHARACTER_ANI.AttackMove); //高速
			}  
	    }
		else
	    {
			PlayAnimation(OBJ.CHARACTER_ANI.RUN); //低速
	    }
        
    }

    public override bool MoveTo(Vector3 vec, float offset = 0.05f, bool isSendFastReach = false)
    {
        mNavMeshAgent.enabled = true;
        mNavMeshAgent.destination = vec;

        mIsMoving = true;

        return true;
    }

    protected override void Tick(float Delta)
    {
        if (State == ObjState.LogicDataInited)
        {
            if (ObjManager.Instance.CanLoad() && LoadResourceAction != null)
            {
                LoadResourceAction();
            }
        }

        if (mIsMoving)
        {
            if (mNavMeshAgent.enabled)
            {
                if (mNavMeshAgent.remainingDistance <= 0.05)
                {
                    PlayAnimation(OBJ.CHARACTER_ANI.STAND);
                    mIsMoving = false;
                    TargetDirection = Direction;
                }
            }
            else
            {
                PlayAnimation(OBJ.CHARACTER_ANI.STAND);
                mIsMoving = false;
            }
        }
        else
        {
            if (SyncDirectionFlag)
            {
                Direction = Vector3.RotateTowards(Direction, TargetDirection, Delta*mAngularSpeed, 0);
            }

			if (null != ShadowRoot)
			{
				RaycastHit hit;
				var orgin = mTransform.position;
				orgin.y = 30;
				if (Physics.Raycast(orgin, Vector3.down, out hit, 100,
					LayerMask.GetMask(GAMELAYER.ShadowReceiver)))
				{
					ShadowRoot.transform.position = new Vector3(mTransform.position.x, hit.point.y, mTransform.position.z);
				}

			}
	       
        }
    }

	public virtual void PlayAnimation(int id, Action<string> action = null)
	{
		if (OBJ.CHARACTER_ANI.DIE == id)
		{
			//硬编码
			var anis = mActorAvatar.GetComponentsInChildren<Animation>();
			foreach (var ani in anis)
			{
				if (null != ani.GetClip("Dead"))
				{
					ani.CrossFade("Dead", 0.01f);
				}
			}
		}

	        base.PlayAnimation(id, action);
	    
	}

	public virtual void InitShadow(bool isDynamicShadow = false, int dynamicCullingMask = -1)
	{
		if (GameSetting.Instance.GameQualityLevel > 1)
		{
			if (isDynamicShadow)
			{
				//场景的meshtree改为进入场景后异步加载了，加载好后会resetshadow，所以这里就不创建角色阴影了
				if (null == GameLogic.Instance.Scene.MeshTree)
				{
					return;
				}

				//先清理，后添加
				var shadowRoot = GameLogic.Instance.Scene.ShadowRoot.transform;
				if (null != shadowRoot)
				{
					for (int i = 0; i < shadowRoot.childCount; i++)
					{
						var child = shadowRoot.GetChild(i);
						if (null != child)
						{
							GameObject.Destroy(child.gameObject);
						}
					}
				}

				var model = FindChildTransform(mModelTransform, "Center");
				GameLogic.Instance.Scene.CreateDynamicShadow(new Vector3(0, 0, 0), model.gameObject, mModel,
					mDestroyObjectsWhenDestroy, gameObject.layer, dynamicCullingMask);
			}
			else
			{
				GameLogic.Instance.Scene.CreateBlobShadow(ShadowRoot, CharModelRecord.ShadowSize, mDestroyObjectsWhenDie);
				var collider = gameObject.GetComponent<CapsuleCollider>();
				if (collider)
				{
					collider.radius = CharModelRecord.ShadowSize;
				}
			}
		}
	}
}