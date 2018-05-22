#region using

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataContract;
using DataTable;
using UnityEngine;

#endregion

// 人物换装
public class ActorAvatar : MonoBehaviour
{

    public class Vertices
    {
        private List<Vector3> verts = new List<Vector3>();
        private List<Vector2> uv = new List<Vector2>();
        private List<Vector3> normals = new List<Vector3>();
        private List<Vector2> uv1 = new List<Vector2>();
        private List<BoneWeight> boneWeights = new List<BoneWeight>();
        private List<int> triangles = new List<int>();
        private List<Vector4> colors = new List<Vector4>(Enumerable.Repeat(Vector4.zero, 15));

        public List<Vector4> Colors
        {
            get { return colors; }
        }

        public void Reset()
        {
            verts.Clear();
            uv.Clear();
            normals.Clear();
            uv1.Clear();
            boneWeights.Clear();
            triangles.Clear();
            colors = new List<Vector4>(Enumerable.Repeat(Vector4.zero, 15));
        }

        public void Add(Mesh mesh, Transform[] orginalBones, Dictionary<Transform, BoneInfo> mapping, int part, Vector4 offset)
        {
            int i = verts.Count;
            verts.AddRange(mesh.vertices);
            uv.AddRange(mesh.uv.Select(o => new Vector2(o.x * offset.z + offset.x, o.y * offset.w + offset.y)));
            normals.AddRange(mesh.normals);

            var vc = mesh.vertexCount;
            for (int j = 0; j < vc; j++)
            {
                uv1.Add(new Vector2(part*3, part*3 + 1));
            }

            var bw = mesh.boneWeights;
            for (int j = 0; j < vc; j++)
            {
                var w = bw[j];
                var w1 = new BoneWeight();
                w1.boneIndex0 = mapping[orginalBones[w.boneIndex0]].Index;
                w1.boneIndex1 = mapping[orginalBones[w.boneIndex1]].Index;
                w1.weight0 = w.weight0;
                w1.weight1 = w.weight1;
                boneWeights.Add(w1);
            }

            var tri = mesh.triangles;
            for (int j = 0; j < tri.Length; j++)
            {
                triangles.Add(tri[j] + i);
            }
        }

