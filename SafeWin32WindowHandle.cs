using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace HelloTriangle;

public sealed class SafeWin32WindowHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public unsafe static SafeWin32WindowHandle Create(
        WINDOW_EX_STYLE extendedStyle,
        int height,
        nint hInstance,
        WINDOW_STYLE style,
        int width,
        int x,
        int y,
        string windowName,
        SafeWin32WindowClassHandle win32WindowClassHandle
    ) {
        var addRefCountSuccess = false;
        var windowClassSafeHandle = new SafeWin32WindowHandle(
            win32WindowClassHandle: win32WindowClassHandle
        );

        win32WindowClassHandle.DangerousAddRef(success: ref addRefCountSuccess);

        if (addRefCountSuccess && OperatingSystem.IsWindowsVersionAtLeast(
            build: 0,
            major: 5,
            minor: 0
        )) {
            var hWnd = PInvoke.CreateWindowEx(
                dwExStyle: extendedStyle,
                dwStyle: style,
                hInstance: ((HINSTANCE)hInstance),
                hMenu: HMENU.Null,
                hWndParent: HWND.Null,
                lpClassName: ((char*)win32WindowClassHandle.DangerousGetHandle()),
                lpParam: null,
                lpWindowName: ((char*)Unsafe.AsPointer(value: ref MemoryMarshal.GetReference(span: (windowName + '\0').AsSpan()))),
                nHeight: height,
                nWidth: width,
                X: x,
                Y: y
            );

            if (HWND.Null != hWnd) {
                windowClassSafeHandle.SetHandle(handle: hWnd);
            }
            else {
                win32WindowClassHandle.DangerousRelease();
            }
        }

        return windowClassSafeHandle;
    }

    private readonly SafeWin32WindowClassHandle m_win32WindowClassHandle;

    private SafeWin32WindowHandle(SafeWin32WindowClassHandle win32WindowClassHandle) : base(ownsHandle: true) {
        m_win32WindowClassHandle = win32WindowClassHandle;
    }

    protected unsafe override bool ReleaseHandle() {
#pragma warning disable CA1416
        var destroyWindowResult = PInvoke.DestroyWindow(hWnd: ((HWND)handle));
#pragma warning restore CA1416

        m_win32WindowClassHandle.DangerousRelease();

        return destroyWindowResult;
    }
}
