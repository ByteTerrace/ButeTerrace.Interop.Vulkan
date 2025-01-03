using Microsoft.Win32.SafeHandles;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace HelloTriangle;

public sealed class SafeVulkanSurfaceHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public unsafe static SafeVulkanSurfaceHandle Create<T>(
        SafeVulkanInstanceHandle instanceHandle,
        T surfaceCreateInfo
    ) where T : struct {
        var addRefCountSuccess = false;
        var surfaceHandle = new SafeVulkanSurfaceHandle(vulkanInstanceHandle: instanceHandle);

        instanceHandle.DangerousAddRef(success: ref addRefCountSuccess);

        if (addRefCountSuccess) {
            surfaceHandle.SetHandle(handle: ((nint)instanceHandle.CreateSurfaceKhr(surfaceCreateInfo: surfaceCreateInfo)));
        }

        return surfaceHandle;
    }
    public unsafe static SafeVulkanSurfaceHandle Create(
        SafeVulkanInstanceHandle vulkanInstanceHandle,
        nint win32InstanceHandle,
        SafeWin32WindowHandle win32WindowHandle
    ) => Create(
        instanceHandle: vulkanInstanceHandle,
        surfaceCreateInfo: new VkWin32SurfaceCreateInfoKHR {
            flags = 0U,
            hinstance = ((void*)win32InstanceHandle),
            hwnd = ((void*)win32WindowHandle.DangerousGetHandle()),
            pNext = null,
            sType = VkStructureType.VK_STRUCTURE_TYPE_WIN32_SURFACE_CREATE_INFO_KHR,
        }
    );

    private readonly SafeVulkanInstanceHandle m_vulkanInstanceHandle;

    private SafeVulkanSurfaceHandle(SafeVulkanInstanceHandle vulkanInstanceHandle) : base(ownsHandle: true) {
        m_vulkanInstanceHandle = vulkanInstanceHandle;
    }

    protected unsafe override bool ReleaseHandle() {
        vkDestroySurfaceKHR(
            instance: ((VkInstance)m_vulkanInstanceHandle.DangerousGetHandle()),
            pAllocator: null,
            surface: ((VkSurfaceKHR)handle)
        );
        m_vulkanInstanceHandle.DangerousRelease();

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
