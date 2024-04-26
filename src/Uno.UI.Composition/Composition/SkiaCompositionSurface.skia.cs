﻿#nullable enable

using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	internal partial class SkiaCompositionSurface : CompositionObject, ICompositionSurface
	{
		// Don't set this field directly. Use SetFrameProviderAndOnFrameChanged instead.
		private ImageFrameProvider? _frameProvider;

		// Unused: But intentionally kept!
		// This is here to keep the Action lifetime the same as SkiaCompositionSurface.
		// i.e, only cause the Action to be GC'ed if SkiaCompositionSurface is GC'ed.
		private Action? _onFrameChanged;

		private void SetFrameProviderAndOnFrameChanged(ImageFrameProvider? provider, Action? onFrameChanged)
		{
			_frameProvider?.Dispose();
			_frameProvider = provider;
			_onFrameChanged = onFrameChanged;
		}

		public SKImage? Image => _frameProvider?.CurrentImage;

		internal SkiaCompositionSurface(SKImage image)
		{
			_frameProvider = ImageFrameProvider.Create(image);
		}

		internal (bool success, object nativeResult) LoadFromStream(Stream imageStream) => LoadFromStream(null, null, imageStream);

		internal (bool success, object nativeResult) LoadFromStream(int? targetWidth, int? targetHeight, Stream imageStream)
		{
			using var stream = new SKManagedStream(imageStream);

			if (targetWidth is int actualTargetWidth && targetHeight is int actualTargetHeight)
			{
				using var codec = SKCodec.Create(stream);

				var bitmap = new SKBitmap(actualTargetWidth, actualTargetHeight, SKColorType.Bgra8888, SKAlphaType.Premul);

				var result = codec.GetPixels(bitmap.Info, bitmap.GetPixels());

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Image load result {result}");
				}

				if (result == SKCodecResult.Success)
				{
					SetFrameProviderAndOnFrameChanged(ImageFrameProvider.Create(SKImage.FromBitmap(bitmap)), null);
				}

				return (result == SKCodecResult.Success || result == SKCodecResult.IncompleteInput, result);
			}
			else
			{
				try
				{
					using var codec = SKCodec.Create(stream);
					var onFrameChanged = () => NativeDispatcher.Main.Enqueue(() => OnPropertyChanged(nameof(Image), isSubPropertyChange: false), NativeDispatcherPriority.High);
					if (!ImageFrameProvider.TryCreate(codec, onFrameChanged, out var provider))
					{
						SetFrameProviderAndOnFrameChanged(null, null);
						return (false, "Failed to decode image");
					}

					SetFrameProviderAndOnFrameChanged(provider, onFrameChanged);
					GC.KeepAlive(onFrameChanged);
					return (true, "Success");
				}
				catch (Exception e)
				{
					SetFrameProviderAndOnFrameChanged(null, null);
					return (false, e.Message);
				}
			}
		}

		/// <summary>
		/// Copies the provided pixels to the composition surface
		/// </summary>
		internal unsafe void CopyPixels(int pixelWidth, int pixelHeight, ReadOnlyMemory<byte> data)
		{
			var info = new SKImageInfo(pixelWidth, pixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);

			using (var pData = data.Pin())
			{
				SetFrameProviderAndOnFrameChanged(ImageFrameProvider.Create(SKImage.FromPixelCopy(info, (IntPtr)pData.Pointer, pixelWidth * 4)), null);
			}
		}

		private protected override void DisposeInternal()
		{
			base.DisposeInternal();
			SetFrameProviderAndOnFrameChanged(null, null);
			GC.SuppressFinalize(this);
		}

		~SkiaCompositionSurface()
		{
			this.Dispose();
		}
	}
}
