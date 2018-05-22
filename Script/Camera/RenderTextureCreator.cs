using System;
using UnityEngine;

namespace Assets.Script.Camera
{
    public class RenderTextureCreator : MonoBehaviour
    {
        private int lastScreenWidth;
        public void BindCamera()
        {
            UnityEngine.Camera mainCamera = null;
            if (GameLogic.Instance != null)
            {
                mainCamera = GameLogic.Instance.MainCamera;
            }
            if (null == mainCamera)
            {
                mainCamera = UnityEngine.Camera.main;
            }
            if (null != mainCamera)
            {
                mainCamera.targetTexture = ResourceManager.Instance.getScreenRenderTexture();
            }
            lastScreenWidth = Screen.width;
        }

        public Material mMat;

        public float Brightness = 1.1f;
        // 	public float Saturation = 1;
        public float ContrastFactor = 1.08f;
        public Material Mat
        {
            get
            {
                if (null == mMat)
                {
                    try
                    {
                        mMat = new Material(Shader.Find("Scorpion/ColorAdjustEffect"));
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.Message);
                    }

                }
                return mMat;
            }

        }

        void OnPreRender()
        {

            if (GameSetting.Instance.RenderTextureEnable)
            {

#if UNITY_EDITOR
                if (lastScreenWidth != Screen.width)
                {
                    ResourceManager.Instance.ResizeRenderTexture();
                    lastScreenWidth = Screen.width;
                }
#endif
                var rt = ResourceManager.Instance.getScreenRenderTexture();
                if (null == Mat)
                {
                    enabled = false;
                    return;
                }

                if (GameLogic.Instance && GameLogic.Instance.Scene)
                {
                    Mat.SetColor("_Color", GameLogic.Instance.Scene.Color);
                    Mat.SetFloat("_Brightness", GameLogic.Instance.Scene.Bright);
                }
                else
                {
                    Mat.SetColor("_Color", Color.white);
                    Mat.SetFloat("_Brightness", 1);
                }

                Mat.SetFloat("_ContrastFactor", ContrastFactor);

                if(GameSetting.Instance.GameQualityLevel == 3)
                {
                    Mat.SetTexture("_Distortion", ResourceManager.Instance.getDistortionRenderTexture());
                    Graphics.Blit(rt, null as RenderTexture, Mat, 1);
                }
                else
                {
                    Graphics.Blit(rt, null as RenderTexture, Mat, 0);
                }
            }
        }
    }
}
