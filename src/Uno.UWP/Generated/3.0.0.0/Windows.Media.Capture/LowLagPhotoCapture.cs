#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class LowLagPhotoCapture 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Capture.CapturedPhoto> CaptureAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CapturedPhoto> LowLagPhotoCapture.CaptureAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CCapturedPhoto%3E%20LowLagPhotoCapture.CaptureAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction FinishAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction LowLagPhotoCapture.FinishAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20LowLagPhotoCapture.FinishAsync%28%29");
		}
		#endif
	}
}
