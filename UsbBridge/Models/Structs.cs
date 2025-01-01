using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Models
{
    // 定义拷贝线状态
    public struct SCopyLineStatus
    {
        Local
    }

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

    /// <summary>
    /// 结构体对应libusb中的DEV_STATUS
    /// </summary>
    public struct SDEV_STATUS
    {
        private ushort _value; // 用于存储所有字段的 16 位无符号整数

        // pad (位 0), 补位
        public bool Pad
        {
            get => (_value & (1 << 0)) != 0;
            set => _value = value ? (ushort)(_value | (1 << 0)) : (ushort)(_value & ~(1 << 0));
        }

        // localAttached (位 1)（0：断开，1：已连接）
        public bool LocalAttached
        {
            get => (_value & (1 << 1)) != 0;
            set => _value = value ? (ushort)(_value | (1 << 1)) : (ushort)(_value & ~(1 << 1));
        }

        // localSpeed (位 2)（0：高速，1：超高速）
        public bool LocalSpeed
        {
            get => (_value & (1 << 2)) != 0;
            set => _value = value ? (ushort)(_value | (1 << 2)) : (ushort)(_value & ~(1 << 2));
        }

        // localSuspend (位 3)（0：活动，1：挂起）
        public bool LocalSuspend
        {
            get => (_value & (1 << 3)) != 0;
            set => _value = value ? (ushort)(_value | (1 << 3)) : (ushort)(_value & ~(1 << 3));
        }

        // pad1 (位 4-8, 共 5 位) 补位
        public byte Pad1
        {
            get => (byte)((_value >> 4) & 0x1F); // 提取位 4-8 (5 位)
            set
            {
                _value = (ushort)(_value & ~(0x1F << 4)); // 清除位 4-8
                _value |= (ushort)((value & 0x1F) << 4); // 设置新值 (限制为 5 位)
            }
        }

        // remoteAttached (位 9) （0：断开，1：已连接）
        public bool RemoteAttached
        {
            get => (_value & (1 << 9)) != 0;
            set => _value = value ? (ushort)(_value | (1 << 9)) : (ushort)(_value & ~(1 << 9));
        }

        // remoteSpeed (位 10)（0：高速，1：超高速）
        public bool RemoteSpeed
        {
            get => (_value & (1 << 10)) != 0;
            set => _value = value ? (ushort)(_value | (1 << 10)) : (ushort)(_value & ~(1 << 10));
        }

        // remoteSuspend (位 11)（0：活动，1：挂起）
        public bool RemoteSuspend
        {
            get => (_value & (1 << 11)) != 0;
            set => _value = value ? (ushort)(_value | (1 << 11)) : (ushort)(_value & ~(1 << 11));
        }

        // pad2 (位 12-15, 共 4 位)
        public byte Pad2
        {
            get => (byte)((_value >> 12) & 0x0F); // 提取位 12-15 (4 位)
            set
            {
                _value = (ushort)(_value & ~(0x0F << 12)); // 清除位 12-15
                _value |= (ushort)((value & 0x0F) << 12); // 设置新值 (限制为 4 位)
            }
        }

        // 获取或设置原始值（16 位整数）
        public ushort RawValue
        {
            get => _value;
            set => _value = value;
        }
    }
}