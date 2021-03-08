namespace Dan200.Core.Lua
{
    internal enum BLONValueType
    {
        Nil = 0,
        False,
        True,
        Zero,
        One,
        UInt8,
        UInt16,
        UInt32,
        UInt8_Negative,
        UInt16_Negative,
        UInt32_Negative,
        Int64,
        Float32,
        Float64,
        String8,
        String16,
        String32,
        String8_Cached,
        String16_Cached,
        String32_Cached,
        PreviouslyCachedString,
        Table8,
        Table16,
        Table32,
    }
}
