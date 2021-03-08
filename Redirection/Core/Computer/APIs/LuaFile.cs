using Dan200.Core.Lua;
using System;
using System.IO;
using System.Text;

namespace Dan200.Core.Computer.APIs
{
    public enum LuaFileContentType
    {
        Text,
        Binary
    }

    public enum LuaFileOpenMode
    {
        Read,
        Write,
    }

    [LuaType("file", exposeType: false)]
    public class LuaFile : LuaObject
    {
        private readonly LuaFileContentType m_contentType;
        private readonly LuaFileOpenMode m_openMode;
        private readonly Stream m_stream;
        private readonly TextReader m_reader;
        private readonly TextWriter m_writer;
        private readonly bool m_leaveOpen;
        private bool m_closed;

        public bool IsOpen
        {
            get
            {
                return !m_closed;
            }
        }

        public LuaFileContentType ContentType
        {
            get
            {
                return m_contentType;
            }
        }

        public LuaFileOpenMode OpenMode
        {
            get
            {
                return m_openMode;
            }
        }

        public TextReader InnerReader
        {
            get
            {
                return m_reader;
            }
        }

        public TextWriter InnerWriter
        {
            get
            {
                return m_writer;
            }
        }

        public Stream InnerStream
        {
            get
            {
                return m_stream;
            }
        }

        public LuaFile(TextReader reader, bool leaveOpen = false)
        {
            m_contentType = LuaFileContentType.Text;
            m_openMode = LuaFileOpenMode.Read;
            m_reader = reader;
            m_leaveOpen = leaveOpen;
        }

        public LuaFile(TextWriter writer, bool leaveOpen = false)
        {
            m_contentType = LuaFileContentType.Text;
            m_openMode = LuaFileOpenMode.Write;
            m_writer = writer;
            m_leaveOpen = leaveOpen;
        }

        public LuaFile(Stream stream, LuaFileOpenMode mode, bool leaveOpen = false)
        {
            m_contentType = LuaFileContentType.Binary;
            m_openMode = mode;
            m_stream = stream;
            m_leaveOpen = leaveOpen;
        }

        public void Close()
        {
            if (!m_closed)
            {
                if (m_contentType == LuaFileContentType.Text && m_openMode == LuaFileOpenMode.Read)
                {
                    if (!m_leaveOpen)
                    {
                        m_reader.Close();
                    }
                }
                else if (m_contentType == LuaFileContentType.Text && m_openMode == LuaFileOpenMode.Write)
                {
                    if (!m_leaveOpen)
                    {
                        m_writer.Close();
                    }
                }
                else if (m_contentType == LuaFileContentType.Binary)
                {
                    if (!m_leaveOpen)
                    {
                        m_stream.Close();
                    }
                }
                m_closed = true;
            }
        }

        public override void Dispose()
        {
            if (m_contentType == LuaFileContentType.Text && m_openMode == LuaFileOpenMode.Read)
            {
                if (!m_leaveOpen)
                {
                    m_reader.Dispose();
                }
            }
            else if (m_contentType == LuaFileContentType.Text && m_openMode == LuaFileOpenMode.Write)
            {
                if (!m_leaveOpen)
                {
                    m_writer.Dispose();
                }
            }
            else if (m_contentType == LuaFileContentType.Binary)
            {
                if (!m_leaveOpen)
                {
                    m_stream.Dispose();
                }
            }
            m_closed = true;
        }

