#region using

using System;
using System.Collections.Generic;
using BehaviourMachine;
using ScriptManager;
using UnityEngine;

#endregion

public enum ObjState
{
    Uninited,
    LogicDataInited,
    LoadingResource,
    Normal,
    Dieing,
    Deleted
}

public class ObjBase : MonoBehaviour
{
    //模型GameObj名字
    public const string ModelName = "Model";
    private static int sUniqueResourceId = 12345;
    public static List<ObjCharacter> ObjVisibility = new List<ObjCharacter>(100);
    [NonSerialized]public int SmoothVisibilityFrameCount = 0;
    protected Action LoadResourceAction;
    [NonSerialized]public ObjState State = ObjState.Uninited;
    [NonSerialized]public int UniqueResourceId;
    [NonSerialized]public int DistSqr;

    public static int GetNextUniqueResourceId()
    {
        return sUniqueResourceId++;
    }

    //类型
    public virtual OBJ.TYPE GetObjType()
    {
        return OBJ.TYPE.INVALID;
    }

    //是否是角色
    public virtual bool IsCharacter()
    {
        return false;
    }

    public static void UpdateVisiblity()
    {
        ObjVisibility.Sort((x, y) =>
        {
            // 0    角色
            // 1    npc
            // 2    怪物
            // 3    boss
            var tx = x.TableCharacter.Type;
            var ty = y.TableCharacter.Type;
            if (tx != ty)
            {
                if (tx == 2)
                    tx = 4;

                if (ty == 2)
                    ty = 4;

                if (x.GetObjType() == OBJ.TYPE.RETINUE)
                {
                    if ((x as ObjRetinue).GetIsMe())
                    {
                        tx = 1;
                    }
                    else
                    {
                        tx = 4;
                    }
                }

                if (y.GetObjType() == OBJ.TYPE.RETINUE)
                {
                    if ((y as ObjRetinue).GetIsMe())
                    {
                        ty = 1;
                    }
                    else
                    {
                        ty = 4;
                    }
                }

                return tx - ty;
            }
            else
            {
                return x.DistSqr - y.DistSqr;
            }
        });

        const int smoothFrameCount = 3;

        var length = ObjVisibility.Count;
        var count = 10 + GameSetting.Instance.MaxVisibleModelCount / 2;
        for (int i = 0; i < length; i++)
        {
            var obj = ObjVisibility[i];
            if (obj.mRenderer != null)
            {
                var enable = (i < count);
                if (obj.mRenderer.enabled)
                {
                    if (enable)
                    {
                        obj.SmoothVisibilityFrameCount = 0;
                    }
                    else
                    {
                        obj.SmoothVisibilityFrameCount--;
                        if (obj.SmoothVisibilityFrameCount < -smoothFrameCount)
                        {
                            obj.mRenderer.enabled = false;
                            if (obj.mShadow) obj.mShadow.enabled = false;
                            var c = obj.mOtherRenderers.Count;
                            for (int j = 0; j < c; j++)
                            {
                                var r = obj.mOtherRenderers[j];
                                if (r)
                                    r.enabled = false;
                            }

                            obj.SmoothVisibilityFrameCount = 0;
                        }
                    }
                }
                else
                {
                    if (enable)
                    {
                        obj.SmoothVisibilityFrameCount++;
                        if (obj.SmoothVisibilityFrameCount > smoothFrameCount)
                        {
                            obj.mRenderer.enabled = true;
                            if (obj.mShadow) obj.mShadow.enabled = true;
                            var c = obj.mOtherRenderers.Count;
                            for (int j = 0; j < c; j++)
                            {
                                var r = obj.mOtherRenderers[j];
                                if (r)
                                    r.enabled = true;
                            }
                            obj.SmoothVisibilityFrameCount = 0;
                        }
                    }
                    else
                    {
                        obj.SmoothVisibilityFrameCount = 0;
                    }
                }
            }
        }

        ObjVisibility.Clear();
    }

    #region 基本属性

    //Obj Id
    protected ulong mObjId;

    //Data Id
    protected int mDataId;

    //Transform (缓存Transform引用)
    protected Transform mTransform;

    #endregion

    #region 逻辑属性

    #endregion

    #region 组件

    //可见模型
    protected GameObject mModel;
    protected Transform mModelTransform;
    protected Renderer mRenderer;
    protected AnimationUpdateFrequencyController mAnimationUpdateFrequencyController;
    protected List<Renderer> mOtherRenderers = new List<Renderer>();
    protected BlobShadow mShadow;
    #endregion

    #region 基本属性方法

    //ObjId
    public virtual void SetObjId(ulong objId)
    {
        mObjId = objId;
    }

    public virtual ulong GetObjId()
    {
        return mObjId;
    }

    //DataId 不同的类型Obj关联的表不一样
    public void SetDataId(int dataId)
    {
        mDataId = dataId;
    }

    public virtual int GetDataId()
    {
        return mDataId;
    }

    //Transform
    public Transform ObjTransform
    {
        get { return mTransform; }
    }

    //位置
    public Vector3 Position
    {
        get { return mTransform.position; }
        set
        {
            mTransform.position = value;
        }
    }

    //朝向
    public Vector3 Direction
    {
        get { return mTransform.forward; }
        set { mTransform.forward = value; }
    }

    //角速度
    protected float mAngularSpeed = Mathf.Deg2Rad*720;
    protected bool mDirectionDirty = true;
    private Vector3 mTargetDirection;

