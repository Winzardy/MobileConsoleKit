using System;
using System.Collections;
using UnityEngine;

namespace MobileConsole
{
	/// <summary>
	/// Captures what the player sees. The console canvas is switched off for one frame, otherwise
	/// every screenshot would show the console instead of the game.
	/// </summary>
	internal static class ScreenshotCapture
	{
		public static void Capture(int maxSize, Action<byte[]> onComplete)
		{
			BugReportRunner.Instance.StartCoroutine(CaptureRoutine(maxSize, onComplete));
		}

		static IEnumerator CaptureRoutine(int maxSize, Action<byte[]> onComplete)
		{
			LogConsole.SetRenderingEnabled(false);

			// The screen can only be read after everything has been rendered
			yield return new WaitForEndOfFrame();

			byte[] png = null;
			Texture2D screenTexture = null;
			Texture2D resizedTexture = null;

			try
			{
				screenTexture = ScreenCapture.CaptureScreenshotAsTexture();
				resizedTexture = Downscale(screenTexture, maxSize);
				png = resizedTexture.EncodeToPNG();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
			finally
			{
				if (resizedTexture != null && resizedTexture != screenTexture)
				{
					UnityEngine.Object.Destroy(resizedTexture);
				}

				if (screenTexture != null)
				{
					UnityEngine.Object.Destroy(screenTexture);
				}

				LogConsole.SetRenderingEnabled(true);
			}

			if (onComplete != null)
			{
				onComplete(png);
			}
		}

		static Texture2D Downscale(Texture2D source, int maxSize)
		{
			if (source == null || maxSize <= 0)
				return source;

			int longestSide = Mathf.Max(source.width, source.height);
			if (longestSide <= maxSize)
				return source;

			float scale = (float)maxSize / longestSide;
			int width = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
			int height = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));

			RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
			RenderTexture previousActive = RenderTexture.active;

			try
			{
				Graphics.Blit(source, renderTexture);
				RenderTexture.active = renderTexture;

				Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
				result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
				result.Apply();
				return result;
			}
			finally
			{
				RenderTexture.active = previousActive;
				RenderTexture.ReleaseTemporary(renderTexture);
			}
		}
	}
}
