using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using BehaviourMachine;

public class WingPartTrainningHelper : MonoBehaviour
{
    // Use this for initialization
    private List<Transform> balls;
    private List<Transform> lines;

    private void Awake()
    {
#if !UNITY_EDITOR
try
{
#endif

        balls = new List<Transform>();
        lines = new List<Transform>();
        var c = gameObject.transform.childCount + 1;
        for (var i = 1; i < c; i++)
        {
            var ball = transform.FindChild("ball" + i);
            balls.Add(ball);
            if (ball != null)
            {
                var line = ball.FindChild("line" + i);
                lines.Add(line);
                if (line != null)
                {
                    var clone = NGUITools.AddChild(line.parent.gameObject, line.gameObject);
                    if (clone != null)
                    {
                        var trans = clone.transform;
                        trans.localPosition = line.localPosition;
                        trans.localRotation = line.localRotation;
                        trans.localScale = line.localScale;
                        var spr = clone.GetComponent<UISprite>();
                        SetGery(spr.transform, true);
                        spr.depth = spr.depth - 1;
                        spr.height = spr.height + 10;
                    }

                    var spr2 = line.GetComponent<UISprite>();
                    if (null != spr2)
                    {
                        spr2.type = UIBasicSprite.Type.Filled;
                        spr2.fillAmount = 0;
                        spr2.fillDirection = UIBasicSprite.FillDirection.Horizontal;
                    }
                }
            }
            else
            {
                lines.Add(null);
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

    public void DoChangePartAnimation()
    {

        var tween  = gameObject.GetComponent<iTween>();
        if (null != tween)
        {
            DestroyImmediate(tween);
        }
        transform.localScale = Vector3.one;
        iTween.ScaleFrom(gameObject, new Vector3(0.1f, 0.1f, 0.1f), 1f);
    }

    public void RefreshTrainningPart(int index, bool active, bool doAnimation)
    {
        if (lines == null)
        {
            return;
        }
        if (index >= lines.Count) return;

        var ball = balls[index];
        if (null != ball)
        {
            SetGery(ball, !active);
        }

        if (index == 0) return;

        var line = lines[index - 1];
        if (null == line) return;

        if (line.gameObject.activeSelf == false)
        {
            line.gameObject.SetActive(true);
        }

        var sp = line.transform.GetComponent<UISprite>();
        if (sp != null)
        {
            sp.fillAmount = 1;
        }

        SetGery(line, !active);
        if (doAnimation)
        {
            DoAnimation(line, ball);
        }
    }

    private static void SetGery(Transform lineTrans, bool isGery)
    {
        var sp = lineTrans.GetComponent<UISprite>();
        if (sp != null)
        {
            var atlasName = sp.atlas.name;
            if (isGery)
            {
                if (atlasName.Contains("Grey"))
                {
                    return;
                }
                ResourceManager.PrepareResource<GameObject>("UI/Atlas/" + atlasName + "Grey.prefab", res =>
                {
                    if (lineTrans == null)
                    {
                        return;
                    }

                    sp.atlas = res.GetComponent<UIAtlas>();
                }, true, true, true, true, true);
            }
            else
            {
                if (!atlasName.Contains("Grey"))
                {
                    return;
                }

                atlasName = atlasName.Remove(atlasName.Length - 4, 4);
                ResourceManager.PrepareResource<GameObject>("UI/Atlas/" + atlasName + ".prefab", res =>
                {
                    if (lineTrans == null)
                    {
                        return;
                    }

                    sp.atlas = res.GetComponent<UIAtlas>();
                },true, true, true,true,true);
            }
        }
    }

    private void DoAnimation(Transform obj, Transform ball)
    {
        var sprite = obj.GetComponent<UISprite>();
        if (sprite != null)
        {
            StartCoroutine(AnimationCoroutine(sprite, ball));
        }

    }

    private void DoBallEffect(Transform trans)
    {
        var child = trans.FindChild("UI_TuJianHeCheng(Clone)");
        NGUITools.Destroy(child);

        ResourceManager.PrepareResource<GameObject>("Effect/UI/UI_TuJianHeCheng.prefab", res =>
        {
            var go = NGUITools.AddChild(trans.gameObject, res);
            var particle = go.GetComponent<ParticleScaler>();
            particle.IsUi = true;
            StartCoroutine(DestroyEffect(1f, () =>
            {
                var effect = trans.FindChild("UI_TuJianHeCheng(Clone)");
                NGUITools.Destroy(effect);
            }));
        }, true, true, true, true, true);
    }

    private IEnumerator DestroyEffect(float delay , Action action)
    {
        yield return new WaitForSeconds(delay);
        action();
    }

    private IEnumerator AnimationCoroutine(UISprite sp, Transform ball)
    {
        float amount = 0;
        if (null != ball)
        {
            SetGery(ball, true);
        }
        while (true)
        {
            yield return new WaitForEndOfFrame();
            amount += 0.06f;
            sp.fillAmount = amount;
            if (amount >= 1.0f)
            {
                sp.fillAmount = 1.0f;
                if (null != ball)
                {
                    SetGery(ball, false);
                    DoBallEffect(ball);
                }
                yield break;
            }
        }
    }
}
