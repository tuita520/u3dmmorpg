using System;
using UnityEngine;
using System.Collections;

public class LineSprite : UISprite
{
    private Vector3 mPostionBegin;

    public Vector3 PostionBegin
    {
        get
        {
            return mPostionBegin; 
        }
        set
        {
            mPostionBegin = value;
            RefresPostion();
        }
    }

    private Vector3 mPostionEnd;

    public Vector3 PostionEnd
    {
        get
        {
            return mPostionEnd; 
        }
        set
        {
            mPostionEnd = value;
            RefresPostion();
        }
    }

    private void RefresPostion()
    {
        this.transform.localPosition = (PostionBegin + PostionEnd)/2;
        if (Math.Abs(PostionEnd.y - PostionBegin.y) > 0.1f)
        {
            var t = Mathf.Atan2(PostionEnd.y - PostionBegin.y, PostionEnd.x - PostionBegin.x);
            this.transform.localRotation = Quaternion.Euler(0, 0, t * Mathf.Rad2Deg);    
        }
        
        width = (int)Vector3.Distance(PostionBegin, PostionEnd);
    }
}
