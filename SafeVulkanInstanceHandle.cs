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
        var destinationHandle = SafeUnmanagedMemoryHandle.Create(size: ((nuint)(requestedNames.Count * sizeof(nuint))));
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
        var propertyCount = uint.MinValue;

        vkGetPhysicalDeviceQueueFamilyProperties2(
            physicalDevice: physicalDevice,
            pQueueFamilyProperties: null,
            pQueueFamilyPropertyCount: &propertyCount
        );

        var propertiesHandle = SafeUnmanagedMemoryHandle.Create(size: (propertyCount * ((uint)sizeof(VkQueueFamilyProperties2))));
        var propertiesPointer = ((VkQueueFamilyProperties2*)propertiesHandle.DangerousGetHandle());

        for (var i = uint.MinValue; (i < propertyCount); ++i) {
            propertiesPointer[i] = new VkQueueFamilyProperties2 {
                pNext = null,
                sType = VkStructureType.VK_STRUCTURE_TYPE_QUEUE_FAMILY_PROPERTIES_2,
            };
        }

        vkGetPhysicalDeviceQueueFamilyProperties2(
            physicalDevice: physicalDevice,
            pQueueFamilyProperties: propertiesPointer,
            pQueueFamilyPropertyCount: &propertyCount
        );

        count = propertyCount;

        return propertiesHandle;
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
            var propertiesHandle = SafeUnmanagedMemoryHandle.Create(size: (propertyCount * ((uint)sizeof(VkExtensionProperties))));

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
        HashSet<string> requestedExtensionNames,
        nint pAllocator = default
    ) {
        using var supportedExtensionPropertiesHandle = GetSupportedDeviceExtensionProperties(
            count: out var supportedExtensionPropertyCount,
            physicalDevice: physicalDevice
        );
        using var enabledExtensionNamesHandle = GetEnabledNames<VkExtensionProperties>(
            enabledPropertyCount: out var enabledExtensionCount,
            requestedNames: requestedExtensionNames,
            supportedPropertiesHandle: supportedExtensionPropertiesHandle,
            supportedPropertyCount: supportedExtensionPropertyCount
        );

        var supportedPhysicalDeviceFeatures = new VkPhysicalDeviceFeatures2 {
            pNext = null,
            sType = VkStructureType.VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_FEATURES_2,
        };

        vkGetPhysicalDeviceFeatures2(
            pFeatures: &supportedPhysicalDeviceFeatures,
            physicalDevice: physicalDevice
        );

        var enabledPhysicalDeviceFeatures = new VkPhysicalDeviceFeatures { };
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
                pEnabledFeatures = &enabledPhysicalDeviceFeatures,
                pNext = null,
                ppEnabledExtensionNames = ((sbyte**)enabledExtensionNamesHandle.DangerousGetHandle()),
                ppEnabledLayerNames = null,
                pQueueCreateInfos = &logicalDeviceQueueCreateInfo,
                queueCreateInfoCount = 1U,
                sType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO,
            },
            pAllocator: pAllocator,
            physicalDevice: physicalDevice
        );
        var logicalDeviceQueueInfo = new VkDeviceQueueInfo2 {
            flags = uint.MinValue,
            pNext = null,
            queueFamilyIndex = queueFamilyIndex,
            queueIndex = uint.MinValue,
            sType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_QUEUE_INFO_2,
        };

        VkQueue logicalDeviceQueue;

        vkGetDeviceQueue2(
            device: ((VkDevice)logicalDeviceHandle.DangerousGetHandle()),
            pQueue: &logicalDeviceQueue,
            pQueueInfo: &logicalDeviceQueueInfo
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
        using var enabledLayerNamesHandle = GetEnabledNames<VkLayerProperties>(
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
                    vkCreateHeadlessSurfaceEXT = ((delegate* unmanaged<VkInstance, VkHeadlessSurfaceCreateInfoEXT*, VkAllocationCallbacks*, VkSurfaceKHR*, VkResult>)vkGetInstanceProcAddr(
                        instance: vkInstance,
                        pName: DangerousGetPointer(span: "vkCreateHeadlessSurfaceEXT\u0000"u8)
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
        using var physicalDevicesHandle = GetPhysicalDevices(out var physicalDeviceCount);

        var physicalDevicesPointer = ((VkPhysicalDevice*)physicalDevicesHandle.DangerousGetHandle());

        VkPhysicalDevice physicalDevice;

        for (var i = uint.MinValue; (i < physicalDeviceCount); ++i) {
            var physicalDeviceProperties = new VkPhysicalDeviceProperties2 {
                pNext = null,
                sType = VkStructureType.VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_PROPERTIES_2,
            };

            physicalDevice = physicalDevicesPointer[i];

            vkGetPhysicalDeviceProperties2(
                physicalDevice: physicalDevice,
                pProperties: &physicalDeviceProperties
            );

            if (physicalDeviceProperties.properties.deviceType == requestedDeviceType) {
                using var physicalDeviceQueueFamilyPropertiesHandle = GetPhysicalDeviceQueueFamilyProperties(
                    count: out var physicalDeviceQueueFamilyPropertyCount,
                    physicalDevice: physicalDevice
                );

                var vulkanPhysicalDeviceQueueFamilyPropertiesPointer = ((VkQueueFamilyProperties2*)physicalDeviceQueueFamilyPropertiesHandle.DangerousGetHandle());

                for (var j = uint.MinValue; (j < physicalDeviceQueueFamilyPropertyCount); ++j) {
                    var physicalDeviceQueueFamilyProperties = vulkanPhysicalDeviceQueueFamilyPropertiesPointer[j];

                    if (physicalDeviceQueueFamilyProperties.queueFamilyProperties.queueFlags.HasFlag(flag: VkQueueFlags.VK_QUEUE_GRAPHICS_BIT)) {
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
