using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace HelloTriangle;

public sealed class SafeVulkanInstanceHandle(VkInstanceManualImports vkInstanceManualImports) : SafeHandleZeroOrMinusOneIsInvalid(ownsHandle: true)
{
    private unsafe static SafeUnmanagedMemoryHandle GetEnabledExtensionNames(
        out uint enabledExtensionCount,
        HashSet<string> requestedNames,
        SafeUnmanagedMemoryHandle supportedPropertiesHandle,
        nuint supportedPropertyCount
    ) {
        var destinationHandle = SafeUnmanagedMemoryHandle.Create(size: ((nuint)requestedNames.Count));
        var destinationIndex = uint.MinValue;
        var destinationPointer = ((sbyte**)destinationHandle.DangerousGetHandle());
        var sourcePointer = supportedPropertiesHandle.DangerousGetHandle();
        var uniqueNames = new HashSet<string>();

        enabledExtensionCount = uint.MinValue;

        for (var i = nuint.MinValue; (i < supportedPropertyCount); ++i) {
            var name = Encoding.UTF8.GetString(bytes: MemoryMarshal.CreateReadOnlySpanFromNullTerminated(value: ((byte*)sourcePointer)));

            if (requestedNames.Contains(item: name) && uniqueNames.Add(item: name)) {
                destinationPointer[destinationIndex++] = ((sbyte*)sourcePointer);
                ++enabledExtensionCount;
            }

            sourcePointer += sizeof(VkExtensionProperties);
        }

        return destinationHandle;
    }
    private unsafe static SafeUnmanagedMemoryHandle GetEnabledLayerNames(
        out uint enabledLayerCount,
        HashSet<string> requestedNames,
        SafeUnmanagedMemoryHandle supportedPropertiesHandle,
        nuint supportedPropertyCount
    ) {
        var destinationHandle = SafeUnmanagedMemoryHandle.Create(size: ((nuint)requestedNames.Count));
        var destinationIndex = uint.MinValue;
        var destinationPointer = ((sbyte**)destinationHandle.DangerousGetHandle());
        var sourcePointer = supportedPropertiesHandle.DangerousGetHandle();
        var uniqueNames = new HashSet<string>();

        enabledLayerCount = uint.MinValue;

        for (var i = nuint.MinValue; (i < supportedPropertyCount); ++i) {
            var name = Encoding.UTF8.GetString(bytes: MemoryMarshal.CreateReadOnlySpanFromNullTerminated(value: ((byte*)sourcePointer)));

            if (requestedNames.Contains(item: name) && uniqueNames.Add(item: name)) {
                destinationPointer[destinationIndex++] = ((sbyte*)sourcePointer);
                ++enabledLayerCount;
            }

            sourcePointer += sizeof(VkLayerProperties);
        }

        return destinationHandle;
    }
    private unsafe static SafeUnmanagedMemoryHandle GetPhysicalDeviceQueueFamilyProperties(VkPhysicalDevice physicalDevice, out uint count) {
        var vkPhysicalDeviceQueueFamilyPropertyCount = 0U;

        vkGetPhysicalDeviceQueueFamilyProperties(
            physicalDevice: physicalDevice,
            pQueueFamilyProperties: null,
            pQueueFamilyPropertyCount: &vkPhysicalDeviceQueueFamilyPropertyCount
        );

        var physicalDeviceQueueFamilyPropertiesHandle = SafeUnmanagedMemoryHandle.Create(size: (vkPhysicalDeviceQueueFamilyPropertyCount * ((uint)sizeof(VkQueueFamilyProperties))));

        vkGetPhysicalDeviceQueueFamilyProperties(
            physicalDevice: physicalDevice,
            pQueueFamilyProperties: ((VkQueueFamilyProperties*)physicalDeviceQueueFamilyPropertiesHandle.DangerousGetHandle()),
            pQueueFamilyPropertyCount: &vkPhysicalDeviceQueueFamilyPropertyCount
        );

        count = vkPhysicalDeviceQueueFamilyPropertyCount;

        return physicalDeviceQueueFamilyPropertiesHandle;
    }
    private unsafe static SafeUnmanagedMemoryHandle GetSupportedExtensionProperties(out uint count) {
        count = uint.MinValue;

        var propertyCount = uint.MinValue;

        if (VkResult.VK_SUCCESS == vkEnumerateInstanceExtensionProperties(
            pLayerName: null,
            pProperties: null,
            pPropertyCount: &propertyCount
        )) {
            var vkInstanceExtensionPropertiesHandle = SafeUnmanagedMemoryHandle.Create(size: (propertyCount * ((uint)sizeof(VkExtensionProperties))));

            if (VkResult.VK_SUCCESS == vkEnumerateInstanceExtensionProperties(
                pLayerName: default,
                pProperties: ((VkExtensionProperties*)vkInstanceExtensionPropertiesHandle.DangerousGetHandle()),
                pPropertyCount: &propertyCount
            )) {
                count = propertyCount;

                return vkInstanceExtensionPropertiesHandle;
            }
        }

        return SafeUnmanagedMemoryHandle.Create(size: 0);
    }
    private unsafe static SafeUnmanagedMemoryHandle GetSupportedLayerProperties(out uint count) {
        count = uint.MinValue;

        var propertyCount = uint.MinValue;

        if (VkResult.VK_SUCCESS == vkEnumerateInstanceLayerProperties(
            pProperties: null,
            pPropertyCount: &propertyCount
        )) {
            var vkInstanceLayerPropertiesHandle = SafeUnmanagedMemoryHandle.Create(size: (propertyCount * ((uint)sizeof(VkLayerProperties))));

            if (VkResult.VK_SUCCESS == vkEnumerateInstanceLayerProperties(
                pProperties: ((VkLayerProperties*)vkInstanceLayerPropertiesHandle.DangerousGetHandle()),
                pPropertyCount: &propertyCount
            )) {
                count = propertyCount;

                return vkInstanceLayerPropertiesHandle;
            }
        }

        return SafeUnmanagedMemoryHandle.Create(size: 0);
    }

