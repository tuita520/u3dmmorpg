using UnityEngine;
using EventSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using ClientDataModel;
using ScriptManager;

namespace GameUI
{
    public class SkillBallDrawMoveSpinning : MonoBehaviour
    {

        public static SkillBallDrawMoveSpinning m_Instance;
        public Transform Target;
        public float speed = 1f;
        public float autoRoteSpeed = 10f;
        private readonly List<Transform> boxTransList = new List<Transform>();
        private readonly List<SkillOutBox> skillBoxList = new List<SkillOutBox>();
        private readonly List<Transform> boxUITransList = new List<Transform>();
        private Vector3 centerVec3;
        private Coroutine rotateCoroutine;
        private int indexNum = 0;
        public float Radius = 0.4f;
        public float angleTween = 18f;//两个相邻的技能Box之间的角度
        public float startAngle = 0; //开始的角度
        public float fullangle = 162f; //角的总的度数
        private bool isShow = false;
        private bool isSelectBall = false;
        public SkillDataModel SkillDataModel
        {
            get { return PlayerDataManager.Instance.PlayerDataModel.SkillData; }
        }

        void Start()
        {
#if !UNITY_EDITOR
try
{
#endif

            m_Instance = this;
            Initialize();           
        
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

        private float angleMix;
        private float angleMax;
        private bool isDrag = false;

        public void OnDrag(Vector2 delta)
        {
            if (Target == null) return;
           
            isShow = true;
            var z = Target.transform.localEulerAngles.z;
            if (z > 180)
            {
                z = z - 360;
            }
            var dis = Time.fixedDeltaTime * -delta.y * 2.5f;
            var zwant = z + dis;
            if (zwant > 34)
            {
                zwant = 34;
            }

            if (zwant < -102)
            {
                zwant = -102;
            }

            Target.localRotation = Quaternion.Euler(0,0,zwant);
            SetFrontScale();
        }

        public void OnPress(bool press)
        {
            if (!press)
            {
                MoveNearest();
            }           
        }

        public void Initialize()
        {
            Target.localPosition = new Vector3(143.85f, 0, 0);
            Target.localRotation = Quaternion.Euler(0, 0, 33.99886f);

            var skillboxs = Target.gameObject.GetComponentsInChildren<SkillOutBox>(true);
            var count = skillboxs.Length;
            float singleDergree = fullangle / count;
            boxTransList.Clear();
            skillBoxList.Clear();
            for (int i = 0; i < count; i++)
            {
                var skillbox = skillboxs[i];
                var trans = skillbox.gameObject.transform;
                var angle = i * Mathf.Deg2Rad * singleDergree + startAngle;
              
                trans.localPosition = new Vector3(Radius * Mathf.Cos(angle), Radius * Mathf.Sin(angle), 0);
                var skillbtn = trans.FindChild("SkillBall");
                skillbtn = skillbtn.FindChild("SkillBallBtn");
                var trigger = skillbtn.gameObject.AddComponent<UIEventTrigger>();
                trigger.onClick.Add(new EventDelegate(() => { MoveSkillToCenter(skillbox.gameObject); }));

                boxTransList.Add(trans);
                skillBoxList.Add(skillbox);
                boxUITransList.Add(skillbtn);
            }
        
            //点的位置
            var fristBoxPos = boxTransList[0].position;
            centerVec3 = new Vector3(fristBoxPos.x, fristBoxPos.y, fristBoxPos.z);
            SetFrontScale();
            boxTransList[0].gameObject.SetActive(true);
            boxTransList[1].gameObject.SetActive(true);
            boxTransList[boxTransList.Count - 1].gameObject.SetActive(false);
        }

        public void SetFrontScale()
        {
            var count = boxTransList.Count;
            for (int i = 0; i < count; i++)
            {
                var child = boxUITransList[i];
                child.rotation = Quaternion.Euler(Vector3.zero);
                //缩放逻辑待添加
                var dis = (boxTransList[i].transform.position - centerVec3).magnitude;
                if (dis < 0.05f)
                {
                    if (i - 2 >= 0)
                    {
                        if (boxUITransList[i - 2].transform != null)
                            boxUITransList[i - 2].transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                    }
                    if (i - 1 >= 0)
                    {
                        if (boxUITransList[i - 1].transform != null)
                            boxUITransList[i - 1].transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                    }                   
                    if (boxUITransList[i].transform != null)
                        boxUITransList[i].transform.localScale = Vector3.one;
                    if (i + 1 <= 8)
                    {
                        if (boxUITransList[i + 1].transform != null)
                            boxUITransList[i + 1].transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                    }
                    if (i + 2 <= 8)
                    {
                        if (boxUITransList[i + 2].transform != null)
                            boxUITransList[i + 2].transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

                    }
                   
                    if (!isShow)
                    {
                        var e = new UIEvent_SkillFrame_OnSkillTalentSelected(SkillDataModel.SkillBoxes[i].SkillId);
                        EventDispatcher.Instance.DispatchEvent(e);
                    }
                }
                //boxUITransList[i].transform.localScale = Vector3.Lerp(boxUITransList[i].transform.localScale, boxUITransList[i + 1].transform.localScale, 0.5f * Time.deltaTime);
            }
        }


        private void MoveNearest()
        {
            var count = boxTransList.Count;
            var minIndex = 0;
            float dis = 0;
            for (int i = 0; i < count; i++)
            {
                var trans = boxTransList[i];
                if (i == 0)
                {
                    dis = (trans.position - centerVec3).magnitude;
                }
                else
                {
                    var d = (trans.position - centerVec3).magnitude;
                    if (dis > d)
                    {
                        dis = d;
                        minIndex = i;
                    }
                }
            }
  
            indexNum = minIndex;
            MoveSkillToCenter(boxTransList[minIndex].gameObject);
        }
        public void MoveSkillToCenter(GameObject obj)
        {
            if (null == obj) return;

            if (rotateCoroutine != null)
            {
                StopCoroutine(rotateCoroutine);
            }
            isShow = true;
            rotateCoroutine = StartCoroutine(MoveToCenterCoroutine(obj));
        }
        
        private void FixedUpdate()
        {
#if !UNITY_EDITOR
try
{
#endif

            if (isShow)
            {
                var angle = Target.localRotation.eulerAngles;
                if (angle.z >= 235f && angle.z < 290f)
                {

                    boxTransList[0].gameObject.SetActive(false);
                    boxTransList[1].gameObject.SetActive(false);
                }
                else if (angle.z >= 310f)
                {
                    boxTransList[0].gameObject.SetActive(true);
                    boxTransList[1].gameObject.SetActive(true);
                }


                if (angle.z >= 20f && angle.z <= 60f)
                {
                    boxTransList[boxTransList.Count - 1].gameObject.SetActive(false);
                }
                else if (angle.z <= 300f && angle.z > 60f)
                {
                    boxTransList[boxTransList.Count - 1].gameObject.SetActive(true);
                }   
            }
        
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

        private IEnumerator MoveToCenterCoroutine(GameObject obj)
        {
            var direction = 1;
            if (obj.transform.position.y < centerVec3.y)
            {
                direction = -1;
            }            
            while (true)
            {
                yield return new WaitForFixedUpdate();
                var angles = Target.localRotation.eulerAngles;
                var dis = (obj.transform.position - centerVec3).magnitude;
                if (dis < 0.05f)
                {
                    isShow = false;
                    isSelectBall = true;
                    //obj.transform.position = centerVec3;                   
                    // Vector3 vec = obj.transform.parent.InverseTransformPoint(centerVec3);
                    var angle = angle_360(obj.transform.position, centerVec3);
                    Target.Rotate(Vector3.forward, angle);
                    SetFrontScale();
                    yield break;
                }
                var fspeed = autoRoteSpeed * (1.8f * dis + 0.8f) * direction;
                angles.z += 0.1f * fspeed;
                Target.localRotation = Quaternion.Euler(angles);
                SetFrontScale();
            }
            yield break;
        }

        float angle_360(Vector3 from_, Vector3 to_)
        {
            Vector3 v3 = Vector3.Cross(from_, to_);
            if (v3.z > 0)
                return Vector3.Angle(from_, to_);
            else
                return 360 - Vector3.Angle(from_, to_);
        }
    }
}