        [LuaMethod]
        public LuaArgs close(LuaArgs args)
        {
            try
            {
                CheckOpen();
                Close();
                return new LuaArgs(true);
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs flush(LuaArgs args)
        {
            try
            {
                CheckOpen();
                if (m_openMode == LuaFileOpenMode.Write)
                {
                    if (m_contentType == LuaFileContentType.Text)
                    {
                        m_writer.Flush();
                    }
                    else if (m_contentType == LuaFileContentType.Binary)
                    {
                        m_stream.Flush();
                    }
                }
                return new LuaArgs(true);
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        public LuaArgs Lines(LuaArgs args, bool autoClose)
        {
            CheckOpen();
            return new LuaArgs((LuaCFunction)delegate (LuaArgs args2)
            {
                var results = read(args);
                if (autoClose && results.IsNil(0))
                {
                    Close();
                }
                return results;
            });
        }

        [LuaMethod]
        public LuaArgs lines(LuaArgs args)
        {
            return Lines(args, false);
        }

		private LuaArgs ReadOneAsync(LuaValue fmt, LuaContinuation continuation)
		{
			if (fmt.IsNumber())
			{
				// Number
				var count = fmt.GetInt();
				if (m_contentType == LuaFileContentType.Binary)
				{
					// Read some bytes
					var bytes = new byte[count];
					if( m_stream is LuaFunctionStream )
					{
						return ((LuaFunctionStream)m_stream).ReadAsync(
							bytes, 0, count,
							delegate(LuaArgs args) {
								int bytesRead = args.GetInt(0);
								if (bytesRead > 0)
								{
									if (bytesRead != count)
									{
										Array.Resize(ref bytes, bytesRead);
									}
									return continuation.Invoke(new LuaArgs(bytes));
								}
								else
								{
									return continuation.Invoke(LuaArgs.Nil);
								}
							}
						);
					}
					else
					{
						int bytesRead = m_stream.Read(bytes, 0, count);
						if (bytesRead > 0)
						{
							if (bytesRead != count)
							{
								Array.Resize(ref bytes, bytesRead);
							}
							return continuation.Invoke(new LuaArgs(bytes));
						}
						else
						{
							return continuation.Invoke(LuaArgs.Nil);
						}
					}
				}
				else
				{
					// Read some chars
					var chars = new char[count];
					int charsRead = m_reader.Read(chars, 0, count);
					if (charsRead > 0)
					{
						var str = new string(chars, 0, charsRead);
						return continuation.Invoke(new LuaArgs(str));
					}
					else
					{
						return continuation.Invoke(LuaArgs.Nil);
					}
				}
			}
			else
			{
				// Format string
				var fmtStr = fmt.GetString();
				if (fmtStr == "*n" || fmtStr == "n")
				{
					// Read a number
					if (m_contentType == LuaFileContentType.Binary)
					{
						throw new NotImplementedException();
					}
					else
					{
						var num = m_reader.ReadLine();
						long lresult;
						double dresult;
						if (long.TryParse(num, out lresult))
						{
							return continuation.Invoke(new LuaArgs(lresult));
						}
						else if (double.TryParse(num, out dresult))
						{
							return continuation.Invoke(new LuaArgs(dresult));
						}
						else
						{
							return continuation.Invoke(LuaArgs.Nil);
						}
					}
				}
				else if (fmtStr == "*a" || fmtStr == "a")
				{
					// Read to end
					if (m_contentType == LuaFileContentType.Binary)
					{
						byte[] buffer = new byte[4096];
						var memoryStream = new MemoryStream();
						if (m_stream is LuaFunctionStream)
						{
							var luaStream = (LuaFunctionStream)m_stream;
							LuaContinuation onReturn = null;
							onReturn = delegate (LuaArgs args)
							{
								var bytesRead = args.GetInt(0);
								if (bytesRead > 0)
								{
									memoryStream.Write(buffer, 0, bytesRead);
									return luaStream.ReadAsync(buffer, 0, buffer.Length, onReturn);
								}
								else
								{
									var bytes = memoryStream.ToArray();
									return continuation.Invoke(new LuaArgs(bytes));
								}
									
							};
							return luaStream.ReadAsync(buffer, 0, buffer.Length, onReturn);
						}
						else
						{
							int bytesRead;
							while ((bytesRead = m_stream.Read(buffer, 0, buffer.Length)) > 0)
							{
								memoryStream.Write(buffer, 0, bytesRead);
							}
							var bytes = memoryStream.ToArray();
							return continuation.Invoke(new LuaArgs(bytes));
						}
					}
					else
					{
						var str = m_reader.ReadToEnd();
						return continuation.Invoke(new LuaArgs(str));
					}
				}
				else if (fmtStr == "*l" || fmtStr == "l")
				{
					// Read a line
					if (m_contentType == LuaFileContentType.Binary)
					{
						var memoryStream = new MemoryStream();
						if (m_stream is LuaFunctionStream)
						{
							var luaStream = (LuaFunctionStream)m_stream;
							byte[] buffer = new byte[4096];
							LuaContinuation onReturn = null;
							onReturn = delegate (LuaArgs args)
							{
								var bytesRead = args.GetInt(0);
								if( bytesRead < buffer.Length || buffer[bytesRead - 1] == '\n' )
								{
									// Found newline or EOF
									memoryStream.Write(buffer, 0, bytesRead);
									if (memoryStream.Length == 0)
									{
										return continuation.Invoke(LuaArgs.Nil);
									}
									else
									{
										// Trim newline
										var results = memoryStream.GetBuffer();
										var len = memoryStream.Length;
										if (len > 0 && results[len - 1] == '\n')
										{
											len--;
										}
										if (len > 0 && results[len - 1] == '\r')
										{
											len--;
										}

										// Return bytes
										var bytes = new byte[len];
										Array.Copy(results, bytes, len);
										return continuation.Invoke(new LuaArgs(bytes));
									}
								}
								else
								{
									// Get some more bytes
									memoryStream.Write(buffer, 0, bytesRead);
									return luaStream.ReadUntilAsync(buffer, 0, buffer.Length, (byte)'\n', onReturn);
								}
							};
							return luaStream.ReadUntilAsync(buffer, 0, buffer.Length, (byte)'\n', onReturn);
						}
						else
						{
							var b = m_stream.ReadByte();
							if (b < 0)
							{
								return continuation.Invoke(LuaArgs.Nil);
							}
							else
							{
								while (true)
								{
									if (b < 0 || b == '\n')
									{
										break;
									}
									else if (b != '\r')
									{
										memoryStream.WriteByte((byte)b);
									}
									b = m_stream.ReadByte();
								}
								var bytes = memoryStream.ToArray();
								return continuation.Invoke(new LuaArgs(bytes));
							}
						}
					}
					else
					{
						var str = m_reader.ReadLine();
						if (str != null)
						{
							return continuation.Invoke(new LuaArgs(str));
						}
						else
						{
							return continuation.Invoke(LuaArgs.Nil);
						}
					}
				}
				else if (fmt == "*L" || fmt == "L")
				{
					// Read a line including the line ending
					if (m_contentType == LuaFileContentType.Binary)
					{
						var memoryStream = new MemoryStream();
						if (m_stream is LuaFunctionStream)
						{
							var luaStream = (LuaFunctionStream)m_stream;
							byte[] buffer = new byte[4096];
							LuaContinuation onReturn = null;
							onReturn = delegate (LuaArgs args)
							{
								var bytesRead = args.GetInt(0);
								if (bytesRead < buffer.Length || buffer[bytesRead - 1] == '\n')
								{
									// Found newline or EOFF
									memoryStream.Write(buffer, 0, bytesRead);
									if (memoryStream.Length == 0)
									{
										return continuation.Invoke(LuaArgs.Nil);
									}
									else
									{
										// Return bytess
										var bytes = memoryStream.ToArray();
										return continuation.Invoke(new LuaArgs(bytes));
									}
								}
								else
								{
									// Get some more bytes
									memoryStream.Write(buffer, 0, bytesRead);
									return luaStream.ReadUntilAsync(buffer, 0, buffer.Length, (byte)'\n', onReturn);
								}
							};
							return luaStream.ReadUntilAsync(buffer, 0, buffer.Length, (byte)'\n', onReturn);
						}
						else
						{
							var b = m_stream.ReadByte();
							if (b < 0)
							{
								return continuation.Invoke(LuaArgs.Nil);
							}
							else
							{
								while (true)
								{
									if (b < 0)
									{
										break;
									}
									else if (b == '\n')
									{
										memoryStream.WriteByte((byte)b);
										break;
									}
									else
									{
										memoryStream.WriteByte((byte)b);
									}
									b = m_stream.ReadByte();
								}
								var bytes = memoryStream.ToArray();
								return continuation.Invoke(new LuaArgs(bytes));
							}
						}
					}
					else
					{
						var str = m_reader.ReadLine();
						if (str != null)
						{
							str = str + '\n';
							return continuation.Invoke(new LuaArgs(str));
						}
						else
						{
							return continuation.Invoke(LuaArgs.Nil);
						}
					}
				}
				else
				{
					throw new LuaError("Invalid format: " + fmt);
				}
			}
		}

        [LuaMethod]
        public LuaArgs read(LuaArgs args)
        {
            try
            {
                CheckOpen();
                if (m_openMode == LuaFileOpenMode.Read)
                {
                    var results = new LuaValue[Math.Max(args.Length, 1)];
                    int i = -1;
					LuaContinuation continuation = null;
					continuation = delegate (LuaArgs args2)
					{
						if (i >= 0)
						{
							results[i] = args2[0];
							if (results[i].IsNil())
							{
                    			return new LuaArgs(results).Select(0, i + 1);
							}
						}
						if (i < results.Length - 1)
						{
							i = i + 1;
							var fmt = (i == 0 && args.IsNil(i)) ? new LuaValue("l") : args[i];
							return ReadOneAsync(fmt, continuation);
						}
						else
						{
							return new LuaArgs(results);
						}
					};
					return continuation.Invoke(LuaArgs.Empty);
                }
                return LuaArgs.Empty;
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs seek(LuaArgs args)
        {
            try
            {
                var whence = args.IsNil(0) ? "cur" : args.GetString(0);
                var offset = args.IsNil(1) ? 0 : args.GetInt(1);

                CheckOpen();
                if (m_contentType == LuaFileContentType.Binary)
                {
                    // Seek in a binary file
                    switch (whence)
                    {
                        case "set":
                            {
                                m_stream.Seek(offset, SeekOrigin.Begin);
                                break;
                            }
                        case "cur":
                            {
                                m_stream.Seek(offset, SeekOrigin.Current);
                                break;
                            }
                        case "end":
                            {
                                m_stream.Seek(offset, SeekOrigin.End);
                                break;
                            }
                        default:
                            {
                                throw new LuaError("Unsupported option: " + whence);
                            }
                    }
                    return new LuaArgs(m_stream.Position);
                }
                else
                {
                    // Seek in a text file
                    throw new NotImplementedException();
                }
            }
            catch (IOException e)
            {
                return new LuaArgs(LuaValue.Nil, e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs setvbuf(LuaArgs args)
        {
            CheckOpen();
            throw new NotImplementedException();
        }

		private LuaArgs WriteOneAsync( LuaValue value, LuaContinuation continuation)
		{
			if (value.IsNumber())
			{
				// Write a number
				var str = value.ToString();
				if (m_contentType == LuaFileContentType.Binary)
				{
					var bytes = Encoding.UTF8.GetBytes(str);
					if (m_stream is LuaFunctionStream)
					{
						return ((LuaFunctionStream)m_stream).WriteAsync(bytes, 0, bytes.Length, continuation);
					}
					else
					{
						m_stream.Write(bytes, 0, bytes.Length);
						return continuation.Invoke(LuaArgs.Empty);
					}
				}
				else
				{
					m_writer.Write(str);
					return continuation.Invoke(LuaArgs.Empty);
				}
			}
			else
			{
				// Write a string
				if (m_contentType == LuaFileContentType.Binary)
				{
					var bytes = value.GetByteString();
					if (m_stream is LuaFunctionStream)
					{
						return ((LuaFunctionStream)m_stream).WriteAsync(bytes, 0, bytes.Length, continuation);
					}
					else
					{
						m_stream.Write(bytes, 0, bytes.Length);
						return continuation.Invoke(LuaArgs.Empty);
					}
				}
				else
				{
					var str = value.GetString();
					m_writer.Write(str.Replace("\n", Environment.NewLine));
					return continuation.Invoke(LuaArgs.Empty);
				}
			}
		}

        [LuaMethod]
        public LuaArgs write(LuaArgs args)
        {
            try
            {
                CheckOpen();
                if (m_openMode == LuaFileOpenMode.Write)
                {
					// Write some stuff
					int i = 0;
					LuaContinuation continuation = null;
					continuation = delegate {
						if (!args.IsNil(i))
						{
							return WriteOneAsync(args[i++], continuation);
						}
						else
						{
							return new LuaArgs(this);
						}
					};
					return continuation.Invoke(LuaArgs.Empty);
                }
                return new LuaArgs(this);
            }
            catch (IOException e)
            {
                return new LuaArgs(LuaValue.Nil, e.Message);
            }
        }

        private void CheckOpen()
        {
            if (m_closed)
            {
                throw new LuaError("Attempt to use closed file");
            }
        }
    }
}
