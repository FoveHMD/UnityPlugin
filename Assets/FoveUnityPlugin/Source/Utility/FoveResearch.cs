using System;
using UnityEngine;

namespace Fove.Unity
{
    /// <summary>
    /// Static class to query Fove research information
    /// </summary>
    /// <remarks>Client backward compatibility is not ensure for Research features.</remarks>
    public static partial class FoveResearch
    {
        /// <summary>
        /// Get the headset eye camera image texture
        /// </summary>
        public static Result<Texture2D> EyesTexture
        {
            get
            {
                if (!m_sEyeTextureUpdateRegistered)
                {
                    EnsureCapabilities(ResearchCapabilities.EyeImage);
                    FoveManager.AddInUpdate += UpdateEyesImage;
                    m_sEyeTextureUpdateRegistered = true;
                }
                return m_sEyesTexture;
            }
        }

        /// <summary>
        /// Get the position camera image texture
        /// </summary>
        public static Result<Texture2D> PositionTexture
        {
            get
            {
                if (!m_sPositionTextureUpdateRegistered)
                {
                    EnsureCapabilities(ResearchCapabilities.PositionImage);
                    FoveManager.AddInUpdate += UpdatePositionImage;
                    m_sPositionTextureUpdateRegistered = true;
                }
                return m_sPositionTexture;
            }
        }

        /// <summary>
        /// Get the texture displayed in the mirror client
        /// </summary>
        public static Result<Texture2D> MirrorTexture
        {
            get
            {
                if (!m_sMirrorTextureUpdateRegistered)
                {
                    EnsureInitialization();
                    FoveManager.AddInUpdate += UpdateMirrorTexture;
                    m_sMirrorTextureUpdateRegistered = true;
                }

                if (m_sMirrorTexture.value == null)
                {
                    IntPtr texPtr;
                    int texWidth, texHeight;
                    GetMirrorTexturePtr(out texPtr, out texWidth, out texHeight);
                    if (texPtr != IntPtr.Zero) // the mirror texture doesn't exist yet
                    {
                        m_sMirrorTexture.value = Texture2D.CreateExternalTexture(texWidth, texHeight, TextureFormat.RGBA32, false, false, texPtr);
                        m_sMirrorTexture.error = ErrorCode.None;
                    }
                }

                return m_sMirrorTexture;
            }
        }

        /// <summary>
        /// Return the research gaze information.
        /// </summary>
        /// <param name="immediate">If true re-query the value to the headset, otherwise it returns the value cached at the beginning of the frame.</param>
        public static Result<ResearchGaze> GetResearchGaze(bool immediate = false)
        {
            if (!m_sGazeUpdateRegistered)
            {
                EnsureInitialization();
                FoveManager.AddInUpdate += UpdateResearchGaze;
                m_sGazeUpdateRegistered = true;
            }

            if (immediate)
                return m_sResearchGaze;

            ResearchGaze gaze;
            var error = m_sResearch.GetGaze(out gaze);
            return new Result<ResearchGaze>(gaze, error);
        }

        /// <summary>
        /// Return the eye outline points in pixels in the <see cref="GetEyesImage"/> .
        /// </summary>
        /// <remarks>This feature requires a license</remarks>
        /// <param name="immediate">If true re-query the value to the headset, otherwise it returns the value cached at the beginning of the frame.</param>
        public static Result<Stereo<EyeShape>> GetEyeShapes(bool immediate = false)
        {
            if (!m_sEyeShapesUpdateRegistered)
            {
                EnsureInitialization();
                FoveManager.Headset.RegisterCapabilities(ClientCapabilities.Gaze);
                FoveManager.AddInUpdate += UpdateEyeShapes;
                m_sEyeShapesUpdateRegistered = true;
            }

            if (immediate)
                return m_sEyeShapes;

            Fove.EyeShape shapeL, shapeR;
            var error = m_sResearch.GetEyeShapes(out shapeL, out shapeR);
            if (error != ErrorCode.None)
                return new Result<Stereo<EyeShape>>(m_sEyeShapes);

            var eyeShapes = new Stereo<EyeShape>((EyeShape)shapeL, (EyeShape)shapeR);
            return new Result<Stereo<EyeShape>>(eyeShapes);
        }
    }
}
