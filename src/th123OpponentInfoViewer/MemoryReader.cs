using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace th123OpponentInfoViewer
{
    public class MemoryReader
    {
        private const int PROCESS_VM_READ = 0x0010;
        private const int PROCESS_QUERY_INFORMATION = 0x0400;

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(
            int dwDesiredAccess,
            bool bInheritHandle,
            int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            int dwSize,
            out int lpNumberOfBytesRead);

        public Process GetGameProcess()
        {
            Process[] processes =
                Process.GetProcessesByName("th123");

            if (processes.Length == 0)
            {
                return null;
            }

            return processes[0];
        }

        public IntPtr OpenGameProcess(Process process)
        {
            if (process == null)
            {
                return IntPtr.Zero;
            }

            return OpenProcess(
                PROCESS_VM_READ | PROCESS_QUERY_INFORMATION,
                false,
                process.Id);
        }

        public uint ReadUInt32(
            IntPtr processHandle,
            uint address)
        {
            if (processHandle == IntPtr.Zero)
            {
                return 0;
            }

            byte[] buffer = new byte[4];

            int bytesRead;

            bool success =
                ReadProcessMemory(
                    processHandle,
                    (IntPtr)address,
                    buffer,
                    4,
                    out bytesRead);

            if (!success)
            {
                return 0;
            }

            return BitConverter.ToUInt32(buffer, 0);
        }

        public byte[] ReadBytes(
            IntPtr processHandle,
            uint address,
            int size)
        {
            byte[] buffer = new byte[size];

            int bytesRead;

            bool success =
                ReadProcessMemory(
                    processHandle,
                    (IntPtr)address,
                    buffer,
                    size,
                    out bytesRead);

            if (!success)
            {
                return new byte[size];
            }

            return buffer;
        }

        public string ReadString(
            IntPtr processHandle,
            uint address,
            int size)
        {
            byte[] buffer =
                ReadBytes(
                    processHandle,
                    address,
                    size);

            int length =
                Array.IndexOf(
                    buffer,
                    (byte)0);

            if (length < 0)
            {
                length = buffer.Length;
            }

            return Encoding
                .GetEncoding(932)
                .GetString(
                    buffer,
                    0,
                    length)
                .Trim();
        }

        public string ReadIPAddress(
            IntPtr processHandle,
            uint address)
        {
            uint ip =
                ReadUInt32(
                    processHandle,
                    address);

            if (ip == 0)
            {
                return "";
            }

            byte[] bytes =
                BitConverter.GetBytes(ip);

            return string.Format(
                "{0}.{1}.{2}.{3}",
                bytes[0],
                bytes[1],
                bytes[2],
                bytes[3]);
        }
    }
}