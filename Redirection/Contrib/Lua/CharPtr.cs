using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Lua
{
	public struct CharPtr
	{
		public static readonly CharPtr Zero = new CharPtr ();

		private IntPtr m_ptr;
		private int m_size;

		internal CharPtr( IntPtr ptr )
		{
			m_ptr = ptr;
			m_size = -1;
		}

		internal CharPtr( IntPtr ptr, int size )
		{
			m_ptr = ptr;
			m_size = size;
		}

		public byte[] GetBytes()
		{
			if (m_ptr == IntPtr.Zero) {
				return null;
			}

			var len = m_size;
			if (len < 0) {
				len = 0;
				while (Marshal.ReadByte (m_ptr, len) != 0)
				{
					++len;
				}
			}
			
			var bytes = new byte[len];
			Marshal.Copy (m_ptr, bytes, 0, len);
			return bytes;
		}

		public string Decode()
		{
			if (m_ptr == IntPtr.Zero) {
				return null;
			}

			var bytes = GetBytes ();
			if (bytes.Length == 0) {
				return string.Empty;
			} else {
				return Encoding.UTF8.GetString (bytes);
			}
		}

		public override string ToString ()
		{
            return m_ptr.ToString();
		}
	}
}
