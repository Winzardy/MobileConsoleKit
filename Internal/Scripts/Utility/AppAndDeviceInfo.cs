using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MobileConsole
{
	public static class AppAndDeviceInfo
	{
		public delegate void CustomInfoProvider(StringBuilder sb);

		static readonly Dictionary<string, CustomInfoProvider> _customInfoProviders = new Dictionary<string, CustomInfoProvider>();

		/// <summary>
		/// Registers a callback that writes directly into the <see cref="StringBuilder"/> used by
		/// <see cref="FullInfos"/>, under a section named after <paramref name="title"/>. The title
		/// is also the key used to <see cref="UnregisterCustomInfoProvider"/> it later. Dispose the
		/// returned handle (e.g. via a <c>using</c> block) to unregister instead.
		/// <code>
		/// AppAndDeviceInfo.RegisterCustomInfoProvider("My Info", sb => sb.AppendLine("Value: " + Value));
		/// </code>
		/// </summary>
		public static IDisposable RegisterCustomInfoProvider(string title, CustomInfoProvider provider)
		{
			if (string.IsNullOrEmpty(title) || provider == null)
			{
				Debug.LogErrorFormat("AppAndDeviceInfo.RegisterCustomInfoProvider was called with an invalid title [{0}] or a null provider", title);
				return null;
			}

			_customInfoProviders[title] = provider;
			return new CustomInfoProviderHandle(title);
		}

		/// <summary>Removes a callback previously passed to <see cref="RegisterCustomInfoProvider"/>.</summary>
		public static void UnregisterCustomInfoProvider(string title)
		{
			if (string.IsNullOrEmpty(title))
			{
				Debug.LogErrorFormat("AppAndDeviceInfo.UnregisterCustomInfoProvider was called with an invalid title [{0}]", title);
				return;
			}

			if (!_customInfoProviders.Remove(title))
			{
				Debug.LogWarningFormat("AppAndDeviceInfo.UnregisterCustomInfoProvider: no provider registered with title [{0}]", title);
			}
		}

		/// <summary>Unregisters its provider on <see cref="Dispose"/>, at most once.</summary>
		sealed class CustomInfoProviderHandle : IDisposable
		{
			readonly string _title;
			bool _disposed;

			public CustomInfoProviderHandle(string title)
			{
				_title = title;
			}

			public void Dispose()
			{
				if (_disposed)
					return;

				_disposed = true;
				UnregisterCustomInfoProvider(_title);
			}
		}

		public static string FullInfos()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("--- App Info ---");
			sb.AppendLine("App name: " + Application.productName);
			sb.AppendLine("Bundle identifier: " + EventBridge.AppBundleIdentifier);
			sb.AppendFormat("App version: {0} ({1})\n", Application.version, EventBridge.AppVersionCode);
			sb.AppendLine("Unity version: " + Application.unityVersion);

			// Device info
			sb.AppendLine();
			sb.AppendLine("--- Device Info ---");
			sb.AppendLine("Device name: " + SystemInfo.deviceName);
			sb.AppendLine("Device model: " + SystemInfo.deviceModel);
			sb.AppendLine("Operation system: " + SystemInfo.operatingSystem);
			sb.AppendLine("System language: " + Application.systemLanguage.ToString());
			sb.AppendLine("Device orientation: " + Input.deviceOrientation.ToString());
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				sb.AppendLine("Connectivity: None");
			}
			else if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork)
			{
				sb.AppendLine("Connectivity: Carrier Data Network (Cellular)");
			}
			else if (Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
			{
				sb.AppendLine("Connectivity: Local (LAN/Wifi)");
			}

			sb.AppendLine();
			sb.AppendLine("--- CPU Info ---");
			sb.AppendFormat("CPU type: {0} ({1} core(s))\n", SystemInfo.processorType, SystemInfo.processorCount);
			if (Application.platform != RuntimePlatform.IPhonePlayer && Application.platform != RuntimePlatform.WebGLPlayer)
			{
				sb.AppendFormat("CPU speed: {0} MHz\n", SystemInfo.processorFrequency);
			}
			sb.AppendFormat("System memory size: {0} MB\n", SystemInfo.systemMemorySize);
			sb.AppendFormat("Allocated memory: {0} MB\n", EventBridge.RequestAllocatedMemory().ToString("n2"));
			sb.AppendFormat("Reserved memory: {0} MB\n", EventBridge.RequestReservedMemory().ToString("n2"));
			sb.AppendFormat("Mono used memory: {0} MB\n", EventBridge.RequestMonoUsedMemory().ToString("n2"));

			sb.AppendLine();
			sb.AppendLine("--- GPU Info ---");
			sb.AppendLine("GPU: " + SystemInfo.graphicsDeviceName);
			sb.AppendFormat("Graphic memory size: {0} MB\n", SystemInfo.graphicsMemorySize);
			sb.AppendFormat("Screen size: {0}x{1}@{2}Hz\n", Screen.currentResolution.width, Screen.currentResolution.height, Screen.currentResolution.refreshRateRatio);
			sb.AppendLine("Screen dpi: " + Screen.dpi);

			AppendCustomInfo(sb);

			return sb.ToString();
		}

		static void AppendCustomInfo(StringBuilder sb)
		{
			foreach (var pair in _customInfoProviders)
			{
				sb.AppendLine();
				sb.AppendLine("--- " + pair.Key + " ---");

				try
				{
					pair.Value(sb);
				}
				catch (Exception e)
				{
					Debug.LogWarningFormat("AppAndDeviceInfo custom info provider [{0}] has failed: {1}", pair.Key, e.Message);
					sb.AppendLine("<error: " + e.Message + ">");
				}
			}
		}
	}
}
