using System;
using System.Buffers.Binary;

namespace Tpl
{
    /// <summary>
    /// Byte layout of a world snapshot (SPEC.md section 6), little-endian, the same on every machine:
    /// header (magic "INKS", format, tuning hash, tick, next id, character count), then each character.
    /// Doubles are stored as their raw bits, so a load restores the exact numbers.
    /// </summary>
    static class Snapshot
    {
        public const uint Magic = 0x534B4E49;   // "INKS"
        public const uint Format = 1;
        public const int HeaderBytes = 32;

        /// <summary>FNV-1a, 64 bit. Used for the state hash and the tuning hash.</summary>
        public static ulong Fnv1a(ReadOnlySpan<byte> data, ulong hash = 14695981039346656037UL)
        {
            foreach (byte b in data) { hash ^= b; hash *= 1099511628211UL; }
            return hash;
        }
    }

    /// <summary>Writes little-endian values into a span. No allocations.</summary>
    ref struct SnapshotWriter
    {
        readonly Span<byte> _b;
        public int Length;

        public SnapshotWriter(Span<byte> buffer) { _b = buffer; Length = 0; }

        public void I32(int v) { BinaryPrimitives.WriteInt32LittleEndian(_b.Slice(Length), v); Length += 4; }
        public void U32(uint v) { BinaryPrimitives.WriteUInt32LittleEndian(_b.Slice(Length), v); Length += 4; }
        public void I64(long v) { BinaryPrimitives.WriteInt64LittleEndian(_b.Slice(Length), v); Length += 8; }
        public void U64(ulong v) { BinaryPrimitives.WriteUInt64LittleEndian(_b.Slice(Length), v); Length += 8; }
        public void F32(float v) => I32(BitConverter.SingleToInt32Bits(v));
        public void F64(double v) => I64(BitConverter.DoubleToInt64Bits(v));
        public void V3(Vec3 v) { F64(v.X); F64(v.Y); F64(v.Z); }
    }

    /// <summary>Reads what <see cref="SnapshotWriter"/> wrote. The caller checks the length first.</summary>
    ref struct SnapshotReader
    {
        readonly ReadOnlySpan<byte> _b;
        int _p;

        public SnapshotReader(ReadOnlySpan<byte> buffer) { _b = buffer; _p = 0; }

        public int I32() { int v = BinaryPrimitives.ReadInt32LittleEndian(_b.Slice(_p)); _p += 4; return v; }
        public uint U32() { uint v = BinaryPrimitives.ReadUInt32LittleEndian(_b.Slice(_p)); _p += 4; return v; }
        public long I64() { long v = BinaryPrimitives.ReadInt64LittleEndian(_b.Slice(_p)); _p += 8; return v; }
        public ulong U64() { ulong v = BinaryPrimitives.ReadUInt64LittleEndian(_b.Slice(_p)); _p += 8; return v; }
        public float F32() => BitConverter.Int32BitsToSingle(I32());
        public double F64() => BitConverter.Int64BitsToDouble(I64());
        public Vec3 V3() => new Vec3(F64(), F64(), F64());
    }
}
