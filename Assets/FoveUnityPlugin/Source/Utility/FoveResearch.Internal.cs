using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Fove.Unity
{
    public static partial class FoveResearch
    {
        private static Result<Texture2D> m_sEyesTexture = new Result<Texture2D>(null, ErrorCode.Data_NoUpdate);
        private static Result<Texture2D> m_sPositionTexture = new Result<Texture2D>(null, ErrorCode.Data_NoUpdate);
        private static Result<Texture2D> m_sMirrorTexture = new Result<Texture2D>(null, ErrorCode.Data_NoUpdate);

        private static Bitmap m_sEyesImage;
        private static Bitmap m_sPositionImage;

        private static HeadsetResearch m_sResearch;

        private static Result<ResearchGaze> m_sResearchGaze = new Result<ResearchGaze>(new ResearchGaze(), ErrorCode.Data_NoUpdate);
        private static Result<Stereo<EyeShape>> m_sEyeShapes = new Result<Stereo<EyeShape>>(new Stereo<EyeShape>(), ErrorCode.Data_NoUpdate);

        private static ResearchCapabilities m_sCaps;

        private static bool m_sGazeUpdateRegistered;
        private static bool m_sEyeShapesUpdateRegistered;
        private static bool m_sEyeTextureUpdateRegistered;
        private static bool m_sPositionTextureUpdateRegistered;
        private static bool m_sMirrorTextureUpdateRegistered;

        private static void EnsureInitialization()
        {
            if (m_sResearch != null)
                return;

            try
            {
                m_sResearch = FoveManager.Headset.GetResearchHeadset(ResearchCapabilities.None);
            }
            catch(Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        private static void EnsureCapabilities(ResearchCapabilities capabilities)
        {
            EnsureInitialization();
            var newCaps = ~m_sCaps & capabilities;
            if (newCaps != 0)
            {
                m_sResearch.RegisterCapabilities(newCaps);
                m_sCaps |= newCaps;
            }
        }

        private delegate ErrorCode GetImageDelegate(out Bitmap img);

        private static GetImageDelegate GetEyesImage = (out Bitmap img) => m_sResearch.GetImage(ImageType.StereoEye, out img);
        private static GetImageDelegate GetPositionImage = (out Bitmap img) => m_sResearch.GetImage(ImageType.Position, out img);

        private static void TryUpdatingTexture(GetImageDelegate func, ref Bitmap bimg, ref Result<Texture2D> texResult)
        {
            try 
            {
                Bitmap temp;
                texResult.error = func(out temp);
                if (texResult.HasError)
                    return;

                if (bimg != null && temp.Timestamp <= bimg.Timestamp)
                    return;
                bimg = temp;

                if (bimg.Width == 0 || bimg.Height == 0)
                {
                    return;
                }

                var tex = texResult.value;
                if (tex != null && (tex.width != bimg.Width || tex.height != bimg.Height))
                {
                    Texture2D.Destroy(tex);
                    tex = null;
                }

                if (tex == null)
                    tex = new Texture2D(bimg.Width, bimg.Height, TextureFormat.RGB24, false);

                tex.LoadRawTextureData(bimg.ImageData.data, (int)bimg.ImageData.length);
                tex.Apply();

                texResult.value = tex;
            }
            catch (Exception e)
            {
                Debug.Log("Error trying to load eyes image bitmap: " + e);
            }
        }

        private static void UpdateResearchGaze()
        {
            m_sResearchGaze = GetResearchGaze(true);
        }

        private static void UpdateEyeShapes()
        {
            Fove.EyeShape shapeL, shapeR;
            m_sEyeShapes.error = m_sResearch.GetEyeShapes(out shapeL, out shapeR);
            if (m_sEyeShapes.Succeeded)
            {
                m_sEyeShapes.value.left = (EyeShape)shapeL;
                m_sEyeShapes.value.right = (EyeShape)shapeR;
            }
        }

        private static void UpdateEyesImage()
        {
            TryUpdatingTexture(GetEyesImage, ref m_sEyesImage, ref m_sEyesTexture);
        }

        private static void UpdatePositionImage()
        {
            TryUpdatingTexture(GetPositionImage, ref m_sPositionImage, ref m_sPositionTexture);
        }

        private static void UpdateMirrorTexture()
        {
            // update the mirror texture native pointer if is exists
            // we need this update because of the rolling buffer
            if (m_sMirrorTexture.value != null)
            {
                int dummy;
                IntPtr texPtr;
                GetMirrorTexturePtr(out texPtr, out dummy, out dummy);
                m_sMirrorTexture.value.UpdateExternalTexture(texPtr);
            }
        }

        [DllImport("FoveUnityFuncs", EntryPoint = "getMirrorTexturePtr")]
        private static extern void GetMirrorTexturePtr(out IntPtr texPtr, out int texWidth, out int texHeight);
    }
}
