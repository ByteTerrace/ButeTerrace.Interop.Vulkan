using HelloTriangle;
using System.Diagnostics;
using System.Text;
using TerraFX.Interop.Vulkan;
using Windows.Win32.UI.WindowsAndMessaging;
using static TerraFX.Interop.Vulkan.Vulkan;

var hInstance = Process.GetCurrentProcess().MainModule!.BaseAddress;

using var vulkanInstanceHandle = SafeVulkanInstanceHandle.Create(
    apiVersion: VK_API_VERSION_1_3,
    applicationName: "BYTRCA",
    applicationVersion: VK_MAKE_VERSION(
        major: 0U,
        minor: 0U,
        patch: 0U
    ),
    engineName: "BYTRCE",
    engineVersion: VK_MAKE_VERSION(
        major: 0U,
        minor: 0U,
        patch: 0U
    ),
    requestedExtensionNames: [
        "VK_KHR_surface",
        "VK_KHR_win32_surface",
    ],
    requestedLayerNames: [
        "VK_LAYER_KHRONOS_validation",
        "VK_LAYER_LUNARG_api_dump",
    ]
);
using var windowClassNameHandle = SafeUnmanagedMemoryHandle.Create(
    encoding: Encoding.Unicode,
    value: "BYTRCWC"
);
using var win32WindowClassHandle = SafeWin32WindowClassHandle.Create(
    classNameHandle: windowClassNameHandle,
    hInstance: hInstance
);
using var win32WindowHandle = SafeWin32WindowHandle.Create(
    extendedStyle: WINDOW_EX_STYLE.WS_EX_LEFT,
    height: 600,
    hInstance: hInstance,
    style: WINDOW_STYLE.WS_OVERLAPPED,
    width: 800,
    windowName: "BYTRCW",
    win32WindowClassHandle: win32WindowClassHandle,
    x: 0,
    y: 0
);

unsafe {
    using var vulkanSurfaceHandle = SafeVulkanSurfaceHandle.Create(
        surfaceCreateInfo: new VkWin32SurfaceCreateInfoKHR {
            flags = 0U,
            hinstance = ((void*)hInstance),
            hwnd = ((void*)win32WindowHandle.DangerousGetHandle()),
            pNext = null,
            sType = VkStructureType.VK_STRUCTURE_TYPE_WIN32_SURFACE_CREATE_INFO_KHR,
        },
        instanceHandle: vulkanInstanceHandle
    );

    var vulkanPhysicalDevice = vulkanInstanceHandle.GetDefaultPhysicalGraphicsDeviceQueue(
        queueFamilyIndex: out var vulkanPhysicalGraphicsDeviceQueueFamilyIndex,
        surfaceHandle: vulkanSurfaceHandle
    );

    using var vulkanLogicalGraphicsDeviceHandle = SafeVulkanInstanceHandle.GetDefaultLogicalGraphicsDeviceQueue(
        physicalDevice: vulkanPhysicalDevice,
        queueFamilyIndex: vulkanPhysicalGraphicsDeviceQueueFamilyIndex,
        queue: out var vulkanLogicalDeviceQueue
    );

    VkSurfaceCapabilitiesKHR vulkanSurfaceCapabilities;

    vkGetPhysicalDeviceSurfaceCapabilitiesKHR(
        physicalDevice: vulkanPhysicalDevice,
        pSurfaceCapabilities: &vulkanSurfaceCapabilities,
        surface: ((VkSurfaceKHR)vulkanSurfaceHandle.DangerousGetHandle())
    );

    using var vulkanSwapchainHandle = SafeVulkanSwapchainHandle.Create(
        logicalDeviceHandle: vulkanLogicalGraphicsDeviceHandle,
        physicalDevice: vulkanPhysicalDevice,
        surfaceHandle: vulkanSurfaceHandle,
        swapchainCreateInfo: new VkSwapchainCreateInfoKHR {
            clipped = VK_TRUE,
            compositeAlpha = VkCompositeAlphaFlagsKHR.VK_COMPOSITE_ALPHA_OPAQUE_BIT_KHR,
            flags = 0U,
            imageArrayLayers = 1U,
            imageColorSpace = VkColorSpaceKHR.VK_COLOR_SPACE_SRGB_NONLINEAR_KHR,
            imageExtent = vulkanSurfaceCapabilities.maxImageExtent,
            imageFormat = VkFormat.VK_FORMAT_B8G8R8A8_SRGB,
            imageSharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE,
            imageUsage = VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT,
            minImageCount = vulkanSurfaceCapabilities.minImageCount,
            oldSwapchain = VK_NULL_HANDLE,
            pNext = null,
            pQueueFamilyIndices = null,
            presentMode = VkPresentModeKHR.VK_PRESENT_MODE_IMMEDIATE_KHR,
            preTransform = vulkanSurfaceCapabilities.currentTransform,
            queueFamilyIndexCount = 0U,
            sType = VkStructureType.VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR,
            surface = ((VkSurfaceKHR)vulkanSurfaceHandle.DangerousGetHandle()),
        }
    );

    Console.WriteLine("Hello Triangle!");
}
