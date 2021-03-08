using Dan200.Core.Lua;
using System;
using System.Collections.Generic;
using System.IO;

namespace Dan200.Core.Computer.APIs
{
    public class LuaFunctionStream : Stream
    {
        private LuaFunction m_function;
        private LuaFileOpenMode m_mode;
        private Queue<byte> m_buffer;

        public override bool CanRead
        {
            get
            {
                return m_mode == LuaFileOpenMode.Read;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return m_mode == LuaFileOpenMode.Write;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public LuaFunctionStream(LuaFunction function, LuaFileOpenMode mode)
        {
            m_function = function;
            m_mode = mode;
            m_buffer = new Queue<byte>();
        }

        protected override void Dispose(bool disposing)
        {
            m_buffer.Clear();
            m_function = null;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (m_mode == LuaFileOpenMode.Read)
            {
                BufferInput(count);
				return DequeueBytes(buffer, offset, count);
            }
            return 0;
        }

		public int ReadUntil(byte[] buffer, int offset, int count, byte target)
		{
			if (m_mode == LuaFileOpenMode.Read)
			{
				count = Math.Min( count, BufferInputUntil(target) );
				return DequeueBytes(buffer, offset, count);
			}
			return 0;
		}

		public LuaArgs ReadUntilAsync(byte[] buffer, int offset, int count, byte target, LuaContinuation continuation )
		{
			if (m_mode == LuaFileOpenMode.Read)
			{
				return BufferInputUntilAsync(target, delegate(LuaArgs results) {
					count = Math.Min(count, results.GetInt(0));
					count = DequeueBytes(buffer, offset, count);
					return continuation.Invoke(new LuaArgs(count));
				});
			}
			return continuation.Invoke(new LuaArgs(0));
		}

		public LuaArgs ReadAsync(byte[] buffer, int offset, int count, LuaContinuation continuation)
		{
			if (m_mode == LuaFileOpenMode.Read)
			{
				return BufferInputAsync(count, delegate (LuaArgs args)
				{
					count = DequeueBytes(buffer, offset, count);
					return continuation.Invoke(new LuaArgs(count));
				});
			}
			return continuation.Invoke(new LuaArgs(0));
		}

        public override int ReadByte()
        {
            if (m_mode == LuaFileOpenMode.Read)
            {
				BufferInput(1);
				return DequeueByte();
            }
            return -1;
        }

		public LuaArgs ReadByteAsync(LuaContinuation continuation)
		{
			if (m_mode == LuaFileOpenMode.Read)
			{
				return BufferInputAsync(1, delegate (LuaArgs args)
				{
					var b = DequeueByte();
					return continuation.Invoke(new LuaArgs(b));
				});
			}
			return continuation.Invoke(new LuaArgs(-1));
		}

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (m_mode == LuaFileOpenMode.Write)
            {
				QueueBytes(buffer, offset, count);
                EmitOutput();
            }
        }

		public LuaArgs WriteAsync(byte[] buffer, int offset, int count, LuaContinuation continuation)
		{
			if (m_mode == LuaFileOpenMode.Write)
			{
				QueueBytes(buffer, offset, count);
				return EmitOutputAsync(continuation);
			}
			return continuation.Invoke(LuaArgs.Empty);
		}

		public override void WriteByte(byte value)
        {
            if (m_mode == LuaFileOpenMode.Write)
            {
				QueueByte(value);
                EmitOutput();
            }
        }

		public LuaArgs WriteByteAsync(byte value, LuaContinuation continuation)
		{
			if (m_mode == LuaFileOpenMode.Write)
			{
				QueueByte(value);
				return EmitOutputAsync(continuation);
			}
			return continuation.Invoke(LuaArgs.Empty);
		}

        public override void Flush()
        {
        }

		private int BufferInputUntil(byte target)
		{
			int i = 0;
			foreach(var b in m_buffer )
			{
				if (b == target)
				{
					return i + 1;
				}
				else
				{
					++i;
				}
			}

			while (m_function != null)
			{
				var results = m_function.Call(new LuaArgs(1));
				if (results.IsString(0))
				{
					var bytes = results.GetByteString(0);
					if (bytes.Length > 0)
					{
						// Read some bytes
						QueueBytes(bytes, 0, bytes.Length);
						for (int j = 0; j < bytes.Length; ++j )
						{
							if (bytes[j] == target)
							{
								return i + j + 1;
							}
						}
						i += bytes.Length;
					}
					else
					{
						// EOF
						m_function = null;
					}
				}
				else
				{
					// EOF
					m_function = null;
				}
			}

			return m_buffer.Count;
		}

		private LuaArgs BufferInputUntilAsync(byte target, LuaContinuation continuation)
		{
			int i = 0;
			foreach (var b in m_buffer)
			{
				if (b == target)
				{
					return continuation.Invoke( new LuaArgs(i + 1) );
				}
				else
				{
					++i;
				}
			}

			LuaContinuation onReturn = null;
			onReturn = delegate (LuaArgs results)
			{
				if (results.IsString(0))
				{
					var bytes = results.GetByteString(0);
					if (bytes.Length > 0)
					{
						// Read some bytes
						QueueBytes(bytes, 0, bytes.Length);
						for (int j = 0; j < bytes.Length; ++j)
						{
							if (bytes[j] == target)
							{
								return continuation.Invoke(new LuaArgs(i + j + 1));
							}
						}
						i += bytes.Length;
					}
					else
					{
						// EOF
						m_function = null;
					}
				}
				else
				{
					// EOF
					m_function = null;
				}
				if (m_function != null)
				{
					return m_function.CallAsync(new LuaArgs(1), onReturn);
				}
				else
				{
					return continuation.Invoke(new LuaArgs(m_buffer.Count));
				}
			};
			if (m_function != null)
			{
				return m_function.CallAsync(new LuaArgs(1), onReturn);
			}
			else
			{
				return continuation.Invoke(new LuaArgs(m_buffer.Count));
			}
		}

		private void BufferInput(int bytesNeeded)
        {
			while (m_function != null && m_buffer.Count < bytesNeeded)
            {
				var results = m_function.Call(new LuaArgs(bytesNeeded - m_buffer.Count));
                if (results.IsString(0))
                {
                    var bytes = results.GetByteString(0);
                    if (bytes.Length > 0)
                    {
						// Read some bytes
						QueueBytes(bytes, 0, bytes.Length);
                    }
                    else
                    {
                        // EOF
                        m_function = null;
                    }
                }
                else
                {
                    // EOF
                    m_function = null;
                }
            }
        }

		private LuaArgs BufferInputAsync(int bytesNeeded, LuaContinuation continuation)
		{
			LuaContinuation onReturn = null;
			onReturn = delegate (LuaArgs results)
			{
				if (results.IsString(0))
				{
					var bytes = results.GetByteString(0);
					if (bytes.Length > 0)
					{
						// Read some bytes
						QueueBytes(bytes, 0, bytes.Length);
					}
					else
					{
						// EOF
						m_function = null;
					}
				}
				else
				{
					// EOF
					m_function = null;
				}
				if (m_function != null && m_buffer.Count < bytesNeeded)
				{
					return m_function.CallAsync(new LuaArgs(bytesNeeded - m_buffer.Count), onReturn);
				}
				else
				{
					return continuation.Invoke(LuaArgs.Empty);
				}
			};
			if (m_function != null && m_buffer.Count < bytesNeeded)
			{
				return m_function.CallAsync(new LuaArgs(m_buffer.Count - bytesNeeded), onReturn);
			}
			else
			{
				return continuation.Invoke(LuaArgs.Empty);
			}
		}

        private void EmitOutput()
        {
            if (m_function != null && m_buffer.Count > 0)
            {
				var bytes = new byte[m_buffer.Count];
				DequeueBytes(bytes, 0, bytes.Length);
                m_function.Call(new LuaArgs(bytes));
            }
        }

		private LuaArgs EmitOutputAsync( LuaContinuation continuation )
		{
			if (m_function != null && m_buffer.Count > 0)
			{
				var bytes = new byte[m_buffer.Count];
				DequeueBytes(bytes, 0, bytes.Length);
				return m_function.CallAsync(new LuaArgs(bytes), continuation);
			}
			return continuation.Invoke(LuaArgs.Empty);
		}

		private void QueueByte(byte b)
		{
			m_buffer.Enqueue(b);
		}

		private void QueueBytes(byte[] buffer, int offset, int count)
		{
			for (int i = offset; i < offset + count; ++i)
			{
				m_buffer.Enqueue(buffer[i]);
			}
		}

		private int DequeueByte()
		{
			if (m_buffer.Count > 0)
			{
				return m_buffer.Dequeue();
			}
			else
			{
				return -1;
			}
		}

		private int DequeueBytes(byte[] buffer, int offset, int count)
		{
			if (count >= m_buffer.Count )
			{
				count = m_buffer.Count;
				m_buffer.CopyTo(buffer, offset);
				m_buffer.Clear();
				return count;
			}
			else
			{
				for (int i = offset; i < offset + count; ++i)
				{
					buffer[i] = m_buffer.Dequeue();
				}
				return count;
			}
		}
	}
}
