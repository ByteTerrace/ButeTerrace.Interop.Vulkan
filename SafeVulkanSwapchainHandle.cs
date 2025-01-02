using Microsoft.Win32.SafeHandles;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace HelloTriangle;

public sealed class SafeVulkanSwapchainHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private unsafe static VkResult IsSurfaceFormatSupported(
        VkPhysicalDevice physicalDevice,
        SafeVulkanSurfaceHandle surfaceHandle,
        VkSwapchainCreateInfoKHR swapchainCreateInfo
    ) {
        var addRefCountSuccess = false;

        try {
            surfaceHandle.DangerousAddRef(success: ref addRefCountSuccess);

            var formatCount = 0U;
            var surface = ((VkSurfaceKHR)surfaceHandle.DangerousGetHandle());

            vkGetPhysicalDeviceSurfaceFormatsKHR(
                physicalDevice: physicalDevice,
                pSurfaceFormatCount: &formatCount,
                pSurfaceFormats: null,
                surface: surface
            );

            using var formatsHandle = SafeUnmanagedMemoryHandle.Create(size: ((nuint)(formatCount * sizeof(VkSurfaceFormatKHR))));

            var formatsPointer = ((VkSurfaceFormatKHR*)formatsHandle.DangerousGetHandle());

            vkGetPhysicalDeviceSurfaceFormatsKHR(
                physicalDevice: physicalDevice,
                pSurfaceFormatCount: &formatCount,
                pSurfaceFormats: formatsPointer,
                surface: surface
            );

            VkSurfaceFormatKHR format;

            for (var i = 0U; (i < formatCount); ++i) {
                format = formatsPointer[i];

                if ((swapchainCreateInfo.imageColorSpace == format.colorSpace) && (swapchainCreateInfo.imageFormat == format.format)) {
                    return VkResult.VK_SUCCESS;
                }
            }

            return VkResult.VK_ERROR_FORMAT_NOT_SUPPORTED;
        }
        finally {
            if (addRefCountSuccess) {
                surfaceHandle.DangerousRelease();
            }
        }
    }
    private unsafe static VkResult IsSurfacePresentModeSupported(
        VkPhysicalDevice physicalDevice,
        SafeVulkanSurfaceHandle surfaceHandle,
        VkSwapchainCreateInfoKHR swapchainCreateInfo
    ) {
        var addRefCountSuccess = false;

        try {
            surfaceHandle.DangerousAddRef(success: ref addRefCountSuccess);

            var presentModeCount = 0U;
            var surface = ((VkSurfaceKHR)surfaceHandle.DangerousGetHandle());

            vkGetPhysicalDeviceSurfacePresentModesKHR(
                physicalDevice: physicalDevice,
                pPresentModeCount: &presentModeCount,
                pPresentModes: null,
                surface: surface
            );

            using var presentModesHandle = SafeUnmanagedMemoryHandle.Create(size: (presentModeCount * sizeof(VkPresentModeKHR)));

            var presentModesPointer = ((VkPresentModeKHR*)presentModesHandle.DangerousGetHandle());

            vkGetPhysicalDeviceSurfacePresentModesKHR(
                physicalDevice: physicalDevice,
                pPresentModeCount: &presentModeCount,
                pPresentModes: presentModesPointer,
                surface: surface
            );

            VkPresentModeKHR presentMode;

            for (var i = 0U; (i < presentModeCount); ++i) {
                presentMode = presentModesPointer[i];

                if (swapchainCreateInfo.presentMode == presentMode) {
                    return VkResult.VK_SUCCESS;
                }
            }

            return VkResult.VK_ERROR_FORMAT_NOT_SUPPORTED;
        }
        finally {
            if (addRefCountSuccess) {
                surfaceHandle.DangerousRelease();
            }
        }
    }

    public unsafe static SafeVulkanSwapchainHandle Create(
        SafeVulkanDeviceHandle logicalDeviceHandle,
        VkPhysicalDevice physicalDevice,
        SafeVulkanSurfaceHandle surfaceHandle,
        VkSwapchainCreateInfoKHR swapchainCreateInfo
    ) {
        var addRefCountSuccess = false;
        var swapchainHandle = new SafeVulkanSwapchainHandle(deviceHandle: logicalDeviceHandle);

        logicalDeviceHandle.DangerousAddRef(success: ref addRefCountSuccess);

        if (addRefCountSuccess) {
            VkSwapchainKHR swapChain;

            if ((VkResult.VK_SUCCESS == IsSurfaceFormatSupported(
                physicalDevice: physicalDevice,
                surfaceHandle: surfaceHandle,
                swapchainCreateInfo: swapchainCreateInfo
            )) && (VkResult.VK_SUCCESS == IsSurfacePresentModeSupported(
                physicalDevice: physicalDevice,
                surfaceHandle: surfaceHandle,
                swapchainCreateInfo: swapchainCreateInfo
            )) && (VkResult.VK_SUCCESS == vkCreateSwapchainKHR(
                device: ((VkDevice)logicalDeviceHandle.DangerousGetHandle()),
                pAllocator: null,
                pCreateInfo: &swapchainCreateInfo,
                pSwapchain: &swapChain
            ))) {
                swapchainHandle.SetHandle(handle: ((nint)swapChain));
            }
            else {
                logicalDeviceHandle.DangerousRelease();
            }
        }

        return swapchainHandle;
    }

    private readonly SafeVulkanDeviceHandle m_deviceHandle;

    private SafeVulkanSwapchainHandle(SafeVulkanDeviceHandle deviceHandle) : base(ownsHandle: true) {
        m_deviceHandle = deviceHandle;
    }

    protected unsafe override bool ReleaseHandle() {
        vkDestroySwapchainKHR(
            device: ((VkDevice)m_deviceHandle.DangerousGetHandle()),
            pAllocator: null,
            swapchain: ((VkSwapchainKHR)handle)
        );
        m_deviceHandle.DangerousRelease();

        return true;
    }
}
