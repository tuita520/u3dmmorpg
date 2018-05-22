using EventSystem;
using System;
#region using
using ClientDataModel;
using UnityEngine;

#endregion

namespace GameUI
{
    public class EraBookCellFrame : MonoBehaviour
    {
        public BindDataRoot BindRoot;
        private EraBookCellDataModel dataModel = new EraBookCellDataModel();
        public GameObject Effect;
        private GameObject effectGameObject;
        public string EffectPath;

        public EraBookCellDataModel ItemData
        {
            get { return dataModel; }
            set
            {
                dataModel = value;
                if (BindRoot != null)
                {
                    BindRoot.SetBindDataSource(dataModel);
                }
            }
        }

        public bool PlayAnim
        {
            set
            {
                if (value)
                {
                    if (!CreateAnim())
                    {
                        Effect.SetActive(false);
                    }
                    Effect.SetActive(true);                        
                }
            }
        }

        private bool CreateAnim()
        {
            if (effectGameObject != null)
                return false;

            DestroyAnim();
            ComplexObjectPool.NewObject(EffectPath, go =>
            {
                if (null == go)
                {
                    return;
                }

                effectGameObject = go;

                go.transform.parent = Effect.transform;
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
                go.transform.localRotation = Quaternion.Euler(0, 0, 0);

            });

            return true;
        }

        private void DestroyAnim()
        {
            if (effectGameObject != null)
            {
                ComplexObjectPool.Release(effectGameObject, true);
                effectGameObject = null;
            }
        }

        //点击Icon
        public void OnClickIcon()
        {
            EventDispatcher.Instance.DispatchEvent(new Event_EraCellClick(dataModel));
        }


        public void OnClickGoto()
        {
            //发送事件
        }


        private void OnDisable()
        {
#if !UNITY_EDITOR
            try
            {
#endif

            DestroyAnim();

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
