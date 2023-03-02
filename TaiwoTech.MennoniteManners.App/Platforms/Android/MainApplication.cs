using Android.App;
using Android.Runtime;

namespace TaiwoTech.MennoniteManners.App;

#if DEBUG
[Application(UsesCleartextTraffic = true)]  // connect to local service
#else                                       // on the host for debugging,
[Application]     
#endif
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
