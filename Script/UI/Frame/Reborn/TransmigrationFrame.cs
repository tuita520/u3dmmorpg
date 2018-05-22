using System;
#region using

using System.Collections;
using EventSystem;
using UnityEngine;
using ClientDataModel;

#endregion

namespace GameUI
{
	public class TransmigrationFrame : MonoBehaviour
	{
		private Coroutine playActionCoroutine;
        private Coroutine rebornAniAndEffCoroutine;
        private Coroutine playRebornAniCoroutine;
		public BindDataRoot Binding;
		public UIDragRotate ModelDrag;
		public CreateFakeCharacter ModelRoot;
		public UISpriteAnimation ReboronAnimation;
        public GameObject rebornEffect;
        public GameObject rebornAni;
        //public RebornDataModel dataModel=new RebornDataModel ();
        
		private IEnumerator AnimationCoroutine()
		{
			if (ReboronAnimation != null)
			{
				while (ReboronAnimation.isPlaying)
				{
					yield return new WaitForSeconds(0.1f);
				}
			}
			yield return new WaitForSeconds(1.0f);
			ReboronAnimation.ResetToBeginning();
			ReboronAnimation.gameObject.SetActive(false);
			ReboronAnimation.enabled = false;
			PlayAnimationFinished();
		}

		public void CreateFakeCharacter()
		{
			DestroyFakeCharacter();

			var player = ObjManager.Instance.MyPlayer;
			ModelRoot.Create(player.GetDataId(), player.EquipList,
				character => { ModelDrag.Target = character.gameObject.transform; });
		}

		public void DestroyFakeCharacter()
		{
			if (null != ModelRoot.Character)
			{
				ModelRoot.DestroyFakeCharacter();
			}
		}

		public void OnClickBtnClose()
		{
			var e = new Close_UI_Event(UIConfig.RebornUi, true);
			EventDispatcher.Instance.DispatchEvent(e);
		}
        
		public void OnClickBtnReborn()
		{

			var e = new RebornOperateEvent(0);
			EventDispatcher.Instance.DispatchEvent(e);          
		}
      
        public void OnClickBtnGoToTask()
        {
            var e = new Close_UI_Event(UIConfig.RebornUi, true);
            EventDispatcher.Instance.DispatchEvent(e);

            var e1 = new RebornOperateEvent(1);
            EventDispatcher.Instance.DispatchEvent(e1);  
        }

		private void OnDestroy()
		{
#if !UNITY_EDITOR
	try
	{
#endif
            EventDispatcher.Instance.RemoveEventListener(RebornAniAndEffPlayOn.EVENT_TYPE, RebornAniAndEffPlay);
            EventDispatcher.Instance.RemoveEventListener(RebornPlayAnimation.EVENT_TYPE, OnPlayRebornAnimation);

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

			Binding.RemoveBinding();
			DestroyFakeCharacter();
			if (playActionCoroutine != null)
			{
				NetManager.Instance.StopCoroutine(playActionCoroutine);
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
            rebornEffect.gameObject.SetActive(false);
			var controllerBase = UIManager.Instance.GetController(UIConfig.RebornUi);
			if (controllerBase == null)
			{
				return;
			}
			Binding.SetBindDataSource(controllerBase.GetDataModel(""));
            controllerBase = UIManager.Instance.GetController(UIConfig.MissionTrackList);
            Binding.SetBindDataSource(controllerBase.GetDataModel(""));

			CreateFakeCharacter();

#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
#endif
		}

		private void OnPlayAnimation(IEvent ievent)
		{
			if (ReboronAnimation != null)
			{
				ReboronAnimation.gameObject.SetActive(true);
				ReboronAnimation.enabled = true;
				if (playActionCoroutine != null)
				{
					NetManager.Instance.StopCoroutine(playActionCoroutine);
				}
				playActionCoroutine = NetManager.Instance.StartCoroutine(AnimationCoroutine());
			}
		}

        private void OnPlayRebornAnimation(IEvent ievent)
        {
            if (rebornAni)
            {
                if (playRebornAniCoroutine != null)
                {
                    NetManager.Instance.StopCoroutine(playRebornAniCoroutine);
                }
                playRebornAniCoroutine = NetManager.Instance.StartCoroutine(PlayRebornAnimation());
            }
        }

        private IEnumerator PlayRebornAnimation()
        {
            rebornAni.SetActive(true);
            yield return new WaitForSeconds(2f);
            rebornAni.SetActive(false);
            var e = new Close_UI_Event(UIConfig.RebornUi, true);
            EventDispatcher.Instance.DispatchEvent(e);
        }

        private void RebornAniAndEffPlay(IEvent ievent)
        {
            if (rebornEffect != null)
            {
                rebornEffect.gameObject.SetActive(false);
   
                if (rebornAniAndEffCoroutine != null)
                {
                    NetManager.Instance.StopCoroutine(rebornAniAndEffCoroutine);
                }
                rebornAniAndEffCoroutine = NetManager.Instance.StartCoroutine(ReincarnationAniPlay());
            }
        }
        private IEnumerator ReincarnationAniPlay()
        {
            rebornEffect.gameObject.SetActive(true);
            ModelRoot.Character.PlayAnimation(OBJ.CHARACTER_ANI.Reborn, aniName => ModelRoot.Character.PlayAnimation(OBJ.CHARACTER_ANI.STAND));
            yield return new WaitForSeconds(2f);
            rebornEffect.gameObject.SetActive(false);
        }

		public void PlayAnimationFinished()
		{
			var e = new Close_UI_Event(UIConfig.RebornUi, true);
			EventDispatcher.Instance.DispatchEvent(e);
		}

		private void Start()
		{
#if !UNITY_EDITOR
	try
	{
#endif
            EventDispatcher.Instance.AddEventListener(RebornPlayAnimation.EVENT_TYPE, OnPlayRebornAnimation);
            EventDispatcher.Instance.AddEventListener(RebornAniAndEffPlayOn.EVENT_TYPE, RebornAniAndEffPlay);

#if !UNITY_EDITOR
	}
	catch (Exception ex)
	{
	    Logger.Error(ex.ToString());
	}
#endif
		}

		private void Update()
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
	}
}