        public void Update(Mesh mesh)
        {
#if !UNITY_EDITOR
try
{
#endif

            mesh.triangles = null;
            mesh.vertices = verts.ToArray();
            mesh.uv = uv.ToArray();
            mesh.normals = normals.ToArray();
            mesh.boneWeights = boneWeights.ToArray();
            mesh.uv1 = uv1.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateBounds();
        
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

        internal void SetMaterialColor(Material mat)
        {
            for (int i = 0; i < Colors.Count; i++)
            {
                mat.SetVector(string.Format("_ColorParams{0}", i), colors[i]);
            }
        }
    }

    public class BoneInfo
    {
        public int Index;
        public Matrix4x4 BindPos;
    }

    protected Dictionary<string, AvatarInfo> _avatarInfo = new Dictionary<string, AvatarInfo>(); // 换装信息

    private readonly Queue<KeyValuePair<string, WaitingResource>> _avatarLoadQueue =
        new Queue<KeyValuePair<string, WaitingResource>>();

    private static int sUniqueResourceId; 
    public int UniqueResourceId;

    private static int GetNextResourceId()
    {
        return sUniqueResourceId++;
    }

    protected GameObject _body; // 基础模型动画
    protected int _bodyModelId;
    protected int _loading;
    private readonly Dictionary<int, List<GameObject>> _parts = new Dictionary<int, List<GameObject>>();
    public string BodyModel;
    public string ChestModel;
    public string FootModel;
    public string HandModel;
    public string HeadModel;
    public string LegModel;
    public string FashionModel;

    public bool FashionMode = false;

    public EquipModelViewRecord ChestModelViewRecord;
    public EquipModelViewRecord FootModelViewRecord;
    public EquipModelViewRecord HandModelViewRecord;
    public EquipModelViewRecord HeadModelViewRecord;
    public EquipModelViewRecord LegModelViewRecord;
    public EquipModelViewRecord FashionModelViewRecord;

    private Coroutine _combineCoroutine;
    private SkinnedMeshRenderer _combined;
    private Vertices _vertices;
    private Transform[] _bones = null;
    private Matrix4x4[] _bindpos = null;
    private Dictionary<Transform, BoneInfo> _mapping = new Dictionary<Transform,BoneInfo>();
    private bool _combineDirty = true;

    private int mRenderQueue = -1;
    private readonly Dictionary<int, string> mWeaponModels = new Dictionary<int, string>();
    public GameObject WingGameObject;
    public GameObject MountGameObject;

    public GameObject Body
    {
        get { return _body; }
        set
        {
            if (_body == value)
            {
                return;
            }

            _body = value;
            if (_body)
            {
                try
                {
                    InitAvatarInfo(transform);

                    if (_avatarInfo["Foot"].defaultPart != null)
                    {
                        if (_combineCoroutine != null)
                        {
                            StopCoroutine(_combineCoroutine);
                            _combineCoroutine = null;
                        }

                        if (!FashionMode)
                        {
                            if (ObjType == OBJ.TYPE.MYPLAYER || ObjType == OBJ.TYPE.OTHERPLAYER || ObjType == OBJ.TYPE.NPC)
                            {
                                _combineCoroutine = StartCoroutine(UpdateCobmine());
                            }
                        }
                    }
                }
                catch
                {
                    // can not find part, ignore it.
                }

                _body.SetRenderQueue(mRenderQueue);
            }
        }
    }

    private void InitAvatarInfo(Transform transform)
    {
        if (!transform)
        {
            if (_avatarInfo.Count == 0)
            {
                _avatarInfo.Add("Foot", new AvatarInfo {partName = "Foot"});
                _avatarInfo.Add("Chest", new AvatarInfo {partName = "Chest"});
                _avatarInfo.Add("Hand", new AvatarInfo {partName = "Hand"});
                _avatarInfo.Add("Head", new AvatarInfo {partName = "Head"});
                _avatarInfo.Add("Leg", new AvatarInfo {partName = "Leg"});
                _avatarInfo.Add("Fashion", new AvatarInfo { partName = "Fashion", NeedCombine = false});
            }
        }
        else
        {
            _avatarInfo.Clear();

            _avatarInfo.Add("Foot", InitOneAvatarInfo("Foot"));
            _avatarInfo.Add("Chest",InitOneAvatarInfo("Chest"));
            _avatarInfo.Add("Hand", InitOneAvatarInfo("Hand"));
            _avatarInfo.Add("Head", InitOneAvatarInfo("Head"));
            _avatarInfo.Add("Leg", InitOneAvatarInfo("Leg"));
            _avatarInfo.Add("Fashion", InitOneAvatarInfo("Fashion"));
        }
    }

    private AvatarInfo InitOneAvatarInfo(string part)
    {
        var info = new AvatarInfo();

        if (part == "Fashion")
        {
            info.NeedCombine = false;
        }

        var trans = FindChild(transform, part);
        if (!trans)
            return info;

        var go = trans.gameObject;
        info.defaultPart = go;

        var renderer = go.GetComponentInChildren<SkinnedMeshRenderer>();
        var mat = renderer.sharedMaterial;

        var offset = mat.GetTextureOffset("_MainTex");
        var scale = mat.GetTextureScale("_MainTex");
        info.Offset = new Vector4(offset.x, offset.y, scale.x, scale.y);

        if (mat.HasProperty("_Color"))
        {
            info.Color = mat.GetColor("_Color");
        }
        if (mat.HasProperty("_BColor"))
        {
            info.BColor = mat.GetColor("_BColor");
        }
        if (mat.HasProperty("_TexColor"))
        {
            info.TexColor = mat.GetColor("_TexColor");
        }

        return info;
    }

    public OBJ.TYPE ObjType { get; set; }
    public int Layer { get; set; }
    public int RenderQueue
    {
        get { return mRenderQueue; }
        set { mRenderQueue = value; }
    }

    private void AddEffect(Transform mp, GameObject obj, Vector3 p, Quaternion q, int part)
    {
        if (_body == null)
        {
            return;
        }

        var parent = mp;
        var o = obj;
        var objTransform = o.transform;
        objTransform.parent = parent;
        objTransform.localPosition = p;
        objTransform.localRotation = q;
        objTransform.localScale = Vector3.one;
        CorrectLayer(o);
        o.SetRenderQueue(mRenderQueue);
        _parts[part].Add(o);
    }

    private void CorrectLayer(GameObject o)
    {
        o.SetLayerRecursive(Layer);
        if (ObjType == OBJ.TYPE.MYPLAYER)
        {
            o.SetTagToLayer(GAMELAYER.IgnoreShadow, UnityEngine.LayerMask.NameToLayer(GAMELAYER.IgnoreShadow));
            o.SetTagToLayer(GAMELAYER.Collider, UnityEngine.LayerMask.NameToLayer(GAMELAYER.Collider));
        }
    }

    public void ChangeToFashionMode(string part, string model, EquipModelViewRecord record, bool sync)
    {
        ChangePart(part, model, record, sync);
        FashionMode = true;
        ChangePart("Foot", string.Empty, null, false, true);
        ChangePart("Chest", string.Empty, null, false, true);
        ChangePart("Hand", string.Empty, null, false, true);
        ChangePart("Head", string.Empty, null, false, true);
        ChangePart("Leg", string.Empty, null, false, true);
        // Hide Combined mesh
        if (_combined)
        {
            Destroy(_combined.sharedMesh);
            Destroy(_combined.sharedMaterial);
            Destroy(_combined.gameObject);
            Destroy(_combined);
            _vertices = null;
        }
    }

    public void RestoreNormalMode()
    {
        ChangePart((GameObject)null, "Fashion", null, false, false);
        FashionMode = false;
        ChangePart("Foot", FootModel, FootModelViewRecord, false, true);
        ChangePart("Chest", ChestModel, ChestModelViewRecord, false, true);
        ChangePart("Hand", HandModel, HandModelViewRecord, false, true);
        ChangePart("Head", HeadModel, HeadModelViewRecord, false, true);
        ChangePart("Leg", LegModel, LegModelViewRecord, false, true);
    }

    // 给人物换装
    public void ChangePart(string part, string model, EquipModelViewRecord record, bool sync, bool changingFashion = false)
    {
        if (!changingFashion)
            SetPartModel(part, model, record);

        // 如果还没有加载完基础模型，则等待
        if (_body == null || _loading > 0)
        {
            _avatarLoadQueue.Enqueue(new KeyValuePair<string, WaitingResource>(part,
                new WaitingResource { ModelPath = model, ModelViewRecord = record, ChangingFashion = changingFashion }));
            return;
        }

        // 如果当前没有穿过任何装备，则恢复默认装备
        if (string.IsNullOrEmpty(model))
        {
            ChangePart((GameObject)null, part, record, sync, !FashionMode);
            TryContinue();
            return;
        }

        if (FashionMode && part != "Fashion")
        {
            TryContinue();
            return;
        }

        //AvatarData adata = DataMgr.Instance.GetAvatarData(avatarId);
        _loading++;
        var resId = UniqueResourceId;
        ComplexObjectPool.NewObject(model, obj =>
        {
            _loading--;

            if (resId != UniqueResourceId)
            {
                ComplexObjectPool.Release(obj);
                TryContinue();
                return;
            }

            if (GetPartModel(part) != model)
            {
                ComplexObjectPool.Release(obj);
                TryContinue();
                return;
            }

            if (null != obj)
            {
                ChangePart(obj, part, record, sync);
            }
            TryContinue();
        }, null, null, sync, ObjType == OBJ.TYPE.MYPLAYER, true, null, ObjType == OBJ.TYPE.MYPLAYER);
    }

    // 替换部件
    private void ChangePart(GameObject avatarModel, string partName, EquipModelViewRecord record, bool sync, bool showDefault = true)
    {
        // 先卸载当前部件
        AvatarInfo currentInfo;
        if (_avatarInfo.TryGetValue(partName, out currentInfo))
        {
            if (currentInfo.avatarPart != null)
            {
                Destroy(currentInfo.avatarPart);
                currentInfo.avatarPart = null;
            }

            if (currentInfo.defaultPart != null)
            {
                currentInfo.defaultPart.SetActive(showDefault);
            }
        }

        // avatarModel是一个resource，并没有实例化
        if (avatarModel == null)
        {
            return;
        }

        if (_body == null)
        {
            ComplexObjectPool.Release(avatarModel);
            return;
        }

        // 需要替换的部件
        var avatarPart = GetPart(avatarModel.transform, partName);
        if (avatarPart == null)
        {
            ComplexObjectPool.Release(avatarModel);
			Logger.Error("{0} should contain a node name {1}",avatarModel.transform.name, partName);
            return;
        }

        // 将原始部件隐藏
        var bodyPart = GetPart(_body.transform, partName);
        if (bodyPart != null)
        {
            bodyPart.gameObject.SetActive(false);
        }

        // 设置到body上的新物件
        var newPart = new GameObject(partName);
        var partTransform = newPart.transform;
        partTransform.parent = _body.transform;
        partTransform.localPosition = Vector3.zero;
        partTransform.localRotation = Quaternion.Euler(0, 0, 0);
        var newPartRender = newPart.AddComponent<SkinnedMeshRenderer>();
        var avatarRender = avatarPart.GetComponent<SkinnedMeshRenderer>();
        {
            // foreach(var item in avatarPart.transform)
            var __enumerator6 = (avatarPart.transform).GetEnumerator();
            while (__enumerator6.MoveNext())
            {
                var item = (Transform) __enumerator6.Current;
                {
                    var child = Instantiate(item.gameObject) as GameObject;
                    if (null == child)
                    {
                        continue;
                    }
                    var childTransform = child.transform;
                    childTransform.parent = partTransform;
                    childTransform.localPosition = item.localPosition;
                    childTransform.localScale = item.localScale;
                    childTransform.localRotation = item.localRotation;
                }
            }
        }

        CorrectLayer(newPart);
        newPart.SetRenderQueue(mRenderQueue);

        // 刷新骨骼模型数据
        SetBones(newPart, avatarPart.gameObject, _body);
        newPartRender.sharedMesh = avatarRender.sharedMesh;
        newPartRender.sharedMaterial = avatarRender.sharedMaterial;

        ComplexObjectPool.Release(avatarModel);

        // 记录换装信息
        AvatarInfo info;
        if (!_avatarInfo.TryGetValue(partName, out info))
            info = new AvatarInfo();

        //if(!GameSetting.Instance.CombineCharacterMesh)
        if (SetMaterial(record, sync, newPartRender, partTransform)) return;

        info.partName = partName;
        if (record != null)
        {
            info.BColor = new Color(record.FlowRed/255.0f, record.FlowGreen/255.0f, record.FlowBlue/255.0f,
                record.FlowAlpha/255.0f);
            info.TexColor = new Color(record.SepcularRed/255.0f, record.SepcularGreen/255.0f, record.SepcularBlue/255.0f,
                record.SepcularAlpha/255.0f);
            info.Color = new Color(record.MainRed/255.0f, record.MainGreen/255.0f, record.MainBlue/255.0f, 1);
        }
        else
        {
            info.BColor = Color.black;
            info.TexColor = Color.black;
            info.Color = Color.white;
        }

        var offset = newPartRender.sharedMaterial.GetTextureOffset("_MainTex");
        var scale = newPartRender.sharedMaterial.GetTextureScale("_MainTex");
        info.Offset = new Vector4(offset.x, offset.y, scale.x, scale.y);
        if (bodyPart != null)
        {
            info.defaultPart = bodyPart.gameObject;
        }
        else
        {
            info.defaultPart = null;
        }

        info.avatarPart = newPart;
        _avatarInfo[partName] = info;

        if (partName != "Fashion")
        {
            _combineDirty = true;
        }
        else
        {
            info.NeedCombine = false;
        }

        //UpdateCobmine();

        // 检查Body的Animation的CullType， 否则会出现当所有装备都卸下时，动画没有了的问题
        _body.animation.cullingType = AnimationCullingType.BasedOnUserBounds;
        _body.animation.localBounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1, 2, 1));