    public Vector3 TargetDirection
    {
        get { return mTargetDirection; }
        set
        {
            mDirectionDirty = true;
            mTargetDirection = value;
        }
    }

    //缩放
    public void SetScale(float scale)
    {
        if (mModel)
        {
            mModelTransform.localScale = new Vector3(scale, scale, scale);
        }
    }

    //获得模型
    public GameObject GetModel()
    {
        return mModel;
    }

    #endregion

    #region 组件方法

    //设置可见模型
    public virtual void SetModel(GameObject objModel)
    {
        if (objModel != null && mModel != null && mModel.GetInstanceID() != objModel.GetInstanceID())
        {
            mModelTransform.parent = null;
            ComplexObjectPool.Release(mModel);
            mModelTransform = null;
            mModel = null;
            return;
        }

        if (objModel == null)
        {
            ComplexObjectPool.Release(mModel);
            mModelTransform = null;
            mModel = null;
            return;
        }

        if (mModel != objModel)
        {
            ComplexObjectPool.Release(mModel);
        }

        mModel = objModel;
        mModelTransform = mModel.transform;

        if (!mModel.GetComponent<MaterialDrawer>())
        {
            var mat = mModel.AddComponent<MaterialDrawer>();
            {
                OptList<Renderer>.List.Clear();
                mModel.GetComponentsInChildren(OptList<Renderer>.List);
                var __arrayLength1 = OptList<Renderer>.List.Count;
                for (var __i1 = 0; __i1 < __arrayLength1; ++__i1)
                {
                    var render = OptList<Renderer>.List[__i1];
                    {
                        mat.OriginalMaterials[render.GetInstanceID()] = render.sharedMaterial;
                    }
                }
            }
        }
        else
        {
            var mat = mModel.GetComponent<MaterialDrawer>();
            {
                OptList<Renderer>.List.Clear();
                mModel.GetComponentsInChildren(OptList<Renderer>.List);
                var __arrayLength2 = OptList<Renderer>.List.Count;
                for (var __i2 = 0; __i2 < __arrayLength2; ++__i2)
                {
                    var render = OptList<Renderer>.List[__i2];
                    {
                        Material material;
                        if (mat.OriginalMaterials.TryGetValue(render.GetInstanceID(), out material))
                        {
                            Destroy(render.material);
                            render.material = material;
                        }
                    }
                }
            }
        }

        mModel.name = ModelName;
        mModelTransform.parent = ObjTransform;
        mModelTransform.localScale = Vector3.one;
        mModelTransform.localRotation = Quaternion.identity;
        mModelTransform.localPosition = Vector3.zero;
        mRenderer = mModel.GetComponentInChildren<SkinnedMeshRenderer>();
        mModel.GetComponentsInChildren<Renderer>(mOtherRenderers);
        for (int i = 0; i < mOtherRenderers.Count; i++)
        {
            if (mOtherRenderers[i] == mRenderer)
            {
                mOtherRenderers.RemoveAt(i);
                i--;
            }
        }
        mAnimationUpdateFrequencyController = mModel.GetComponent<AnimationUpdateFrequencyController>();

        OnSetModel();
    }

    //当模型改变时
    protected virtual void OnSetModel()
    {
    }

    #endregion

    #region Mono脚本接口

    protected virtual void Awake()
    {
#if !UNITY_EDITOR
        try
        {
#endif

        mTransform = gameObject.transform;

#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }

    // Use this for initialization
    protected virtual void Start()
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

        Tick(Time.deltaTime);

#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }

    protected virtual void OnDestroy()
    {
#if !UNITY_EDITOR
        try
        {
#endif

        ComplexObjectPool.Release(mModel);
        mModel = null;
        ComplexObjectPool.Release(gameObject);

#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }

    #endregion

    #region 自身逻辑

    //初始化
    public virtual bool Init(InitBaseData initData, Action callback = null)
    {
        State = ObjState.LogicDataInited;
        mObjId = initData.ObjId;
        mDataId = initData.DataId;
        return true;
    }

    //重置
    public virtual void Reset()
    {
    }

    //每帧
    protected virtual void Tick(float delta)
    {
    }

    //销毁
    public virtual void Destroy()
    {
        OptList<EffectController>.List.Clear();
        gameObject.GetComponentsInChildren(OptList<EffectController>.List);
        {
            var __array3 = OptList<EffectController>.List;
            var __arrayLength3 = __array3.Count;
            for (var __i3 = 0; __i3 < __arrayLength3; ++__i3)
            {
                var effect = __array3[__i3];
                {
                    effect.trans.parent = null;
                    EffectManager.Instance.StopEffect(effect.Id);
                    
                    //ComplexObjectPool.Release(effect.gameObject);
                }
            }
        }
        ComplexObjectPool.Release(mModel);
        mModel = null;
        mRenderer = null;
        mOtherRenderers.Clear();
		mShadow = null;
        SmoothVisibilityFrameCount = 0;
        DistSqr = 0;
        if (mAnimationUpdateFrequencyController != null)
        {
            mAnimationUpdateFrequencyController.UpdateFrequency = 0;
            mAnimationUpdateFrequencyController = null;
        }


        StopAllCoroutines();
        ComplexObjectPool.Release(gameObject);
        State = ObjState.Deleted;
        UniqueResourceId = 0;
    }

    #endregion
}

public class MaterialDrawer : MonoBehaviour
{
    public Dictionary<int, Material> OriginalMaterials = new Dictionary<int, Material>();
}