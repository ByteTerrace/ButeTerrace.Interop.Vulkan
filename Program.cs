using HelloTriangle;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.Vulkan;
using Windows.Win32.UI.WindowsAndMessaging;
using static TerraFX.Interop.Mimalloc.Mimalloc;
using static TerraFX.Interop.Vulkan.Vulkan;

// setup custom memory allocator
VkAllocationCallbacks allocationCallbacks; 
nint pAllocator;

unsafe {
#if DEBUG
    mi_register_deferred_free(
        arg: null,
        fn: (bool force, ulong heartbeat, void* _) => {
            Console.WriteLine(value: $"mimalloc heartbeat: {heartbeat}");
            mi_collect(force: force);
        }
    );
#endif

    [UnmanagedCallersOnly]
    static void* PfnAllocation(void* pUserData, nuint size, nuint alignment, VkSystemAllocationScope allocationScope) =>
        mi_malloc_aligned(alignment: alignment, size: size);
    [UnmanagedCallersOnly]
    static void PfnFree(void* pUserData, void* pMemory) =>
        mi_free(p: pMemory);
    [UnmanagedCallersOnly]
    static void* PfnReallocation(void* pUserData, void* pOriginal, nuint size, nuint alignment, VkSystemAllocationScope allocationScope) =>
        mi_realloc_aligned(alignment: alignment, newsize: size, p: pOriginal);

    allocationCallbacks = new VkAllocationCallbacks {
        pfnAllocation = &PfnAllocation,
        pfnFree = &PfnFree,
        pfnReallocation = &PfnReallocation,
    };
    pAllocator = ((nint)(&allocationCallbacks));
}

// create Vulkan instance
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
    pAllocator: pAllocator,
    requestedExtensionNames: [
        "VK_EXT_direct_mode_display",
        "VK_EXT_headless_surface",
        "VK_EXT_surface_maintenance1",
        "VK_EXT_swapchain_colorspace",
        "VK_KHR_android_surface",
        "VK_KHR_display",
        "VK_KHR_get_surface_capabilities2",
        "VK_KHR_surface",
        "VK_KHR_wayland_surface",
        "VK_KHR_win32_surface",
        "VK_NN_vi_surface",
    ],
    requestedLayerNames: [
#if DEBUG
        "VK_LAYER_KHRONOS_profiles",
        "VK_LAYER_KHRONOS_shader_object",
        "VK_LAYER_KHRONOS_validation",
        "VK_LAYER_LUNARG_api_dump",
        "VK_LAYER_LUNARG_screenshot",
#endif
    ]
);

// create Vulkan logical device
var vulkanPhysicalDevice = vulkanInstanceHandle.GetDefaultPhysicalGraphicsDeviceQueue(
    queueFamilyIndex: out var vulkanPhysicalGraphicsDeviceQueueFamilyIndex,
    requestedDeviceType: VkPhysicalDeviceType.VK_PHYSICAL_DEVICE_TYPE_DISCRETE_GPU
);

using var vulkanLogicalGraphicsDeviceHandle = SafeVulkanInstanceHandle.GetDefaultLogicalGraphicsDeviceQueue(
    pAllocator: pAllocator,
    physicalDevice: vulkanPhysicalDevice,
    queue: out var vulkanLogicalDeviceQueue,
    queueFamilyIndex: vulkanPhysicalGraphicsDeviceQueueFamilyIndex,
    requestedExtensionNames: [
#if DEBUG
        "VK_KHR_portability_subset",
#endif
        "VK_KHR_swapchain",
    ]
);

// create Win32 window and Vulkan surface
var hInstance = Process.GetCurrentProcess().MainModule!.BaseAddress;

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
using var vulkanSurfaceHandle = SafeVulkanSurfaceHandle.Create(
    pAllocator: pAllocator,
    physicalDevice: vulkanPhysicalDevice,
    queueFamilyIndex: vulkanPhysicalGraphicsDeviceQueueFamilyIndex,
    vulkanInstanceHandle: vulkanInstanceHandle,
    win32InstanceHandle: hInstance,
    win32WindowHandle: win32WindowHandle
);

// create Vulkan swapchain
VkSurfaceCapabilitiesKHR vulkanSurfaceCapabilities;

unsafe {
    vkGetPhysicalDeviceSurfaceCapabilitiesKHR(
        physicalDevice: vulkanPhysicalDevice,
        pSurfaceCapabilities: &vulkanSurfaceCapabilities,
        surface: ((VkSurfaceKHR)vulkanSurfaceHandle.DangerousGetHandle())
    );
}

using var vulkanSwapchainHandle = SafeVulkanSwapchainHandle.Create(
    logicalDeviceHandle: vulkanLogicalGraphicsDeviceHandle,
    pAllocator: pAllocator,
    physicalDevice: vulkanPhysicalDevice,
    surfaceHandle: vulkanSurfaceHandle,
    swapchainCreateInfo: new VkSwapchainCreateInfoKHR {
        clipped = VK_TRUE,
        compositeAlpha = VkCompositeAlphaFlagsKHR.VK_COMPOSITE_ALPHA_OPAQUE_BIT_KHR,
        flags = uint.MinValue,
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
        queueFamilyIndexCount = uint.MinValue,
        sType = VkStructureType.VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR,
        surface = ((VkSurfaceKHR)vulkanSurfaceHandle.DangerousGetHandle()),
    }
);

Console.WriteLine("Hello Triangle!");
