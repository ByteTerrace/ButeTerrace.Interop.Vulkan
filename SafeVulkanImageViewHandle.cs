using Microsoft.Win32.SafeHandles;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace HelloTriangle;

public sealed class SafeVulkanImageViewHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public unsafe static SafeVulkanImageViewHandle Create(
        VkImageViewCreateInfo imageViewCreateInfo,
        SafeVulkanDeviceHandle logicalDeviceHandle,
        nint pAllocator = default
    ) {
        var addRefCountSuccess = false;
        var imageViewHandle = new SafeVulkanImageViewHandle(
            deviceHandle: logicalDeviceHandle,
            pAllocator: pAllocator
        );

        logicalDeviceHandle.DangerousAddRef(success: ref addRefCountSuccess);

        if (addRefCountSuccess) {
            VkImageView imageView;

            if (VkResult.VK_SUCCESS == vkCreateImageView(
                device: ((VkDevice)logicalDeviceHandle.DangerousGetHandle()),
                pAllocator: ((VkAllocationCallbacks*)pAllocator),
                pCreateInfo: &imageViewCreateInfo,
                pView: &imageView
            )) {
                imageViewHandle.SetHandle(handle: ((nint)imageView));
            }
            else {
                logicalDeviceHandle.DangerousRelease();
            }
        }

        return imageViewHandle;
    }

    private readonly SafeVulkanDeviceHandle m_deviceHandle;
    private readonly nint m_pAllocator;

    private SafeVulkanImageViewHandle(
        SafeVulkanDeviceHandle deviceHandle,
        nint pAllocator
    ) : base(ownsHandle: true) {
        m_deviceHandle = deviceHandle;
        m_pAllocator = pAllocator;
    }

    protected unsafe override bool ReleaseHandle() {
        vkDestroyImageView(
            device: ((VkDevice)m_deviceHandle.DangerousGetHandle()),
            imageView: ((VkImageView)handle),
            pAllocator: ((VkAllocationCallbacks*)m_pAllocator)
        );
        m_deviceHandle.DangerousRelease();

        return true;
    }
}
