using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace HelloTriangle;

public sealed class SafeWin32WindowClassHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate LRESULT DefWindowProcDelegate(HWND hWnd, uint Msg, WPARAM wParam, LPARAM lParam);

    [SupportedOSPlatform("windows5.0")]
    private static readonly DefWindowProcDelegate DefWindowProc = (HWND hWnd, uint Msg, WPARAM wParam, LPARAM lParam) => {
        return Msg switch {
            _ => PInvoke.DefWindowProc(
                hWnd: hWnd,
                lParam: lParam,
                Msg: Msg,
                wParam: wParam
            ),
        };
    };

    public unsafe static SafeWin32WindowClassHandle Create(
        SafeUnmanagedMemoryHandle classNameHandle,
        nint hInstance
    ) {
        var addRefCountSuccess = false;
        var windowClassSafeHandle = new SafeWin32WindowClassHandle(
            classNameHandle: classNameHandle,
            hInstance: hInstance
        );

        classNameHandle.DangerousAddRef(success: ref addRefCountSuccess);

        if (addRefCountSuccess && OperatingSystem.IsWindowsVersionAtLeast(
            build: 0,
            major: 5,
            minor: 0
        )) {
            WNDCLASSEXW wndclassexw;

            if (!PInvoke.GetClassInfoEx(
                hInstance: ((HINSTANCE)hInstance),
                lpszClass: ((char*)classNameHandle.DangerousGetHandle()),
                lpwcx: &wndclassexw
            )) {
                wndclassexw = new() {
                    cbSize = ((uint)sizeof(WNDCLASSEXW)),
                    hInstance = ((HINSTANCE)hInstance),
                    lpfnWndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)Marshal.GetFunctionPointerForDelegate(d: DefWindowProc),
                    lpszClassName = ((char*)classNameHandle.DangerousGetHandle()),
                };

                var wndatom = PInvoke.RegisterClassEx(param0: &wndclassexw);

                if (0 != wndatom) {
                    windowClassSafeHandle.SetHandle(handle: wndatom);
                }
                else {
                    classNameHandle.DangerousRelease();
                }
            }
        }

        return windowClassSafeHandle;
    }

    private readonly SafeUnmanagedMemoryHandle m_classNameHandle;
    private readonly nint m_hInstance;

    private SafeWin32WindowClassHandle(
        SafeUnmanagedMemoryHandle classNameHandle,
        nint hInstance
    ) : base(ownsHandle: true) {
        m_classNameHandle = classNameHandle;
        m_hInstance = hInstance;
    }

    protected unsafe override bool ReleaseHandle() {
#pragma warning disable CA1416
        var unregisterClassResult = PInvoke.UnregisterClass(
            hInstance: ((HINSTANCE)m_hInstance),
            lpClassName: ((char*)m_classNameHandle.DangerousGetHandle())
        );

        m_classNameHandle.DangerousRelease();

        return unregisterClassResult;
#pragma warning restore CA1416
    }
}
