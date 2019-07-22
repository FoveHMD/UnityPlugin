using System;
using UnityEngine;

namespace Fove.Unity
{
	public static class FoveResearch
	{
		private static Texture2D m_sEyesTexture;
		private static Texture2D m_sPositionTexture;

		private static Bitmap m_sEyesImage;
		private static Bitmap m_sPositionImage;

		private static HeadsetResearch m_sResearch;

		private static ResearchGaze m_sResearchGaze;

		public static Texture2D EyesTexture
		{
			get
			{
				EnsureInitialization();
				return m_sEyesTexture;
			}
		}

		public static Texture2D PositionTexture
		{
			get
			{
				EnsureInitialization();
				return m_sPositionTexture;
			}
		}

		public static ResearchGaze GetResearchGaze(bool immediate = false)
		{
			EnsureInitialization();

			if (immediate)
				return m_sResearchGaze;

			ResearchGaze gaze;
			m_sResearch.GetGaze(out gaze);
			return gaze;
		}

		private static void AcceptAddIn(Headset headset)
		{
			m_sResearch = headset.GetResearchHeadset(ResearchCapabilities.EyeImage | ResearchCapabilities.PositionImage);
		}

		private static void EnsureInitialization()
		{
			if (m_sResearch != null)
				return;

			AcceptAddIn(FoveManager.Headset);
			FoveManager.AddInUpdate += UpdateImages;
			m_sEyesTexture = null;
			m_sPositionTexture = null;

			m_sResearch.RegisterCapabilities(ResearchCapabilities.EyeImage | ResearchCapabilities.PositionImage);
		}

		private delegate ErrorCode GetImageDelegate(out Bitmap img);

		private static GetImageDelegate GetEyesImage = (out Bitmap img) => m_sResearch.GetImage(ImageType.StereoEye, out img);
		private static GetImageDelegate GetPositionImage = (out Bitmap img) => m_sResearch.GetImage(ImageType.Position, out img);

		private static void TryUpdatingTexture(GetImageDelegate func, ref Bitmap bimg, ref Texture2D tex)
		{
			try {
				Bitmap temp;
				var err = func(out temp);

				if (err == ErrorCode.None)
				{
					if (bimg != null && temp.Timestamp <= bimg.Timestamp)
						return;
					bimg = temp;

					if (bimg.Width == 0 || bimg.Height == 0)
					{
						return;
					}

					if (tex != null && (tex.width != bimg.Width || tex.height != bimg.Height))
					{
						Texture2D.Destroy(tex);
						tex = null;
					}

					if (tex == null)
						tex = new Texture2D(bimg.Width, bimg.Height, TextureFormat.RGB24, false);

					tex.LoadRawTextureData(bimg.ImageData.data, (int)bimg.ImageData.length);
					tex.Apply();
				}
				else
				{
					//... log?
				}
			}
			catch (Exception e)
			{
				Debug.Log("Error trying to load eyes image bitmap: " + e);
			}
		}

		private static void UpdateImages()
		{
			m_sResearchGaze = GetResearchGaze(true);
			TryUpdatingTexture(GetEyesImage, ref m_sEyesImage, ref m_sEyesTexture);
			TryUpdatingTexture(GetPositionImage, ref m_sPositionImage, ref m_sPositionTexture);
		}
	}
}
