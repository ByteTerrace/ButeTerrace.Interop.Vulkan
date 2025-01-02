using Microsoft.Win32.SafeHandles;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace HelloTriangle;

public sealed class SafeVulkanDeviceHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public unsafe static SafeVulkanDeviceHandle Create(
        VkDeviceCreateInfo deviceCreateInfo,
        VkPhysicalDevice physicalDevice
    ) {
        var deviceHandle = new SafeVulkanDeviceHandle();

        VkDevice device;

        if (VkResult.VK_SUCCESS == vkCreateDevice(
            pAllocator: null,
            pCreateInfo: &deviceCreateInfo,
            pDevice: &device,
            physicalDevice: physicalDevice
        )) {
            deviceHandle.SetHandle(handle: device);
        }

        return deviceHandle;
    }

    public SafeVulkanDeviceHandle() : base(ownsHandle: true) { }

    protected unsafe override bool ReleaseHandle() {
        vkDestroyDevice(
            device: ((VkDevice)handle),
            pAllocator: null
        );

        return true;
    }
}