        try
        {
            if (_combineCoroutine != null)
            {
                StopCoroutine(_combineCoroutine);
                _combineCoroutine = null;
            }
            if (_combineDirty && !FashionMode)
            {
                if (gameObject && gameObject.activeSelf)
                {
                    if (ObjType == OBJ.TYPE.MYPLAYER || ObjType == OBJ.TYPE.OTHERPLAYER || ObjType == OBJ.TYPE.NPC)
                        _combineCoroutine = StartCoroutine(UpdateCobmine());
                }
            }
            
        }
        catch (Exception ex)
        {
        }
    }

    private bool SetMaterial(EquipModelViewRecord record, bool sync, SkinnedMeshRenderer newPartRender,
        Transform partTransform)
    {
        const string MainTexVariableName = "_MainTex";
        if (ObjType == OBJ.TYPE.MYPLAYER)
        {
            var resId = UniqueResourceId;
            _loading++;
            ResourceManager.PrepareResource<Material>(Resource.Material.MainPlayerMaterial, mat =>
            {
                _loading--;

                if (resId != UniqueResourceId)
                {
                    GameObject.Destroy(mat);
                    TryContinue();
                    return;
                }

                if (!newPartRender)
                {
                    GameObject.Destroy(mat);
                    TryContinue();
                    return;
                }

                var newMat = new Material(mat);

                Material oldMat = newPartRender.sharedMaterial;
                newMat.SetTexture(MainTexVariableName, oldMat.GetTexture(MainTexVariableName));
                newMat.SetTextureOffset(MainTexVariableName, oldMat.GetTextureOffset(MainTexVariableName));
                newMat.SetTextureScale(MainTexVariableName, oldMat.GetTextureScale(MainTexVariableName));
                newPartRender.material = newMat;

                if (record != null)
                {
                    newMat.SetColor("_BColor",
                        new Color(record.FlowRed/255.0f, record.FlowGreen/255.0f, record.FlowBlue/255.0f,
                            record.FlowAlpha/255.0f));
                    newMat.SetColor("_TexColor",
                        new Color(record.SepcularRed/255.0f, record.SepcularGreen/255.0f, record.SepcularBlue/255.0f,
                            record.SepcularAlpha / 255.0f));
                    newMat.SetColor("_Color",
                       new Color(record.MainRed / 255.0f, record.MainGreen / 255.0f, record.MainBlue / 255.0f,
                           1f));
                }
                else
                {
                    newMat.SetColor("_BColor", Color.black);
                    newMat.SetColor("_TexColor", Color.black);
                    newMat.SetColor("_Color", Color.white);
                }

                ResourceManager.ChangeShader(partTransform);

                TryContinue();
            }, true, true, sync);
        }
        else
        {
            if (!newPartRender.sharedMaterial)
                return true;

            var newMat = new Material(newPartRender.sharedMaterial);

            if (!newPartRender)
            {
                return true;
            }

            Material oldMat = newPartRender.sharedMaterial;
            newMat.SetTexture(MainTexVariableName, oldMat.GetTexture(MainTexVariableName));
            newMat.SetTextureOffset(MainTexVariableName, oldMat.GetTextureOffset(MainTexVariableName));
            newMat.SetTextureScale(MainTexVariableName, oldMat.GetTextureScale(MainTexVariableName));
            newPartRender.material = newMat;

            if (record != null)
            {
                newMat.SetColor("_BColor",
                    new Color(record.FlowRed/255.0f, record.FlowGreen/255.0f, record.FlowBlue/255.0f,
                        record.FlowAlpha/255.0f));
                newMat.SetColor("_TexColor",
                    new Color(record.SepcularRed/255.0f, record.SepcularGreen/255.0f, record.SepcularBlue/255.0f,
                        record.SepcularAlpha/255.0f));
                newMat.SetColor("_Color",
                    new Color(record.MainRed / 255.0f, record.MainGreen / 255.0f, record.MainBlue / 255.0f,
                        1f));
            }
            else
            {
                newMat.SetColor("_BColor", Color.black);
                newMat.SetColor("_TexColor", Color.black);
                newMat.SetColor("_Color", Color.white);
            }

            ResourceManager.ChangeShader(partTransform);
        }
        return false;
    }