    public unsafe static SafeVulkanDeviceHandle GetDefaultLogicalGraphicsDeviceQueue(
        VkPhysicalDevice physicalDevice,
        uint queueFamilyIndex,
        out VkQueue queue
    ) {
        using var enabledExtensionNames = SafeUnmanagedMemoryHandle.Create(size: 1);

        ((sbyte**)enabledExtensionNames.DangerousGetHandle())[0] = ((sbyte*)Unsafe.AsPointer(value: ref MemoryMarshal.GetReference(span: "VK_KHR_swapchain\u0000"u8)));

        var physicalDeviceEnabledFeatures = new VkPhysicalDeviceFeatures { };
        var logicalDeviceQueuePriorities = 1.0f;
        var logicalDeviceQueueCreateInfo = new VkDeviceQueueCreateInfo {
            flags = 0U,
            pNext = null,
            pQueuePriorities = &logicalDeviceQueuePriorities,
            queueCount = 1U,
            queueFamilyIndex = queueFamilyIndex,
            sType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO,
        };
        var logicalDeviceHandle = SafeVulkanDeviceHandle.Create(
            deviceCreateInfo: new VkDeviceCreateInfo {
                enabledExtensionCount = 1U,
                flags = 0U,
                pEnabledFeatures = &physicalDeviceEnabledFeatures,
                pNext = null,
                pQueueCreateInfos = &logicalDeviceQueueCreateInfo,
                ppEnabledExtensionNames = ((sbyte**)enabledExtensionNames.DangerousGetHandle()),
                queueCreateInfoCount = 1U,
                sType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO,
            },
            physicalDevice: physicalDevice
        );

        VkQueue logicalDeviceQueue;

        vkGetDeviceQueue(
            device: ((VkDevice)logicalDeviceHandle.DangerousGetHandle()),
            queueFamilyIndex: queueFamilyIndex,
            queueIndex: 0U,
            pQueue: &logicalDeviceQueue
        );

        queue = logicalDeviceQueue;

        return logicalDeviceHandle;
    }

