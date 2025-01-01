using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Models
{

    // 定义libusb结构体
    [StructLayout(LayoutKind.Sequential)]
    public struct SLibusbDeviceDescriptor
    {
        public byte bLength;                // Size of this descriptor (in bytes)
        public byte bDescriptorType;        // Descriptor type
        public ushort bcdUSB;               // USB specification release number in binary-coded decimal.
        public byte bDeviceClass;           // USB-IF class code for the device.
        public byte bDeviceSubClass;        // USB-IF subclass code for the device, qualified by the bDeviceClass value
        public byte bDeviceProtocol;        // USB-IF protocol code for the device, qualified by the bDeviceClass and bDeviceSubClass values
        public byte bMaxPacketSize0;        // Maximum packet size for endpoint 0
        public ushort idVendor;             // USB-IF vendor ID
        public ushort idProduct;            // USB-IF product ID
        public ushort bcdDevice;            // Device release number in binary-coded decimal
        public byte iManufacturer;          // Index of string descriptor describing manufacturer
        public byte iProduct;               // Index of string descriptor describing product
        public byte iSerialNumber;          // Index of string descriptor containing device serial number
        public byte bNumConfigurations;     // Number of possible configurations
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SLibusbConfigDescriptor
    {
        public byte bLength;                // Size of this descriptor (in bytes).
        public byte bDescriptorType;        // Descriptor type. Will have value LIBUSB_DT_CONFIG in this context.
        public ushort wTotalLength;         // Total length of data returned for this configuration.
        public byte bNumInterfaces;         // Number of interfaces supported by this configuration.
        public byte bConfigurationValue;    // Identifier value for this configuration.
        public byte iConfiguration;         // Index of string descriptor describing this configuration.
        public byte bmAttributes;          // Configuration characteristics.
        public byte MaxPower;               // Maximum power consumption of the USB device from this bus in this configuration when the device is fully operational.
        // Not used directly
        public IntPtr interface_;           // Pointer to array of libusb_interface structs (simplified)
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SLibusbInterfaceDescriptor
    {
        public byte bLength;                // Size of this descriptor (in bytes)
        public byte bDescriptorType;        // Descriptor type. Will have value
        public byte bInterfaceNumber;       // Number of this interface
        public byte bAlternateSetting;      // Value used to select this alternate setting for this interface
        public byte bNumEndpoints;          // Number of endpoints used by this interface (excluding the control endpoint).
        public byte bInterfaceClass;        // USB-IF class code for this interface. See \ref libusb_class_code.
        public byte bInterfaceSubClass;     // USB-IF subclass code for this interface, qualified by the bInterfaceClass value
        public byte bInterfaceProtocol;     // USB-IF protocol code for this interface, qualified by the bInterfaceClass and bInterfaceSubClass values
        public byte iInterface;             // Index of string descriptor describing this interface
        public IntPtr endpoint;             // Array of endpoint descriptors. The length of this array is determined by the bNumEndpoints field.
        public IntPtr extra;                // Extra descriptors. If libusb encounters unknown interface descriptors, it will store them here.
        public int extra_length;            // Length of the extra descriptors, in bytes.
    }

    /// <summary>
    /// Represents the standard USB endpoint descriptor.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SLibusbEndpointDescriptor
    {
        public byte bLength;                // Size of this descriptor (in bytes)
        public byte bDescriptorType;        // Descriptor type. Will have value
        public byte bEndpointAddress;       // The address of the endpoint described by this descriptor. Bits 0:3 are the endpoint number.Bits 4:6 are reserved.Bit 7 indicates direction, see \ref libusb_endpoint_direction.
        public byte bmAttributes;           // Attributes which apply to the endpoint
        public ushort wMaxPacketSize;       // Maximum packet size this endpoint is capable of sending/receiving.
        public byte bInterval;              // Interval for polling endpoint for data transfers.
        public byte bRefresh;               // For audio devices only: the rate at which synchronization feedback is provided.
        public byte bSynchAddress;          // For audio devices only: the address if the synch endpoint
        public IntPtr bExtra;               // Extra descriptors. If libusb encounters unknown endpoint descriptors, it will store them here, should you wish to parse them.
        public int bNumEndpoints;           // Length of the extra descriptors, in bytes.
    }

    /// <summary>结构体对应libusb中的DEV_STATUS</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SDEV_STATUS
    {
        public bool localSuspend;           // 本地设备是否挂起
        public bool localAttached;          // 本地设备是否连接
        public bool localSpeed;             // 本地设备速度（超级速度或高速）
        public byte pad1;                   // 填充字段
        public bool remoteSuspend;          // 远程设备是否挂起
        public bool remoteAttached;         // 远程设备是否连接
        public bool remoteSpeed;            // 远程设备速度（超级速度或高速）
        public byte pad2;                   // 填充字段
    }

}
