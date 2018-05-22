#region using

using System;
using ClientDataModel;
using UnityEngine;
using System.Collections;

#endregion

namespace GameUI
{
	public class BookIconFrame : MonoBehaviour
	{
	    public BindDataRoot BindRoot;
        public Vector3 RotateTo = new Vector3(-9, -21, -25);
        private BookInfoDataModel dataModel = new BookInfoDataModel();
        private UIWidget widget;
	    private Bezier bezier;
	    private float bezierTime = -1.0f;
	    private float bezierTotalTime = 1.0f;
	    private Transform objTransform;


        private void Awake()
        {
#if !UNITY_EDITOR
            try
            {
#endif

            widget = gameObject.GetComponent<UIWidget>();

#if !UNITY_EDITOR
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
#endif
        }
	
	    [TableBinding("Icon")]
	    public int IconId
	    {
            get { return dataModel.IconId; }
	        set
	        {
                dataModel.IconId = value;
                
	            if (BindRoot != null)
	            {
                    BindRoot.SetBindDataSource(dataModel);
	            }
	        }
	    }

        private void Update()
        {
#if !UNITY_EDITOR
	        try
	        {
#endif
            if (bezierTime < 0)
                return;

            bezierTime += Time.deltaTime;
            if (bezierTime <= bezierTotalTime)
            {
                objTransform.localPosition = bezier.BezierCurve(bezierTime / bezierTotalTime);
            }
            else
            {
                objTransform.localPosition = bezier.BezierCurve(1.0f);
                bezierTime = -1.0f;
            }

#if !UNITY_EDITOR
	        }
	        catch (Exception ex)
	        {
	            Logger.Error(ex.ToString());
	        }
#endif
        }

	    private void StarBezierMove(Vector3 from, Vector3 to, float seconds)
	    {
	        bezierTotalTime = seconds;
            var p0 = from;
            var p2 = to;

            var p1 = new Vector3();
            p1.x = p0.x + 0.5f * (p2.x - p0.x);
            p1.y = p0.y + 1.5f * (p2.y - p0.y);
            bezier = new Bezier(p0, p1, p2);
            bezierTime = 0.0f;
	    }

        public void Fly(Transform toObj, FlyIconInfo info, Action<Vector3> call = null)
	    {
            objTransform = transform;
            objTransform.localPosition = info.From;
            objTransform.localScale = Vector3.one;
            widget.alpha = info.FromAlpha;

            var scaleTo = Vector3.one;
            if (info.ToWidth > 0 && widget.width > 0)
                scaleTo.x = (float)info.ToWidth / widget.width;

            if (info.ToHeight > 0 && widget.height > 0)
                scaleTo.y = (float)info.ToHeight / widget.height;

            if (info.Depth > 0)
                widget.depth = info.Depth;

            var root = widget.root;
            Func<Vector3> targetFunc = () =>
            {
                return root.transform.InverseTransformPoint(toObj.position);
            };

            Logger.Debug("Start Fly ...!" );
            root.StartCoroutine(FlyObject(targetFunc(), scaleTo, info, targetFunc, call));
	    }

        private IEnumerator FlyObject(Vector3 to, Vector3 toScale, FlyIconInfo info, Func<Vector3> toFunc = null, Action<Vector3> call = null)
        {
            //delay
            yield return new WaitForSeconds(info.Delay);

            //move
            float elapse = 0;
            var toPos = (toFunc != null) ? toFunc() : to;
            Logger.Debug("fly to Pos =>" + toPos.x + "，" + toPos.y + "," + toPos.z);
            if (info.UseBezier)
            {
                StarBezierMove(info.From, toPos, info.Time);
            }
            var startRot = objTransform.localRotation;
            while (elapse < info.Time)
            {
                elapse += Time.deltaTime;
                var t = elapse/info.Time;
                if (!info.UseBezier)
                {
                    objTransform.localPosition = Vector3.Lerp(info.From, toPos, t);
                }
                objTransform.localScale = Vector3.Lerp(Vector3.one, toScale, t);
                if (info.UseRotate)
                {
                    var rot = Quaternion.Lerp(startRot, Quaternion.Euler(RotateTo), t);
                    objTransform.localRotation = rot;                    
                }
                widget.alpha = Mathf.Lerp(1.0f, info.ToAlpha, t);
                yield return null;
            }

            //stay
            yield return new WaitForSeconds(info.Stay);

            //call
            if (null != call)
            {
                call(toPos);
            }
        }
	}
}
