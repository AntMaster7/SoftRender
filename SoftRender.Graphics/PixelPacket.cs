using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace SoftRender
{
    public class PixelPacket : PointPacket
    {
        public Vector256<int> Rs { get; set; }

        public Vector256<int> Gs { get; set; }

        public Vector256<int> Bs { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector256<byte> ConvertToVector256Int8WithTruncation(Vector256<int> value)
        {
            var truncated = Avx2.Shuffle(value.AsByte(), Vector256.Create(
                0, 4, 8, 12, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0, 4, 8, 12, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF));
            
            return Avx2.PermuteVar8x32(truncated.AsInt32(), Vector256.Create(0, 4, 5, 5, 5, 5, 5, 5)).AsByte();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void StoreInterleaved(byte* rgb, Vector256<int> mask)
        {
            var bbrr = Avx.Shuffle(Bs.AsSingle(), Rs.AsSingle(), 0b01111000); // B0 B2 R3 R1 | B4 B6 R7 R5
            var ggrr = Avx.Shuffle(Gs.AsSingle(), Rs.AsSingle(), 0b10000010); // G2 G0 R0 R2 | G6 G4 R4 R6
            var rgbr = Avx.Shuffle(ggrr, bbrr, 0b11000110);                   // R0 G0 B0 R1 | R4 G4 B4 R5
            var ggbb = Avx.Shuffle(Gs.AsSingle(), Bs.AsSingle(), 0b11011101); // G1 G3 B1 B3 | G5 G7 B5 B7
            var gbrg = Avx.Shuffle(ggbb, ggrr, 0b00111000);                   // G1 B1 R2 G2 | G5 B5 R6 G6
            var brgb = Avx.Shuffle(bbrr, ggbb, 0b11011001);                   // B2 R3 G3 B3 | B6 R7 G7 B7

            var rgbr0 = Avx.ExtractVector128(rgbr, 0).AsInt32();
            var gbrg0 = Avx.ExtractVector128(gbrg, 0).AsInt32();
            var brgb0 = Avx.ExtractVector128(brgb, 0).AsInt32();
            var rgbr1 = Avx.ExtractVector128(rgbr, 1).AsInt32();
            var gbrg1 = Avx.ExtractVector128(gbrg, 1).AsInt32();
            var brgb1 = Avx.ExtractVector128(brgb, 1).AsInt32();

            var rs = ConvertToVector256Int8WithTruncation(Vector256.Create(rgbr0, gbrg0));
            var gs = ConvertToVector256Int8WithTruncation(Vector256.Create(brgb0, rgbr1));
            var bs = ConvertToVector256Int8WithTruncation(Vector256.Create(gbrg1, brgb1));

            var ssgs = Avx2.Shuffle(gs.AsInt32(), 1 << 2 | 1 << 6);
            var rsgs = Avx2.BlendVariable(rs.AsInt32(), ssgs.AsInt32(), Vector256.Create(0, 0, -1, -1, 0, 0, 0, 0));

            var moveMasks = Avx2.Shuffle(mask.AsByte(), Vector256.Create(
                0, 0, 0, 4, 4, 4, 8, 8, 8, 12, 12, 12, 0xFF, 0xFF, 0xFF, 0xFF,
                0, 0, 0, 4, 4, 4, 8, 8, 8, 12, 12, 12, 0xFF, 0xFF, 0xFF, 0xFF));

            var moveMask0 = moveMasks.GetLower().AsByte();
            Sse2.MaskMove(rsgs.GetLower().AsByte(), moveMask0, rgb);

            var moveMask1 = Avx2.ExtractVector128(moveMasks.AsByte(), 1);
            var gsbs = Avx.Shuffle(rsgs.AsSingle(), bs.AsSingle(), 0b01000011);
            var gsbs0 = Sse2.Shuffle(gsbs.GetLower().AsInt32(), 0b00111000);
            Sse2.MaskMove(gsbs0.AsByte(), moveMask1, rgb + 12);
        }
    }
}
