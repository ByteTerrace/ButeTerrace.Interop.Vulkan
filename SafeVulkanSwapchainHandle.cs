using Microsoft.Win32.SafeHandles;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace HelloTriangle;

public sealed class SafeVulkanSwapchainHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private unsafe static VkResult IsImageFormatSupported(
        VkPhysicalDevice physicalDevice,
        VkSwapchainCreateInfoKHR swapchainCreateInfo
    ) {
        var deviceImageFormatInfo = new VkPhysicalDeviceImageFormatInfo2 {
            flags = uint.MinValue,
            format = swapchainCreateInfo.imageFormat,
            pNext = null,
            sType = VkStructureType.VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_IMAGE_FORMAT_INFO_2,
            usage = swapchainCreateInfo.imageUsage,
        };
        var imageFormatProperties = new VkImageFormatProperties2 {
            pNext = null,
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_FORMAT_PROPERTIES_2,
        };

        return vkGetPhysicalDeviceImageFormatProperties2(
            physicalDevice: physicalDevice,
            pImageFormatInfo: &deviceImageFormatInfo,
            pImageFormatProperties: &imageFormatProperties
        );
    }
    private unsafe static VkResult IsSurfaceFormatSupported(
        VkPhysicalDevice physicalDevice,
        SafeVulkanSurfaceHandle surfaceHandle,
        VkSwapchainCreateInfoKHR swapchainCreateInfo
    ) {
        var addRefCountSuccess = false;

        try {
            surfaceHandle.DangerousAddRef(success: ref addRefCountSuccess);

            var formatCount = uint.MinValue;
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

            for (var i = uint.MinValue; (i < formatCount); ++i) {
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

            var presentModeCount = uint.MinValue;
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

            for (var i = uint.MinValue; (i < presentModeCount); ++i) {
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
        VkSwapchainCreateInfoKHR swapchainCreateInfo,
        nint pAllocator = default
    ) {
        var addRefCountSuccess = false;
        var imageCount = uint.MinValue;

        logicalDeviceHandle.DangerousAddRef(success: ref addRefCountSuccess);

        if (addRefCountSuccess) {
            VkSwapchainKHR swapChain;

            if ((VkResult.VK_SUCCESS == IsImageFormatSupported(
                physicalDevice: physicalDevice,
                swapchainCreateInfo: swapchainCreateInfo
            )) && (VkResult.VK_SUCCESS == IsSurfaceFormatSupported(
                physicalDevice: physicalDevice,
                surfaceHandle: surfaceHandle,
                swapchainCreateInfo: swapchainCreateInfo
            )) && (VkResult.VK_SUCCESS == IsSurfacePresentModeSupported(
                physicalDevice: physicalDevice,
                surfaceHandle: surfaceHandle,
                swapchainCreateInfo: swapchainCreateInfo
            )) && (VkResult.VK_SUCCESS == vkCreateSwapchainKHR(
                device: ((VkDevice)logicalDeviceHandle.DangerousGetHandle()),
                pAllocator: ((VkAllocationCallbacks*)pAllocator),
                pCreateInfo: &swapchainCreateInfo,
                pSwapchain: &swapChain
            )) && (VkResult.VK_SUCCESS == vkGetSwapchainImagesKHR(
                device: ((VkDevice)logicalDeviceHandle.DangerousGetHandle()),
                swapchain: swapChain,
                pSwapchainImageCount: &imageCount,
                pSwapchainImages: null
            ))) {
                var imagesHandle = SafeUnmanagedMemoryHandle.Create(size: (imageCount * ((uint)sizeof(VkImage))));
                var swapchainHandle = new SafeVulkanSwapchainHandle(
                    deviceHandle: logicalDeviceHandle,
                    imageCount: imageCount,
                    imagesHandle: imagesHandle,
                    pAllocator: pAllocator
                );

                if (VkResult.VK_SUCCESS == vkGetSwapchainImagesKHR(
                    device: ((VkDevice)logicalDeviceHandle.DangerousGetHandle()),
                    swapchain: swapChain,
                    pSwapchainImageCount: &imageCount,
                    pSwapchainImages: ((VkImage*)imagesHandle.DangerousGetHandle())
                )) {
                    swapchainHandle.SetHandle(handle: ((nint)swapChain));

                    return swapchainHandle;
                }
            }

            logicalDeviceHandle.DangerousRelease();
        }

        return new(
            deviceHandle: logicalDeviceHandle,
            imageCount: uint.MinValue,
            imagesHandle: SafeUnmanagedMemoryHandle.Create(size: nuint.MinValue),
            pAllocator: pAllocator
        );
    }

    private readonly SafeVulkanDeviceHandle m_deviceHandle;
    private readonly uint m_imageCount;
    private readonly SafeUnmanagedMemoryHandle m_imagesHandle;
    private readonly nint m_pAllocator;

    private SafeVulkanSwapchainHandle(
        SafeVulkanDeviceHandle deviceHandle,
        uint imageCount,
        SafeUnmanagedMemoryHandle imagesHandle,
        nint pAllocator
    ) : base(ownsHandle: true) {
        m_deviceHandle = deviceHandle;
        m_imageCount = imageCount;
        m_imagesHandle = imagesHandle;
        m_pAllocator = pAllocator;
    }

    protected unsafe override bool ReleaseHandle() {
        vkDestroySwapchainKHR(
            device: ((VkDevice)m_deviceHandle.DangerousGetHandle()),
            pAllocator: ((VkAllocationCallbacks*)m_pAllocator),
            swapchain: ((VkSwapchainKHR)handle)
        );
        m_imagesHandle.DangerousRelease();
        m_deviceHandle.DangerousRelease();

        return true;
    }

    public unsafe VkImage GetImage(uint index) =>
        ((index < m_imageCount) ? ((VkImage*)m_imagesHandle.DangerousGetHandle())[index] : default);
}
