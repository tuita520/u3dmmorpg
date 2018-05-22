using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


[System.Serializable]
public class PlayableGameObj
{
    public GameObject ParticleObj;
    public AnimationClip ClipObj;
}


public class GameAnimationTest : MonoBehaviour
{
    public float Delay = 0;
    public float LoopTime = 5.0f;

    public List<PlayableGameObj> FirstPlayList = new List<PlayableGameObj>();
    public List<PlayableGameObj> LoopList = new List<PlayableGameObj>();

    //private Animation[] AnimArr;
    private bool IsInitOver;
    Coroutine tine = null;

    private void OnEnable()
    {
#if !UNITY_EDITOR
try
{
#endif
       
        if (!IsInitOver)
        {                        
            PlayFirst();            
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

        IsInitOver = false;
        if (tine != null)
        {
            StopCoroutine(tine);
            tine = null;
        }

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

        IsInitOver = false;
        if (tine != null)
        {
            StopCoroutine(tine);
            tine = null;
        }
    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}


    [ContextMenu("Play")]
    public void Play()
    {
        PlayFirst();    
    }
    
    public void PlayFirst()
    {
       
        for (int i = 0; i < FirstPlayList.Count; i++)
        {
            if (FirstPlayList[i].ParticleObj == null) continue;
            GameUtils.ResetEffect(FirstPlayList[i].ParticleObj);

            if (FirstPlayList[i].ClipObj == null) continue;
            //if (AnimArr[i] == null)
            //{
                if (FirstPlayList[i].ParticleObj.GetComponent<Animation>())
                {
                    Animation anim= FirstPlayList[i].ParticleObj.GetComponent<Animation>();
                    anim.Play(FirstPlayList[i].ClipObj.name);
                }
                //else
                //{
                //    AnimArr[i] = FirstPlayList[i].ParticleObj.AddComponent<Animation>();
                //}
            //}
            //if (!AnimArr[i].GetClip(LoopList[i].ClipObj.name))
            //    AnimArr[i].AddClip(FirstPlayList[i].ClipObj, FirstPlayList[i].ClipObj.name);
            
            //gameObject.SampleAnimation(FirstPlayList[i].ClipObj, 0);
            //AnimArr[i].Play(FirstPlayList[i].ClipObj.name);         
        }
        IsInitOver = true;
        if (tine != null)
        {
            StopCoroutine(tine);
        }
        tine = StartCoroutine(IEtorPlayLoop());
    }

    private IEnumerator IEtorPlayLoop()
    {
        yield return new WaitForSeconds(Delay);

        for (int i = 0; i < LoopList.Count; i++)
        {
            if (LoopList[i].ParticleObj == null) continue;
            //GameUtils.ResetEffect(LoopList[i].ParticleObj);

            if (LoopList[i].ClipObj == null) continue;
            //if (AnimArr[i] == null)
            //{
                if (LoopList[i].ParticleObj.GetComponent<Animation>())
                {
                    Animation anim = LoopList[i].ParticleObj.GetComponent<Animation>();
                    LoopList[i].ClipObj.wrapMode = WrapMode.Loop;
                    anim.CrossFade(LoopList[i].ClipObj.name);                    
                }
            //    else
            //    {
            //        AnimArr[i] = LoopList[i].ParticleObj.AddComponent<Animation>();
            //    }
            //}
            //if (!AnimArr[i].GetClip(LoopList[i].ClipObj.name))
            //{
                //AnimArr[i].AddClip(LoopList[i].ClipObj, LoopList[i].ClipObj.name);
            //}                        
            //AnimArr[i].animation.CrossFade(LoopList[i].ClipObj.name);
            //LoopList[i].ClipObj.wrapMode = WrapMode.Loop;
        }
        
    }

    

}





