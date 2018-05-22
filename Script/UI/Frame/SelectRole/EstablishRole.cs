using System;
#region using

using UnityEngine;
using System.Collections.Generic;
using System.Collections;


#endregion

namespace GameUI
{
	public class EstablishRole : MonoBehaviour
	{
	    // Use this for initialization
		public List<GameObject> EnableList;
	    public Transform EffectRoot;

		public bool IsPlayingAction { get; private set; }
	    public void PlayAction(Action call=null)
	    {
	        var animation = gameObject.GetComponent<Animation>();
	        animation.Stop();
	        animation.Play("Action");
	
	        animation.CrossFadeQueued("Stand");
			ShowEffect(true);
	
		    if (null != EffectRoot)
		    {
				GameUtils.ResetEffect(EffectRoot.gameObject);    
		    }
			StopAllCoroutines();
		    StartCoroutine(PlayActionCoroutine(call));
	    }
	
		private IEnumerator PlayActionCoroutine(Action call)
		{
			IsPlayingAction = true;
			while (true)
			{
				if (!animation.IsPlaying("Action"))
				{
					break;
				}
				yield return new WaitForEndOfFrame();
			}
			IsPlayingAction = false;
			if (null != call)
			{
				call();
			}
			
		}
	
		public void PlayStand()
		{
			animation.Play("Stand");
			ShowEffect(false);
		}
		public void ShowEffect(bool flag)
		{
			if (null != EnableList)
			{
				foreach (var o in EnableList)
				{
					o.SetActive(flag);
				}
			}
            if (EffectRoot != null && EffectRoot.gameObject != null)
		    {
                EffectRoot.gameObject.SetActive(flag);
		    }
		}
	
	    private void Start()
	    {
	#if !UNITY_EDITOR
	try
	{
	#endif
	
		    PlayStand();
	#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
	#endif
	    }
	}
}