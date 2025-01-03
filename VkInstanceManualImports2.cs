using TerraFX.Interop.Vulkan;

namespace HelloTriangle;

public unsafe partial struct VkInstanceManualImports2
{
    public delegate* unmanaged<VkPhysicalDevice, uint, void*, VkBool32> vkGetPhysicalDeviceWaylandPresentationSupportKHR;
    public delegate* unmanaged<VkPhysicalDevice, uint, VkBool32> vkGetPhysicalDeviceWin32PresentationSupportKHR;
}