    public unsafe static SafeVulkanInstanceHandle Create(
        uint apiVersion,
        string applicationName,
        uint applicationVersion,
        string engineName,
        uint engineVersion,
        HashSet<string> requestedExtensionNames,
        HashSet<string> requestedLayerNames
    ) {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static sbyte* DangerousGetPointer(ReadOnlySpan<byte> span) =>
            ((sbyte*)Unsafe.AsPointer(value: ref MemoryMarshal.GetReference(span: span)));

        using var applicationNameSafeHandle = SafeUnmanagedMemoryHandle.Create(encoding: Encoding.UTF8, value: applicationName);
        using var engineNameSafeHandle = SafeUnmanagedMemoryHandle.Create(encoding: Encoding.UTF8, value: engineName);
        using var supportedExtensionPropertiesSafeHandle = GetSupportedExtensionProperties(count: out var supportedExtensionPropertyCount);
        using var supportedLayerPropertiesSafeHandle = GetSupportedLayerProperties(count: out var supportedLayerPropertyCount);
        using var enabledExtensionNamesSafeHandle = GetEnabledExtensionNames(
            enabledExtensionCount: out var enabledExtensionCount,
            requestedNames: requestedExtensionNames,
            supportedPropertiesHandle: supportedExtensionPropertiesSafeHandle,
            supportedPropertyCount: supportedExtensionPropertyCount
        );
        using var enabledLayerNamesSafeHandle = GetEnabledLayerNames(
            enabledLayerCount: out var enabledLayerCount,
            requestedNames: requestedLayerNames,
            supportedPropertiesHandle: supportedLayerPropertiesSafeHandle,
            supportedPropertyCount: supportedLayerPropertyCount
        );

        var vkApplicationInfo = new VkApplicationInfo {
            apiVersion = apiVersion,
            applicationVersion = applicationVersion,
            engineVersion = engineVersion,
            pApplicationName = ((sbyte*)applicationNameSafeHandle.DangerousGetHandle()),
            pEngineName = ((sbyte*)engineNameSafeHandle.DangerousGetHandle()),
            pNext = null,
            sType = VkStructureType.VK_STRUCTURE_TYPE_APPLICATION_INFO,
        };
        var vkInstanceCreateInfo = new VkInstanceCreateInfo {
            enabledExtensionCount = enabledExtensionCount,
            enabledLayerCount = enabledLayerCount,
            flags = 0,
            pApplicationInfo = &vkApplicationInfo,
            pNext = null,
            ppEnabledExtensionNames = ((sbyte**)enabledExtensionNamesSafeHandle.DangerousGetHandle()),
            ppEnabledLayerNames = ((sbyte**)enabledLayerNamesSafeHandle.DangerousGetHandle()),
            sType = VkStructureType.VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO,
        };

        VkInstance vkInstance;

        if (VkResult.VK_SUCCESS == vkCreateInstance(
            pAllocator: null,
            pCreateInfo: &vkInstanceCreateInfo,
            pInstance: &vkInstance
        )) {
            var vkInstanceSafeHandle = new SafeVulkanInstanceHandle(vkInstanceManualImports: new() {
                vkCreateAndroidSurfaceKHR = ((delegate* unmanaged<VkInstance, VkAndroidSurfaceCreateInfoKHR*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult>)vkGetInstanceProcAddr(
                    instance: vkInstance,
                    pName: DangerousGetPointer(span: "vkCreateAndroidSurfaceKHR\u0000"u8)
                )),
                vkCreateWaylandSurfaceKHR = ((delegate* unmanaged<VkInstance, VkWaylandSurfaceCreateInfoKHR*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult>)vkGetInstanceProcAddr(
                    instance: vkInstance,
                    pName: DangerousGetPointer(span: "vkCreateWaylandSurfaceKHR\u0000"u8)
                )),
                vkCreateWin32SurfaceKHR = ((delegate* unmanaged<VkInstance, VkWin32SurfaceCreateInfoKHR*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult>)vkGetInstanceProcAddr(
                    instance: vkInstance,
                    pName: DangerousGetPointer(span: "vkCreateWin32SurfaceKHR\u0000"u8)
                ))
            });

            vkInstanceSafeHandle.SetHandle(handle: vkInstance);

            return vkInstanceSafeHandle;
        }

        return new SafeVulkanInstanceHandle(vkInstanceManualImports: default);
    }

    protected unsafe override bool ReleaseHandle() {
        vkDestroyInstance(
            instance: ((VkInstance)handle),
            pAllocator: null
        );

        return true;
    }

