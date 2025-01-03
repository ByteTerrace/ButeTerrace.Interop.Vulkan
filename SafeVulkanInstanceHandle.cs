using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace HelloTriangle;

public sealed class SafeVulkanInstanceHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private unsafe static SafeUnmanagedMemoryHandle GetEnabledNames<T>(
        out uint enabledPropertyCount,
        HashSet<string> requestedNames,
        SafeUnmanagedMemoryHandle supportedPropertiesHandle,
        nuint supportedPropertyCount
    ) where T : unmanaged {
        var destinationHandle = SafeUnmanagedMemoryHandle.Create(size: ((nuint)requestedNames.Count));
        var destinationIndex = uint.MinValue;
        var destinationPointer = ((sbyte**)destinationHandle.DangerousGetHandle());
        var sourcePointer = supportedPropertiesHandle.DangerousGetHandle();
        var uniqueNames = new HashSet<string>();

        enabledPropertyCount = uint.MinValue;

        for (var i = nuint.MinValue; (i < supportedPropertyCount); ++i) {
            var name = Encoding.UTF8.GetString(bytes: MemoryMarshal.CreateReadOnlySpanFromNullTerminated(value: ((byte*)sourcePointer)));

            if (requestedNames.Contains(item: name) && uniqueNames.Add(item: name)) {
                destinationPointer[destinationIndex++] = ((sbyte*)sourcePointer);
                ++enabledPropertyCount;
            }

            sourcePointer += sizeof(T);
        }

        return destinationHandle;
    }
    private unsafe static SafeUnmanagedMemoryHandle GetPhysicalDeviceQueueFamilyProperties(
        VkPhysicalDevice physicalDevice,
        out uint count
    ) {
        var queueFamilyPropertyCount = uint.MinValue;

        vkGetPhysicalDeviceQueueFamilyProperties(
            physicalDevice: physicalDevice,
            pQueueFamilyProperties: null,
            pQueueFamilyPropertyCount: &queueFamilyPropertyCount
        );

        var queueFamilyPropertiesHandle = SafeUnmanagedMemoryHandle.Create(size: (queueFamilyPropertyCount * ((uint)sizeof(VkQueueFamilyProperties))));

        vkGetPhysicalDeviceQueueFamilyProperties(
            physicalDevice: physicalDevice,
            pQueueFamilyProperties: ((VkQueueFamilyProperties*)queueFamilyPropertiesHandle.DangerousGetHandle()),
            pQueueFamilyPropertyCount: &queueFamilyPropertyCount
        );

        count = queueFamilyPropertyCount;

        return queueFamilyPropertiesHandle;
    }
    private unsafe static SafeUnmanagedMemoryHandle GetSupportedDeviceExtensionProperties(
        VkPhysicalDevice physicalDevice,
        out uint count
    ) {
        count = uint.MinValue;

        var propertyCount = uint.MinValue;

        if (VkResult.VK_SUCCESS == vkEnumerateDeviceExtensionProperties(
            physicalDevice: physicalDevice,
            pLayerName: null,
            pProperties: null,
            pPropertyCount: &propertyCount
        )) {
            var propertiesHandle = SafeUnmanagedMemoryHandle.Create(size: propertyCount);

            if (VkResult.VK_SUCCESS == vkEnumerateDeviceExtensionProperties(
                physicalDevice: physicalDevice,
                pLayerName: null,
                pProperties: ((VkExtensionProperties*)propertiesHandle.DangerousGetHandle()),
                pPropertyCount: &propertyCount
            )) {
                count = propertyCount;

                return propertiesHandle;
            }
        }

        return SafeUnmanagedMemoryHandle.Create(size: nuint.MinValue);
    }
    private unsafe static SafeUnmanagedMemoryHandle GetSupportedInstanceExtensionProperties(out uint count) {
        count = uint.MinValue;

        var propertyCount = uint.MinValue;

        if (VkResult.VK_SUCCESS == vkEnumerateInstanceExtensionProperties(
            pLayerName: null,
            pProperties: null,
            pPropertyCount: &propertyCount
        )) {
            var propertiesHandle = SafeUnmanagedMemoryHandle.Create(size: (propertyCount * ((uint)sizeof(VkExtensionProperties))));

            if (VkResult.VK_SUCCESS == vkEnumerateInstanceExtensionProperties(
                pLayerName: default,
                pProperties: ((VkExtensionProperties*)propertiesHandle.DangerousGetHandle()),
                pPropertyCount: &propertyCount
            )) {
                count = propertyCount;

                return propertiesHandle;
            }
        }

        return SafeUnmanagedMemoryHandle.Create(size: nuint.MinValue);
    }
    private unsafe static SafeUnmanagedMemoryHandle GetSupportedInstanceLayerProperties(out uint count) {
        count = uint.MinValue;

        var propertyCount = uint.MinValue;

        if (VkResult.VK_SUCCESS == vkEnumerateInstanceLayerProperties(
            pProperties: null,
            pPropertyCount: &propertyCount
        )) {
            var propertiesHandle = SafeUnmanagedMemoryHandle.Create(size: (propertyCount * ((uint)sizeof(VkLayerProperties))));

            if (VkResult.VK_SUCCESS == vkEnumerateInstanceLayerProperties(
                pProperties: ((VkLayerProperties*)propertiesHandle.DangerousGetHandle()),
                pPropertyCount: &propertyCount
            )) {
                count = propertyCount;

                return propertiesHandle;
            }
        }

        return SafeUnmanagedMemoryHandle.Create(size: nuint.MinValue);
    }

    public unsafe static SafeVulkanDeviceHandle GetDefaultLogicalGraphicsDeviceQueue(
        VkPhysicalDevice physicalDevice,
        uint queueFamilyIndex,
        out VkQueue queue,
        nint pAllocator = default
    ) {
        using var supportedExtensionPropertiesHandle = GetSupportedDeviceExtensionProperties(
            count: out var supportedExtensionPropertyCount,
            physicalDevice: physicalDevice
        );
        using var enabledExtensionNamesHandle = GetEnabledNames<VkExtensionProperties>(
            enabledPropertyCount: out var enabledExtensionCount,
            requestedNames: ["VK_KHR_swapchain",],
            supportedPropertiesHandle: supportedExtensionPropertiesHandle,
            supportedPropertyCount: supportedExtensionPropertyCount
        );

        var physicalDeviceEnabledFeatures = new VkPhysicalDeviceFeatures { };
        var logicalDeviceQueuePriorities = 1.0f;
        var logicalDeviceQueueCreateInfo = new VkDeviceQueueCreateInfo {
            flags = uint.MinValue,
            pNext = null,
            pQueuePriorities = &logicalDeviceQueuePriorities,
            queueCount = 1U,
            queueFamilyIndex = queueFamilyIndex,
            sType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO,
        };
        var logicalDeviceHandle = SafeVulkanDeviceHandle.Create(
            deviceCreateInfo: new VkDeviceCreateInfo {
                enabledExtensionCount = enabledExtensionCount,
                enabledLayerCount = uint.MinValue,
                flags = uint.MinValue,
                pEnabledFeatures = &physicalDeviceEnabledFeatures,
                pNext = null,
                pQueueCreateInfos = &logicalDeviceQueueCreateInfo,
                ppEnabledExtensionNames = ((sbyte**)enabledExtensionNamesHandle.DangerousGetHandle()),
                ppEnabledLayerNames = null,
                queueCreateInfoCount = 1U,
                sType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO,
            },
            pAllocator: pAllocator,
            physicalDevice: physicalDevice
        );

        VkQueue logicalDeviceQueue;

        vkGetDeviceQueue(
            device: ((VkDevice)logicalDeviceHandle.DangerousGetHandle()),
            queueFamilyIndex: queueFamilyIndex,
            queueIndex: uint.MinValue,
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
        HashSet<string> requestedLayerNames,
        nint pAllocator = default
    ) {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static sbyte* DangerousGetPointer(ReadOnlySpan<byte> span) =>
            ((sbyte*)Unsafe.AsPointer(value: ref MemoryMarshal.GetReference(span: span)));

        using var applicationNameHandle = SafeUnmanagedMemoryHandle.Create(encoding: Encoding.UTF8, value: applicationName);
        using var engineNameHandle = SafeUnmanagedMemoryHandle.Create(encoding: Encoding.UTF8, value: engineName);
        using var supportedExtensionPropertiesHandle = GetSupportedInstanceExtensionProperties(count: out var supportedExtensionPropertyCount);
        using var supportedLayerPropertiesHandle = GetSupportedInstanceLayerProperties(count: out var supportedLayerPropertyCount);
        using var enabledExtensionNamesHandle = GetEnabledNames<VkExtensionProperties>(
            enabledPropertyCount: out var enabledExtensionCount,
            requestedNames: requestedExtensionNames,
            supportedPropertiesHandle: supportedExtensionPropertiesHandle,
            supportedPropertyCount: supportedExtensionPropertyCount
        );
        using var enabledLayerNamesHandle = GetEnabledNames<VkExtensionProperties>(
            enabledPropertyCount: out var enabledLayerCount,
            requestedNames: requestedLayerNames,
            supportedPropertiesHandle: supportedLayerPropertiesHandle,
            supportedPropertyCount: supportedLayerPropertyCount
        );

        var applicationInfo = new VkApplicationInfo {
            apiVersion = apiVersion,
            applicationVersion = applicationVersion,
            engineVersion = engineVersion,
            pApplicationName = ((sbyte*)applicationNameHandle.DangerousGetHandle()),
            pEngineName = ((sbyte*)engineNameHandle.DangerousGetHandle()),
            pNext = null,
            sType = VkStructureType.VK_STRUCTURE_TYPE_APPLICATION_INFO,
        };
        var instanceCreateInfo = new VkInstanceCreateInfo {
            enabledExtensionCount = enabledExtensionCount,
            enabledLayerCount = enabledLayerCount,
            flags = uint.MinValue,
            pApplicationInfo = &applicationInfo,
            pNext = null,
            ppEnabledExtensionNames = ((sbyte**)enabledExtensionNamesHandle.DangerousGetHandle()),
            ppEnabledLayerNames = ((sbyte**)enabledLayerNamesHandle.DangerousGetHandle()),
            sType = VkStructureType.VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO,
        };

        VkInstance vkInstance;

        if (VkResult.VK_SUCCESS == vkCreateInstance(
            pAllocator: ((VkAllocationCallbacks*)pAllocator),
            pCreateInfo: &instanceCreateInfo,
            pInstance: &vkInstance
        )) {
            var instanceSafeHandle = new SafeVulkanInstanceHandle(
                instanceManualImports: new() {
                    vkCreateAndroidSurfaceKHR = ((delegate* unmanaged<VkInstance, VkAndroidSurfaceCreateInfoKHR*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult>)vkGetInstanceProcAddr(
                        instance: vkInstance,
                        pName: DangerousGetPointer(span: "vkCreateAndroidSurfaceKHR\u0000"u8)
                    )),
                    vkCreateViSurfaceNN = ((delegate* unmanaged<VkInstance, VkViSurfaceCreateInfoNN*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult>)vkGetInstanceProcAddr(
                        instance: vkInstance,
                        pName: DangerousGetPointer(span: "vkCreateViSurfaceNN\u0000"u8)
                    )),
                    vkCreateWaylandSurfaceKHR = ((delegate* unmanaged<VkInstance, VkWaylandSurfaceCreateInfoKHR*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult>)vkGetInstanceProcAddr(
                        instance: vkInstance,
                        pName: DangerousGetPointer(span: "vkCreateWaylandSurfaceKHR\u0000"u8)
                    )),
                    vkCreateWin32SurfaceKHR = ((delegate* unmanaged<VkInstance, VkWin32SurfaceCreateInfoKHR*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult>)vkGetInstanceProcAddr(
                        instance: vkInstance,
                        pName: DangerousGetPointer(span: "vkCreateWin32SurfaceKHR\u0000"u8)
                    ))
                },
                instanceManualImports2: new() {
                    vkGetPhysicalDeviceWaylandPresentationSupportKHR = ((delegate* unmanaged<VkPhysicalDevice, uint, void*, VkBool32>)vkGetInstanceProcAddr(
                        instance: vkInstance,
                        pName: DangerousGetPointer(span: "vkGetPhysicalDeviceWaylandPresentationSupportKHR\u0000"u8)
                    )),
                    vkGetPhysicalDeviceWin32PresentationSupportKHR = ((delegate* unmanaged<VkPhysicalDevice, uint, VkBool32>)vkGetInstanceProcAddr(
                        instance: vkInstance,
                        pName: DangerousGetPointer(span: "vkGetPhysicalDeviceWin32PresentationSupportKHR\u0000"u8)
                    ))
                },
                pAllocator: pAllocator
            );

            instanceSafeHandle.SetHandle(handle: vkInstance);

            return instanceSafeHandle;
        }

        return new SafeVulkanInstanceHandle(
            instanceManualImports: default,
            instanceManualImports2: default,
            pAllocator: default
        );
    }

    private readonly VkInstanceManualImports m_instanceManualImports;
    private readonly VkInstanceManualImports2 m_instanceManualImports2;
    private readonly nint m_pAllocator;

    internal VkInstanceManualImports InstanceManualImports => m_instanceManualImports;
    internal VkInstanceManualImports2 InstanceManualImports2 => m_instanceManualImports2;

    private SafeVulkanInstanceHandle(
        VkInstanceManualImports instanceManualImports,
        VkInstanceManualImports2 instanceManualImports2,
        nint pAllocator
    ) : base(ownsHandle: true) {
        m_instanceManualImports = instanceManualImports;
        m_instanceManualImports2 = instanceManualImports2;
        m_pAllocator = pAllocator;
    }

    protected unsafe override bool ReleaseHandle() {
        vkDestroyInstance(
            instance: ((VkInstance)handle),
            pAllocator: (VkAllocationCallbacks*)m_pAllocator
        );

        return true;
    }

    public unsafe VkPhysicalDevice GetDefaultPhysicalGraphicsDeviceQueue(
        VkPhysicalDeviceType requestedDeviceType,
        out uint queueFamilyIndex
    ) {
        using var vulkanPhysicalDevicesHandle = GetPhysicalDevices(out var physicalDeviceCount);

        var physicalDevicesPointer = ((VkPhysicalDevice*)vulkanPhysicalDevicesHandle.DangerousGetHandle());

        VkPhysicalDevice physicalDevice;
        VkPhysicalDeviceProperties physicalDeviceProperties;

        for (var i = uint.MinValue; (i < physicalDeviceCount); ++i) {
            physicalDevice = physicalDevicesPointer[i];

            vkGetPhysicalDeviceProperties(
                physicalDevice: physicalDevice,
                pProperties: &physicalDeviceProperties
            );

            if (physicalDeviceProperties.deviceType == requestedDeviceType) {
                using var vulkanPhysicalDeviceQueueFamilyPropertiesHandle = GetPhysicalDeviceQueueFamilyProperties(
                    count: out var vkPhysicalDeviceQueueFamilyPropertyCount,
                    physicalDevice: physicalDevice
                );

                var vulkanPhysicalDeviceQueueFamilyPropertiesPointer = ((VkQueueFamilyProperties*)vulkanPhysicalDeviceQueueFamilyPropertiesHandle.DangerousGetHandle());

                for (var j = uint.MinValue; (j < vkPhysicalDeviceQueueFamilyPropertyCount); ++j) {
                    var vulkanPhysicalDeviceQueueFamilyProperties = vulkanPhysicalDeviceQueueFamilyPropertiesPointer[j];

                    if (vulkanPhysicalDeviceQueueFamilyProperties.queueFlags.HasFlag(flag: VkQueueFlags.VK_QUEUE_GRAPHICS_BIT)) {
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

        return SafeUnmanagedMemoryHandle.Create(size: nuint.MinValue);
    }
}