    private IEnumerator UpdateCobmine()
    {
        yield return new WaitForSeconds(1);

        if (!GameSetting.Instance.CombineCharacterMesh)
            yield break;

        if(FashionMode)
            yield break;

        const string MainTexVariableName = "_MainTex";
        Texture tex = null;

        try
        {
#if UNITY_EDITOR
            bool canMerge = true;
            foreach (var info in _avatarInfo)
            {
                if(!info.Value.NeedCombine)
                    continue;

                var part = info.Value.avatarPart;
                SkinnedMeshRenderer renderer;
                if (part)
                {
                    renderer = part.renderer as SkinnedMeshRenderer;
                }
                else
                {
                    part = info.Value.defaultPart;
                    renderer = part.renderer as SkinnedMeshRenderer;
                }

                if (!renderer.sharedMesh.isReadable)
                {
                    canMerge = false;
                    Debug.LogError("模型文件是不可读写的，不能合并。", renderer.sharedMesh);
                }
            }

            if (!canMerge)
                yield break;
#endif

            if (!_combined)
            {
                var com = new GameObject("Combined");
                com.transform.parent = _body.transform;
                com.transform.localPosition = Vector3.zero;
                com.transform.localRotation = Quaternion.identity;
                com.transform.localScale = Vector3.one;
                _combined = com.AddComponent<SkinnedMeshRenderer>();
                _combined.sharedMesh = new Mesh();
                _combined.sharedMesh.name = "Combined";
                _combined.sharedMesh.MarkDynamic();
                _vertices = new Vertices();
            }

            _combined.gameObject.layer = Layer;

            if (_body == null)
            {
                yield break;
            }

            _mapping.Clear();
            foreach (var info in _avatarInfo)
            {
                if (!info.Value.NeedCombine)
                    continue;

                var part = info.Value.avatarPart;
                SkinnedMeshRenderer renderer;
                if (part)
                {
                    renderer = part.renderer as SkinnedMeshRenderer;
                }
                else
                {
                    part = info.Value.defaultPart;
                    renderer = part.renderer as SkinnedMeshRenderer;
                }

                var bones = renderer.bones;
                for (int i = 0; i < renderer.bones.Length; i++)
                {
                    _mapping[bones[i]] = new BoneInfo() {BindPos = renderer.sharedMesh.bindposes[i]};
                }
            }

            _bones = _mapping.Keys.ToArray();
            _bindpos = new Matrix4x4[_bones.Length];

            for (int i = 0; i < _bones.Length; i++)
            {
                var b = _mapping[_bones[i]];
                b.Index = i;
                _bindpos[i] = b.BindPos;
            }

            _vertices.Reset();
            foreach (var info in _avatarInfo)
            {
                if (!info.Value.NeedCombine)
                    continue;

                Mesh mesh;
                var part = info.Value.avatarPart;
                SkinnedMeshRenderer renderer;
                if (part)
                {
                    renderer = part.renderer as SkinnedMeshRenderer;
                    mesh = renderer.sharedMesh;
                }
                else
                {
                    part = info.Value.defaultPart;
                    renderer = part.renderer as SkinnedMeshRenderer;
                    mesh = renderer.sharedMesh;
                }

                int index = GetPartIndex(info.Key);
                _vertices.Add(mesh, renderer.bones, _mapping, index, info.Value.Offset);

                _vertices.Colors[index * 3] = info.Value.TexColor;
                _vertices.Colors[index * 3 + 1] = info.Value.BColor;
                _vertices.Colors[index * 3 + 2] = info.Value.Color;

                if (tex == null)
                    tex = renderer.sharedMaterial.GetTexture(MainTexVariableName);
            }
        }
        catch(Exception ex)
        {
            Debug.LogWarning("Combine mesh got exception: " + ex.ToString());
            yield break;
        }

        foreach (var info in _avatarInfo)
        {

            if (!info.Value.NeedCombine)
                continue;

            var part = info.Value.avatarPart;
            if (!part)
            {
                part = info.Value.defaultPart;
            }

            part.SetActive(false);
        }

        _combined.bones = _bones;
        _combined.sharedMesh.bindposes = _bindpos;
        _vertices.Update(_combined.sharedMesh);

        _combineDirty = false;

        var resId = UniqueResourceId;
        _loading++;
        ResourceManager.PrepareResource<Material>(
            ObjType == OBJ.TYPE.MYPLAYER ? Resource.Material.MainPlayerCombinedMaterial : Resource.Material.OtherPlayerCombinedMaterial,
            mat =>
            {
                _loading--;

                if (resId != UniqueResourceId)
                {
                    GameObject.Destroy(mat);
                    TryContinue();
                    return;
                }

                if (!_combined)
                {
                    GameObject.Destroy(mat);
                    TryContinue();
                    return;
                }

                var newMat = new Material(mat);

                newMat.SetTexture(MainTexVariableName, tex);
                _vertices.SetMaterialColor(newMat);
                _combined.material = newMat;

                ResourceManager.ChangeShader(_combined.transform);

                // 检查Body的Animation的CullType， 否则会出现当所有装备都卸下时，动画没有了的问题
                _body.animation.cullingType = AnimationCullingType.BasedOnUserBounds;
                _body.animation.localBounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1, 2, 1));

                TryContinue();
            }, true, true, true);
    }

    public void Destroy()
    {
        {
            // foreach(var avatarInfo in _avatarInfo)
            var __enumerator2 = (_avatarInfo).GetEnumerator();
            while (__enumerator2.MoveNext())
            {
                var avatarInfo = __enumerator2.Current;
                {
                    if (avatarInfo.Value.avatarPart != null)
                    {
                        Destroy(avatarInfo.Value.avatarPart);
                    }

                    if (avatarInfo.Value.defaultPart != null)
                    {
                        avatarInfo.Value.defaultPart.SetActive(true);
                    }
                }
            }
        }
        {
            // foreach(var part in _parts)
            var __enumerator3 = (_parts).GetEnumerator();
            while (__enumerator3.MoveNext())
            {
                var part = __enumerator3.Current;
                {
                    {
                        var __list5 = part.Value;
                        var __listCount5 = __list5.Count;
                        for (var __i5 = 0; __i5 < __listCount5; ++__i5)
                        {
                            var o = __list5[__i5];
                            {
                                ComplexObjectPool.Release(o);
                            }
                        }
                    }
                }
            }
        }


        if (_body)
        {
            OptList<Renderer>.List.Clear();
            _body.GetComponentsInChildren(true, OptList<Renderer>.List);
            {
                var __array1 = OptList<Renderer>.List;
                var __arrayLength1 = __array1.Count;
                for (var __i1 = 0; __i1 < __arrayLength1; ++__i1)
                {
                    var renderer = __array1[__i1];
                    {
                        renderer.enabled = true;
                    }
                }
            }
        }

        _parts.Clear();
        _avatarLoadQueue.Clear();
        _avatarInfo.Clear();
        InitAvatarInfo(null);

        if (_combined)
        {
            Destroy(_combined.sharedMesh);
            Destroy(_combined.sharedMaterial);
            Destroy(_combined.gameObject);
            Destroy(_combined);
            _vertices = null;
        }

        if(_combineCoroutine != null)
            StopCoroutine(_combineCoroutine);

        _loading = 0;
        _body = null;
        mWeaponModels.Clear();
        WingGameObject = null;
        MountGameObject = null;

        BodyModel = string.Empty;
        FootModel = string.Empty;
        ChestModel = string.Empty;
        HandModel = string.Empty;
        LegModel = string.Empty;
        HeadModel = string.Empty;

        UniqueResourceId = GetNextResourceId();
    }

    private static Transform FindChild(Transform t, string searchName)
    {
        {
            // foreach(var c in t)
            var __enumerator4 = (t).GetEnumerator();
            while (__enumerator4.MoveNext())
            {
                var c = (Transform) __enumerator4.Current;
                {
                    var partName = c.name;
                    if (partName == searchName)
                    {
                        return c;
                    }
                    var r = FindChild(c, searchName);
                    if (r != null)
                    {
                        return r;
                    }
                }
            }
        }
        return null;
    }

    // 递归遍历子物体
    private static Transform GetPart(Transform t, string searchName)
    {
        if (t.name == searchName)
        {
            return t;
        }
        else
        {
            // foreach(var c in t)
            var __enumerator1 = (t).GetEnumerator();
            while (__enumerator1.MoveNext())
            {
                var c = (Transform) __enumerator1.Current;
                {
                    var partName = c.name;

                    if (partName == searchName)
                    {
                        return c;
                    }
                    var r = GetPart(c, searchName);
                    if (r != null)
                    {
                        return r;
                    }
                }
            }
        }
        return null;
    }

    private string GetPartModel(string part)
    {
        switch (part)
        {
            case "Foot":
                return FootModel;
            case "Chest":
                return ChestModel;
            case "Hand":
                return HandModel;
            case "Head":
                return HeadModel;
            case "Leg":
                return LegModel;
            case "Fashion":
                return FashionModel;
        }

        return string.Empty;
    }

    private int GetPartIndex(string part)
    {
        switch (part)
        {
            case "Foot":
                return 0;
            case "Chest":
                return 1;
            case "Hand":
                return 2;
            case "Head":
                return 3;
            case "Leg":
                return 4;
            case "Fashion":
                return 5;
        }

        return 0;
    }

    // 创建模型
    public void LoadModel(string model, bool sync)
    {
        var resId = UniqueResourceId;
        _loading++;
        ComplexObjectPool.NewObject(Resource.Dir.Model + model, obj =>
        {
            _loading--;

            if (resId != UniqueResourceId)
            {
                ComplexObjectPool.Release(obj);
                TryContinue();
                return;
            }

            Body = obj;
            Body.transform.parent = gameObject.transform;

            InitAvatarInfo(transform);

            TryContinue();
        }, null, null, sync);
    }

    public void MountWeapon(MountPoint p,
                            string model,
                            WeaponMountRecord mountRecord,
                            EquipModelViewRecord record,
                            int part,
                            bool sync,
                            Action<GameObject> callback = null)
    {
        mWeaponModels[part] = model;

        // remove all the objs with the same tag.
        List<GameObject> l;
        if (!_parts.TryGetValue(part, out l))
        {
            l = new List<GameObject>();
            _parts.Add(part, l);
        }
        var lCount0 = l.Count;
        for (var i = 0; i < lCount0; i++)
        {
            ComplexObjectPool.Release(l[i], Layer == UnityEngine.LayerMask.NameToLayer("UI"));
        }
        l.Clear();

        if (string.IsNullOrEmpty(model))
        {
            TryContinue();
            return;
        }

        if (_body == null || _loading > 0)
        {
            _avatarLoadQueue.Enqueue(new KeyValuePair<string, WaitingResource>(p.ToString(),
                new WaitingResource
                {
                    ModelPath = model,
                    ModelViewRecord = record,
                    IsWeapon = true,
                    Part = part,
                    Callback = callback,
                    WeaponMountRecord = mountRecord
                }));
            return;
        }

        var resIdx = UniqueResourceId;
        _loading++;

        ComplexObjectPool.NewObject(model, obj =>
        {
            _loading--;

            if (resIdx != UniqueResourceId)
            {
                ComplexObjectPool.Release(obj, Layer == UnityEngine.LayerMask.NameToLayer("UI"));
                TryContinue();
                return;
            }

            string value;
            if (mWeaponModels.TryGetValue(part, out value) && value != model)
            {
                ComplexObjectPool.Release(obj, Layer == UnityEngine.LayerMask.NameToLayer("UI"));
                TryContinue();
                return;
            }

            if (null != obj)
            {
                if (!MountWeapon(p, obj, mountRecord, record, part, sync, callback))
                {
                    ComplexObjectPool.Release(obj);
                    TryContinue();
                    return;
                }
            }

            if (record == null)
            {
                TryContinue();
                return;
            }

            var recordEffectPathLength1 = record.EffectPath.Length;
            for (var i = 0; i < recordEffectPathLength1; i++)
            {
                if (!string.IsNullOrEmpty(record.EffectPath[i]))
                {
                    var resId = UniqueResourceId;
                    _loading++;
                    var index = i;
                    ComplexObjectPool.NewObject(record.EffectPath[index], effect =>
                    {
                        _loading--;

                        if (resId != UniqueResourceId)
                        {
                            ComplexObjectPool.Release(effect, Layer == UnityEngine.LayerMask.NameToLayer("UI"));
                            TryContinue();
                            return;
                        }

                        string _model;
                        if (mWeaponModels.TryGetValue(part, out _model) && _model != model)
                        {
                            ComplexObjectPool.Release(effect, Layer == UnityEngine.LayerMask.NameToLayer("UI"));
                            TryContinue();
                            return;
                        }

                        if (obj != null && effect != null)
                        {
                            if (record.EffectMount[index] == -1)
                            {
                                AddEffect(obj.transform, effect,
                                    new Vector3(record.EffectPosX[index], record.EffectPosY[index],
                                        record.EffectPosZ[index]),
                                    Quaternion.Euler(record.EffectDirX[index], record.EffectDirY[index],
                                        record.EffectDirZ[index]), part);
                            }
                            else
                            {
                                AddEffect(
                                    GetPart(_body.transform, ((MountPoint) record.EffectMount[index]).ToString()),
                                    effect,
                                    new Vector3(record.EffectPosX[index], record.EffectPosY[index],
                                        record.EffectPosZ[index]),
                                    Quaternion.Euler(record.EffectDirX[index], record.EffectDirY[index],
                                        record.EffectDirZ[index]), part);
                            }
                        }
                        TryContinue();
                    }, null, null, sync);
                }
            }

            TryContinue();
        }, null, null, sync, ObjType == OBJ.TYPE.MYPLAYER, true, null, ObjType == OBJ.TYPE.MYPLAYER);
    }

    private bool MountWeapon(MountPoint p,
                             GameObject obj,
                             WeaponMountRecord mountRecord,
                             EquipModelViewRecord record,
                             int part,
                             bool sync,
                             Action<GameObject> callback)
    {
        if (_body == null)
        {
            return false;
        }

        if (obj == null)
        {
            return false;
        }

        var parent = GetPart(_body.transform, p.ToString());

        if (parent == null)
        {
            return false;
        }

        var o = obj;
        var objTransform = o.transform;
        objTransform.parent = parent;
        objTransform.localPosition = new Vector3(mountRecord.PosX, mountRecord.PosY, mountRecord.PosZ);
        objTransform.localRotation = Quaternion.Euler(mountRecord.DirX, mountRecord.DirY, mountRecord.DirZ);
        objTransform.localScale = Vector3.one;
        CorrectLayer(o);
        o.SetRenderQueue(mRenderQueue);
        _parts[part].Add(o);

        if (callback != null)
        {
            callback(o);
        }

        Material material;
        Renderer renderer = o.renderer;
        if (renderer != null)
        {
            material = renderer.sharedMaterial;
        }
        else
        {
            renderer = objTransform.GetComponentInChildren<SkinnedMeshRenderer>();
            if (renderer == null)
            {
                Logger.Log2Bugly("MountWeapon renderer is null, {0}, {1}, {2}", p, obj.name, record.Id);
                return false;
            }
            material = renderer.sharedMaterial;
        }

        if (material == null)
        {
            return false;
        }

        // 翅膀没有这个
        if (!material.HasProperty("_BColor"))
        {
            return true;
        }

        const string MainTexVariableName = "_MainTex";

        if (ObjType == OBJ.TYPE.MYPLAYER)
        {
            var resId = UniqueResourceId;
            _loading++;
            ResourceManager.PrepareResource<Material>(Resource.Material.MainPlayerMaterial, mat =>
            {
                _loading--;

                if (resId != UniqueResourceId)
                {
                    GameObject.Destroy(mat);
                    TryContinue();
                    return;
                }

                if (!mat)
                {
                    TryContinue();
                    return;
                }

                if (!o || !renderer)
                {
                    TryContinue();
                    return;
                }

                if (!material)
                {
                    TryContinue();
                    return;
                }

                var newMat = new Material(mat);
                Material oldMat = material;
                newMat.SetTexture(MainTexVariableName, oldMat.GetTexture(MainTexVariableName));
                newMat.SetTextureOffset(MainTexVariableName, oldMat.GetTextureOffset(MainTexVariableName));
                newMat.SetTextureScale(MainTexVariableName, oldMat.GetTextureScale(MainTexVariableName));
                renderer.material = newMat;

                if (record != null)
                {
                    newMat.SetColor("_BColor",
                        new Color(record.FlowRed/255.0f, record.FlowGreen/255.0f, record.FlowBlue/255.0f,
                            record.FlowAlpha/255.0f));
                    newMat.SetColor("_TexColor",
                        new Color(record.SepcularRed/255.0f, record.SepcularGreen/255.0f, record.SepcularBlue/255.0f,
                            record.SepcularAlpha/255.0f));
                    newMat.SetColor("_Color",
                        new Color(record.MainRed / 255.0f, record.MainGreen / 255.0f, record.MainBlue / 255.0f,
                            1f));
                }
                else
                {
                    newMat.SetColor("_BColor", Color.black);
                    newMat.SetColor("_TexColor", Color.black);
                    newMat.SetColor("_Color", Color.white);
                }

                ResourceManager.ChangeShader(objTransform);

                TryContinue();
            }, true, true, sync);
        }
        else
        {
            if (renderer)
            {
                var newMat = new Material(material);
                newMat.SetTexture(MainTexVariableName, material.GetTexture(MainTexVariableName));
                renderer.material = newMat;

                if (record != null)
                {
                    newMat.SetColor("_BColor",
                        new Color(record.FlowRed/255.0f, record.FlowGreen/255.0f, record.FlowBlue/255.0f,
                            record.FlowAlpha/255.0f));
                    newMat.SetColor("_TexColor",
                        new Color(record.SepcularRed/255.0f, record.SepcularGreen/255.0f, record.SepcularBlue/255.0f,
                            record.SepcularAlpha/255.0f));
                    newMat.SetColor("_Color",
                       new Color(record.MainRed / 255.0f, record.MainGreen / 255.0f, record.MainBlue / 255.0f,
                           1f));
                }
                else
                {
                    newMat.SetColor("_BColor", Color.black);
                    newMat.SetColor("_TexColor", Color.black);
                    newMat.SetColor("_Color", Color.white);
                }

                ResourceManager.ChangeShader(objTransform);
            }
        }

        return true;
    }

    /// <summary>
    ///     移除当前部件，当前部件变为默认部件
    /// </summary>
    /// <param name="part"></param>
    public void RemovePart(string part)
    {
        AvatarInfo currentInfo;
        if (_avatarInfo.TryGetValue(part, out currentInfo))
        {
            if (currentInfo.avatarPart != null)
            {
                ComplexObjectPool.Release(currentInfo.avatarPart);
                currentInfo.avatarPart = null;
            }

            if (currentInfo.defaultPart != null)
            {
                currentInfo.defaultPart.SetActive(true);
            }
        }
    }

    // 刷新骨骼数据   将root物体的bodyPart骨骼更新为avatarPart
    private static void SetBones(GameObject goBodyPart, GameObject goAvatarPart, GameObject root)
    {
        var bodyRender = goBodyPart.GetComponent<SkinnedMeshRenderer>();
        var avatarRender = goAvatarPart.GetComponent<SkinnedMeshRenderer>();
        var myBones = new Transform[avatarRender.bones.Length];
        var avatarRenderbonesLength2 = avatarRender.bones.Length;
        for (var i = 0; i < avatarRenderbonesLength2; i++)
        {
            myBones[i] = FindChild(root.transform, avatarRender.bones[i].name);
        }
        bodyRender.bones = myBones;
    }

    private void SetPartModel(string part, string model, EquipModelViewRecord record)
    {
        switch (part)
        {
            case "Foot":
                FootModel = model;
                FootModelViewRecord = record;
                break;
            case "Chest":
                ChestModel = model;
                ChestModelViewRecord = record;
                break;
            case "Hand":
                HandModel = model;
                HandModelViewRecord = record;
                break;
            case "Head":
                HeadModel = model;
                HeadModelViewRecord = record;
                break;
            case "Leg":
                LegModel = model;
                LegModelViewRecord = record;
                break;
            case "Fashion":
                FashionModel = model;
                LegModelViewRecord = record;
                break;
        }
    }

    private void Start()
    {
#if !UNITY_EDITOR
        try
        {
#endif

      InitAvatarInfo(null);
        

        //         LoadModel(BodyModel);
        // 
        //         ChangePart("Foot", FootModel);
        //         ChangePart("Chest", ChestModel);
        //         ChangePart("Hand", HandModel);
        //         ChangePart("Leg", LegModel);
        //         ChangePart("Head", HeadModel);
        //         ChangePart("Fashion", FashionModel);
        // 
        //         MountWeapon(MountPoint.RightWeapen, RightWeapon); 
        //         MountWeapon(MountPoint.LeftWeapen, LeftWeapon);
        // 
        //         MountWeapon(MountPoint.Center, Wing);

#if !UNITY_EDITOR
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
#endif
    }

    private void TryContinue()
    {
        if (_loading == 0)
        {
            // 换装请求
            if (_avatarLoadQueue.Count > 0)
            {
                var avatar = _avatarLoadQueue.Dequeue();
                if (avatar.Value.IsWeapon)
                {
                    string value;
                    if (mWeaponModels.TryGetValue(avatar.Value.Part, out value) && value == avatar.Value.ModelPath)
                    {
                        MountWeapon((MountPoint) Enum.Parse(typeof (MountPoint), avatar.Key), avatar.Value.ModelPath,
                            avatar.Value.WeaponMountRecord,
                            avatar.Value.ModelViewRecord, avatar.Value.Part, avatar.Value.Sync, avatar.Value.Callback);
                    }
                    else
                    {
                        TryContinue();
                    }
                }
                else
                {
                    if (GetPartModel(avatar.Key) == avatar.Value.ModelPath || string.IsNullOrEmpty(avatar.Value.ModelPath))
                    {
                        ChangePart(avatar.Key, avatar.Value.ModelPath, avatar.Value.ModelViewRecord, avatar.Value.Sync, avatar.Value.ChangingFashion);
                    }
                    else
                    {
                        TryContinue();
                    }
                }
            }
        }
    }

    // 换装的部件信息
    protected class AvatarInfo
    {
        public GameObject avatarPart;
        public GameObject defaultPart;
        public string partName;
        public Color BColor = Color.black;
        public Color Color = Color.white;
        public Color TexColor = Color.black;
        public Vector4 Offset;
        public bool NeedCombine = true;
    }

    private class WaitingResource
    {
        public Action<GameObject> Callback;
        public bool IsWeapon;
        public string ModelPath;
        public EquipModelViewRecord ModelViewRecord;
        public int Part;
        public readonly bool Sync = false;
        public WeaponMountRecord WeaponMountRecord;
        public bool ChangingFashion = false;
    }
}