    public unsafe VkSurfaceKHR CreateSurfaceKhr<T>(T surfaceCreateInfo) where T : struct {
        var needsRelease = false;
        var surface = default(VkSurfaceKHR);

        try {
            DangerousAddRef(success: ref needsRelease);

            _ = surfaceCreateInfo switch {
                VkAndroidSurfaceCreateInfoKHR androidSurfaceCreateInfoKhr => vkInstanceManualImports.vkCreateAndroidSurfaceKHR(
                    ((VkInstance)handle),
                    &androidSurfaceCreateInfoKhr,
                    null,
                    &surface
                ),
                VkWaylandSurfaceCreateInfoKHR waylandSurfaceCreateInfoKhr => vkInstanceManualImports.vkCreateWaylandSurfaceKHR(
                    ((VkInstance)handle),
                    &waylandSurfaceCreateInfoKhr,
                    null,
                    &surface
                ),
                VkWin32SurfaceCreateInfoKHR win32SurfaceCreateInfoKhr => vkInstanceManualImports.vkCreateWin32SurfaceKHR(
                    ((VkInstance)handle),
                    &win32SurfaceCreateInfoKhr,
                    null,
                    &surface
                ),
                _ => VkResult.VK_ERROR_EXTENSION_NOT_PRESENT,
            };
        }
        finally {
            if (needsRelease) {
                DangerousRelease();
            }
        }

        return surface;
    }
    public unsafe VkPhysicalDevice GetDefaultPhysicalGraphicsDeviceQueue(
        SafeVulkanSurfaceHandle surfaceHandle,
        out uint queueFamilyIndex
    ) {
        using var vulkanPhysicalDevices = GetPhysicalDevices(out var physicalDeviceCount);

        var physicalDevicesPointer = ((VkPhysicalDevice*)vulkanPhysicalDevices.DangerousGetHandle());

        VkPhysicalDevice physicalDevice;
        VkPhysicalDeviceProperties physicalDeviceProperties;

        for (var i = uint.MinValue; (i < physicalDeviceCount); ++i) {
            physicalDevice = physicalDevicesPointer[i];

            vkGetPhysicalDeviceProperties(
                physicalDevice: physicalDevice,
                pProperties: &physicalDeviceProperties
            );

            // select the first available discrete GPU; not ideal, but *should* work for the vast majority of systems that aren't virtualized
            if (physicalDeviceProperties.deviceType.HasFlag(flag: VkPhysicalDeviceType.VK_PHYSICAL_DEVICE_TYPE_DISCRETE_GPU)) {
                using var vulkanPhysicalDeviceQueueFamilyPropertiesHandle = GetPhysicalDeviceQueueFamilyProperties(
                    count: out var vkPhysicalDeviceQueueFamilyPropertyCount,
                    physicalDevice: physicalDevice
                );

                var vulkanPhysicalDeviceQueueFamilyPropertiesPointer = ((VkQueueFamilyProperties*)vulkanPhysicalDeviceQueueFamilyPropertiesHandle.DangerousGetHandle());

                for (var j = uint.MinValue; (j < vkPhysicalDeviceQueueFamilyPropertyCount); ++j) {
                    var vulkanPhysicalDeviceQueueFamilyProperties = vulkanPhysicalDeviceQueueFamilyPropertiesPointer[j];

                    // select the first available graphics queue; not ideal, but *should* work for the average gaming rig
                    if (vulkanPhysicalDeviceQueueFamilyProperties.queueFlags.HasFlag(flag: VkQueueFlags.VK_QUEUE_GRAPHICS_BIT) && surfaceHandle.IsPhysicalDeviceSupported(
                        physicalDevice: physicalDevice,
                        queueFamilyIndex: j
                    )) {
                        queueFamilyIndex = j;

                        return physicalDevice;
                    }
                }

                break;
            }
        }

        queueFamilyIndex = uint.MinValue;

        return default;
    }
    public unsafe SafeUnmanagedMemoryHandle GetPhysicalDevices(out uint count) {
        count = uint.MinValue;

        var needsRelease = false;

        try {
            DangerousAddRef(success: ref needsRelease);

            var deviceCount = uint.MinValue;

            if (VkResult.VK_SUCCESS == vkEnumeratePhysicalDevices(
                instance: ((VkInstance)handle),
                pPhysicalDeviceCount: &deviceCount,
                pPhysicalDevices: null
            )) {
                var physicalDevicesHandle = SafeUnmanagedMemoryHandle.Create(size: (deviceCount * ((uint)sizeof(VkPhysicalDevice))));

                if (VkResult.VK_SUCCESS == vkEnumeratePhysicalDevices(
                    instance: ((VkInstance)handle),
                    pPhysicalDeviceCount: &deviceCount,
                    pPhysicalDevices: ((VkPhysicalDevice*)physicalDevicesHandle.DangerousGetHandle())
                )) {
                    count = deviceCount;

                    return physicalDevicesHandle;
                }
            }
        }
        finally {
            if (needsRelease) {
                DangerousRelease();
            }
        }

        return SafeUnmanagedMemoryHandle.Create(size: 0);
    }
}
