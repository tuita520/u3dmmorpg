using ScriptManager;
using System;
#region using

using System.Collections;
using System.Collections.Generic;
using DataTable;
using PathologicalGames;
using UnityEngine;

#endregion

namespace GameUI
{
    public class OverheadFrame : MonoBehaviour
    {
        public UILabel Label;
        public StackLayout LayoutTitle;
        public WorldTo2DCameraConstraint mConstraint;
        private float mOffset;
        private bool mRefreshTitle;
        public UILabel PopTalkLabel;
        public List<OverheadTitleFrame> TitleList;
        public UILabel DescriptionLabel;
        private GameObject objTitle = null;
        private GameObject objHpBar = null;
        public GameObject chickenKing;
        public UILabel LvLabel;
        public UISliderNormal expSlider;
        void Awake()
        {
            var kingTra = transform.FindChild("chickenKing");
            if (kingTra != null)
            {
                chickenKing = kingTra.gameObject;
                chickenKing.SetActive(false);
            }

            var lv = transform.FindChild("LvLabel");
            if (lv != null)
            {
                LvLabel = lv.GetComponent<UILabel>();
                LvLabel.gameObject.SetActive(false);
            }

            var expSli = transform.FindChild("expBar");
            if (expSli != null)
            {
                expSlider = expSli.GetComponent<UISliderNormal>();
                expSlider.value = 0;
                expSlider.gameObject.SetActive(false);
            }

        }
        private IEnumerator AutoHide(float time)
        {
            yield return new WaitForSeconds(time);
            PopTalkLabel.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
#if !UNITY_EDITOR
	try
	{
#endif

            if (mRefreshTitle)
            {
                if (LayoutTitle != null)
                {
                    LayoutTitle.ResetLayout();
                }
                mRefreshTitle = false;
            }

#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
#endif
        }

        private void OnDisable()
        {
#if !UNITY_EDITOR
	try
	{
#endif

            StopAllCoroutines();
            if (null != PopTalkLabel && null != PopTalkLabel.gameObject)
            {
                PopTalkLabel.gameObject.SetActive(false);
            }

#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
#endif
        }
        private void OnEnable()
        {
#if !UNITY_EDITOR
	try
	{
#endif
            PopTalkLabel.gameObject.SetActive(false);
            mRefreshTitle = true;



#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
#endif
        }

        public virtual void UpdateHpBar(int curHp, int maxHp)
        {
            if (objHpBar == null || objHpBar.activeSelf == false)
                return;
            UISliderNormal slider = objHpBar.GetComponent<UISliderNormal>();
            if (slider != null)
            {
                slider.value = (float)curHp / (float)maxHp;
            }

        }
        public virtual void UpdatePlayerChickenExp(int level, int exp)
        {
            if (expSlider != null)
                expSlider.gameObject.SetActive(true);
            if (LvLabel != null && LvLabel.gameObject != null)
            {
                LvLabel.gameObject.SetActive(true);
                LvLabel.text = level.ToString();
            }

            var tb = Table.GetCheckenLv(level);
            if (expSlider != null && tb != null)
            {
                expSlider.value = (float)exp / tb.NeedExp;
            }
        }
        public virtual void ShowChickenKing(bool isShow)
        {
            if (chickenKing != null)
                chickenKing.SetActive(isShow);
        }
        public virtual void Reset(bool isMySelf = false)
        {
            foreach (var nameBoardTitleLogic in TitleList)
            {
                nameBoardTitleLogic.gameObject.SetActive(false);
            }
            objTitle = transform.FindChild("layOut").gameObject;
            objHpBar = transform.FindChild("HpBar").gameObject;
            if (objTitle != null && objHpBar != null)
            {

                if (SceneManager.Instance.IsShowHpBar() == true)
                {
                    objTitle.SetActive(false);
                    objHpBar.SetActive(true);
                    if (isMySelf && expSlider != null && LvLabel!=null)
                    {
                         expSlider.gameObject.SetActive(true);
                         LvLabel.gameObject.SetActive(true);
                    }
                       

                }
                else
                {
                    objTitle.SetActive(true);
                    objHpBar.SetActive(false);
                    //   expSlider.gameObject.SetActive(false);

                }
            }
        }

        public void ResetOffset()
        {
            mConstraint.offset = new Vector3(0, mOffset, 0);
        }

        public void RestLayoutTitle()
        {
            mRefreshTitle = true;
        }

        public void SetBattleColor(int camp)
        {
            TitleList[0].SetBattleColor(camp);
        }

        public void SetFlyOffset()
        {
            mConstraint.offset = new Vector3(0, mOffset - 0.2f, 0);
        }



        public void SetOwner(GameObject owner, GameObject root, float offset)
        {
            var objTransform = gameObject.transform;
            //objTransform.parent = root.transform;
            objTransform.SetParentEX(root.transform);
            objTransform.localScale = new Vector3(1, 1, 1);

            mOffset = offset;

            mConstraint.target = owner.transform;
            mConstraint.offset = new Vector3(0, offset, 0);
            mConstraint.orthoCamera = UIManager.Instance.UICamera;
            mConstraint.targetCamera = GameLogic.Instance.MainCamera;
        }

        public void SetText(string str)
        {
            Label.text = str;
        }
        public void SetText(string str, Color col)
        {
            var colStr = GameUtils.ColorToString(col);
            str = string.Format("[{0}]{1}[-]", colStr, str);
            SetText(str);
        }
        public void SetDescriptionText(string str, Color col)
        {
            var colStr = GameUtils.ColorToString(col);
            str = string.Format("[{0}]{1}[-]", colStr, str);
            SetDescriptionLabel(str);
        }
        private void SetDescriptionLabel(string str)
        {
            if (null != DescriptionLabel)
            {
                DescriptionLabel.text = str;
            }
        }

        public void SetTitle(int pos, int titleId, string TitleName, bool isMySelf)
        {
            var go = TitleList[pos].gameObject;
            if (titleId == -1)
            {
                go.SetActive(false);
            }
            else
            {
                if (null != PlayerDataManager.Instance && null != PlayerDataManager.Instance.BattleUnionDataModel
                    && null != PlayerDataManager.Instance.BattleUnionDataModel.MyUnion)
                {
                    if (titleId >= 2000 && titleId <= 2003)
                    {
                        if (PlayerDataManager.Instance.mUnionMembers.Count == 1)
                        {
                            titleId = 2000;
                        }
                    }
                }
                var tbNameTitle = Table.GetNameTitle(titleId);
                if (tbNameTitle != null)
                {
                    var active = true;
                    TitleList[pos].SetTitle(tbNameTitle, TitleName, ref active);
                    if (go.activeSelf != active)
                    {
                        go.SetActive(active);
                    }
                }
            }

            if (!GameSetting.Instance.ShowOtherPlayer && !isMySelf)
            {
                go.SetActive(false);
            }
	        if (!GameSetting.Instance.ShowOtherPlayerNameTitle && !isMySelf)
	        {
	            go.SetActive(false);
	        }
	    }

        public void ShowHideOtherTitle(bool isSHow, int pos, int titleId, string TitleName)
        {
            if (isSHow)
            {
                SetTitle(pos, titleId, TitleName, false);
            }
            else
            {
                if (pos < TitleList.Count)
                {
                    var go = TitleList[pos].gameObject;
                    go.SetActive(false);
                }
            }
        }

        // Use this for initialization
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

        public void Talk(string str, float time = 4)
        {
            PopTalkLabel.gameObject.SetActive(true);
            PopTalkLabel.text = str;
            StopAllCoroutines();
            StartCoroutine(AutoHide(time));
            RestLayoutTitle();
        }
    }
}