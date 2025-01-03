using Microsoft.Win32.SafeHandles;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace HelloTriangle;

public sealed class SafeVulkanSurfaceHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public unsafe static SafeVulkanSurfaceHandle Create<T>(
        SafeVulkanInstanceHandle instanceHandle,
        VkPhysicalDevice physicalDevice,
        uint queueFamilyIndex,
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
            var isPhysicalDeviceSurfaceSupported = uint.MinValue;
            var surface = default(VkSurfaceKHR);

            if (VkResult.VK_SUCCESS == surfaceCreateInfo switch {
                VkAndroidSurfaceCreateInfoKHR androidSurfaceCreateInfo => instanceHandle.InstanceManualImports.vkCreateAndroidSurfaceKHR(
                    ((VkInstance)instanceHandle.DangerousGetHandle()),
                    &androidSurfaceCreateInfo,
                    ((VkAllocationCallbacks*)pAllocator),
                    &surface
                ),
                VkViSurfaceCreateInfoNN viSurfaceCreateInfo => instanceHandle.InstanceManualImports.vkCreateViSurfaceNN(
                    ((VkInstance)instanceHandle.DangerousGetHandle()),
                    &viSurfaceCreateInfo,
                    ((VkAllocationCallbacks*)pAllocator),
                    &surface
                ),
                VkWaylandSurfaceCreateInfoKHR waylandSurfaceCreateInfo => instanceHandle.InstanceManualImports.vkCreateWaylandSurfaceKHR(
                    ((VkInstance)instanceHandle.DangerousGetHandle()),
                    &waylandSurfaceCreateInfo,
                    ((VkAllocationCallbacks*)pAllocator),
                    &surface
                ),
                VkWin32SurfaceCreateInfoKHR win32SurfaceCreateInfo => instanceHandle.InstanceManualImports.vkCreateWin32SurfaceKHR(
                    ((VkInstance)instanceHandle.DangerousGetHandle()),
                    &win32SurfaceCreateInfo,
                    ((VkAllocationCallbacks*)pAllocator),
                    &surface
                ),
                _ => VkResult.VK_ERROR_UNKNOWN,
            }) {
                surfaceHandle.SetHandle(handle: ((nint)surface));

                if (VkResult.VK_SUCCESS != vkGetPhysicalDeviceSurfaceSupportKHR(
                    physicalDevice: physicalDevice,
                    pSupported: &isPhysicalDeviceSurfaceSupported,
                    queueFamilyIndex: queueFamilyIndex,
                    surface: surface
                )) {
                    surfaceHandle.Dispose();
                    surfaceHandle.SetHandle(handle: nint.Zero);
                }
            }
            else {
                instanceHandle.DangerousRelease();
            }
        }

        return surfaceHandle;
    }
    public unsafe static SafeVulkanSurfaceHandle Create(
        VkPhysicalDevice physicalDevice,
        uint queueFamilyIndex,
        SafeVulkanInstanceHandle vulkanInstanceHandle,
        nint win32InstanceHandle,
        SafeWin32WindowHandle win32WindowHandle,
        nint pAllocator = default
    ) {
        var addRefCountSuccess = false;

        win32WindowHandle.DangerousAddRef(success: ref addRefCountSuccess);

        try {
            SafeVulkanSurfaceHandle surfaceHandle;

            if (VkBool32.TRUE == vulkanInstanceHandle.InstanceManualImports2.vkGetPhysicalDeviceWin32PresentationSupportKHR(
                physicalDevice,
                queueFamilyIndex
            )) {
                surfaceHandle = Create(
                    instanceHandle: vulkanInstanceHandle,
                    pAllocator: pAllocator,
                    physicalDevice: physicalDevice,
                    queueFamilyIndex: queueFamilyIndex,
                    surfaceCreateInfo: new VkWin32SurfaceCreateInfoKHR {
                        flags = uint.MinValue,
                        hinstance = ((void*)win32InstanceHandle),
                        hwnd = ((void*)win32WindowHandle.DangerousGetHandle()),
                        pNext = null,
                        sType = VkStructureType.VK_STRUCTURE_TYPE_WIN32_SURFACE_CREATE_INFO_KHR,
                    }
                );
            }
            else {
                surfaceHandle = new(
                    instanceHandle: vulkanInstanceHandle,
                    pAllocator: pAllocator
                );
            }

            return surfaceHandle;
        }
        finally {
            if (addRefCountSuccess) {
                win32WindowHandle.DangerousRelease();
            }
        }
    }

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
}
