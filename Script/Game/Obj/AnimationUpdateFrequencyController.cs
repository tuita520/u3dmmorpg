using System;
using System.Collections.Generic;
using UnityEngine;

public class AnimationUpdateFrequencyController : UnityEngine.MonoBehaviour
{
    private static List<AnimationUpdateFrequencyController> animations = new List<AnimationUpdateFrequencyController>(128);
    private static List<AnimationUpdateFrequencyController> animationsTemp = new List<AnimationUpdateFrequencyController>(128);
    private static int updateFrame = 0;
    private static int lastRemovedRectIndex = -1;

    [NonSerialized]public HashSet<string> OriginalAnimations = new HashSet<string>();
    [NonSerialized] public int UpdateFrequency = 0;
    private int index = -1;
    private UnityEngine.Animation animation;

    public void Awake()
    {
#if !UNITY_EDITOR
try
{
#endif

        animation = GetComponent<UnityEngine.Animation>();
        index = animations.Count;
        animations.Add(this);

        foreach (UnityEngine.AnimationState state in animation)
        {
            OriginalAnimations.Add(state.name);
        }
    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

    public void OnEnable()
    {
#if !UNITY_EDITOR
try
{
#endif

        if (index == -1)
            index = Add(this);
    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

    public void OnDisable()
    {
#if !UNITY_EDITOR
try
{
#endif

        if (index != -1)
        {
            animations[index] = null;
            lastRemovedRectIndex = index;
            index = -1;
        }
    
#if !UNITY_EDITOR
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
#endif
}

    private static int GetNextAvaiableIndex()
    {
        if (lastRemovedRectIndex < 0)
            return lastRemovedRectIndex;

        for (int i = lastRemovedRectIndex; i < animations.Count; i++)
        {
            if (animations[i] == null)
                return i;
        }

        return -1;
    }

    private static int Add(AnimationUpdateFrequencyController rect)
    {
        var index = GetNextAvaiableIndex();
        if (index >= 0)
        {
            animations[index] = rect;
            return index;
        }
        else
        {
            animations.Add(rect);
            return animations.Count - 1;
        }
    }

    public static void Tick()
    {
        var frameCount = UnityEngine.Time.frameCount;
        var enable = frameCount % GameSetting.Instance.SlowdownAnimationFrameRate == 0;
        var count = animations.Count;
        int nullCount = 0;
        for (int i = 0; i < count; i++)
        {
            var anim = animations[i];
            if (anim != null)
            {
                anim.animation.enabled = anim.UpdateFrequency == 0 ? enable : (frameCount % anim.UpdateFrequency == 0);
            }
            else
            {
                if (nullCount == 0)
                    lastRemovedRectIndex = i;
                nullCount++;
            }
        }

        if ((nullCount << 1) > count)
        {
            Compress();
        }
    }

    private static void Compress()
    {
        animationsTemp.Clear();
        var count = animations.Count;
        for (int i = 0; i < count; ++i)
        {
            if (animations[i] != null)
            {
                animations[i].index = animationsTemp.Count;
                animationsTemp.Add(animations[i]);
            }
        }

        // swap
        var t = animations;
        animations = animationsTemp;
        animationsTemp = t;
    }
}