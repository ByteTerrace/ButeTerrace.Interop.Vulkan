using Microsoft.Win32.SafeHandles;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace HelloTriangle;

public sealed class SafeVulkanSurfaceHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public unsafe static SafeVulkanSurfaceHandle Create<T>(
        SafeVulkanInstanceHandle instanceHandle,
        T surfaceCreateInfo,
        nint pAllocator = default
    ) where T : struct {
        var addRefCountSuccess = false;
        var surfaceHandle = new SafeVulkanSurfaceHandle(
            instanceHandle: instanceHandle,
            pAllocator: pAllocator
        );

        instanceHandle.DangerousAddRef(success: ref addRefCountSuccess);

        if (addRefCountSuccess) {
            surfaceHandle.SetHandle(handle: ((nint)instanceHandle.CreateSurfaceKhr(
                pAllocator: pAllocator,
                surfaceCreateInfo: surfaceCreateInfo
            )));
        }

        return surfaceHandle;
    }
    public unsafe static SafeVulkanSurfaceHandle Create(
        SafeVulkanInstanceHandle vulkanInstanceHandle,
        nint win32InstanceHandle,
        SafeWin32WindowHandle win32WindowHandle,
        nint pAllocator = default
    ) => Create(
        instanceHandle: vulkanInstanceHandle,
        pAllocator: pAllocator,
        surfaceCreateInfo: new VkWin32SurfaceCreateInfoKHR {
            flags = 0U,
            hinstance = ((void*)win32InstanceHandle),
            hwnd = ((void*)win32WindowHandle.DangerousGetHandle()),
            pNext = null,
            sType = VkStructureType.VK_STRUCTURE_TYPE_WIN32_SURFACE_CREATE_INFO_KHR,
        }
    );

    private readonly SafeVulkanInstanceHandle m_instanceHandle;
    private readonly nint m_pAllocator;

    private SafeVulkanSurfaceHandle(
        SafeVulkanInstanceHandle instanceHandle,
        nint pAllocator
    ) : base(ownsHandle: true) {
        m_instanceHandle = instanceHandle;
        m_pAllocator = pAllocator;
    }

    protected unsafe override bool ReleaseHandle() {
        vkDestroySurfaceKHR(
            instance: ((VkInstance)m_instanceHandle.DangerousGetHandle()),
            pAllocator: ((VkAllocationCallbacks*)m_pAllocator),
            surface: ((VkSurfaceKHR)handle)
        );
        m_instanceHandle.DangerousRelease();

        return true;
    }

    public unsafe bool IsPhysicalDeviceSupported(
        VkPhysicalDevice physicalDevice,
        uint queueFamilyIndex
    ) {
        var needsRelease = false;

        try {
            DangerousAddRef(success: ref needsRelease);

            var isSupported = 0U;

            if (VkResult.VK_SUCCESS == vkGetPhysicalDeviceSurfaceSupportKHR(
                physicalDevice: physicalDevice,
                pSupported: &isSupported,
                queueFamilyIndex: queueFamilyIndex,
                surface: ((VkSurfaceKHR)handle)
            )) {
                return Convert.ToBoolean(value: isSupported);
            }
        }
        finally {
            if (needsRelease) {
                DangerousRelease();
            }
        }

        return false;
    }
}
