using MiniVNCClient.Data;
using MiniVNCClient.Data.RectangleEncodings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MiniVNCClient.Processors
{
    internal class CursorProcessor
    {
        public static void Process(CursorData cursorData, int clientBytesPerPixel)
        {
            var cursorBytesPerPixel = 4;

            switch (cursorData)
            {
                case CursorRectangleData cursor:
                    {
                        if (cursor.Width == 0 || cursor.Height == 0)
                        {
                            break;
                        }

                        var bitStride = (cursor.Width + 7) / 8 * 8;
                        var bitMask = cursor.BitMask
                            !.SelectMany(b =>
                                Enumerable.Range(0, 8)
                                .Select(i => ((b >> (8 - (i + 1))) & 0b00000001) == 1)
                            )
                            .ToArray();

                        var pixelData = new byte[cursor.Width * cursor.Height * cursorBytesPerPixel];

                        var width = cursor.Width * cursorBytesPerPixel;
                        var height = cursor.Height * cursorBytesPerPixel;

                        for (int y = 0; y < cursor.Height; y++)
                        {
                            for (int x = 0; x < cursor.Width; x++)
                            {
                                cursor.PixelData
                                    .AsSpan()
                                    .Slice(start: (y * cursor.Width + x) * clientBytesPerPixel, length: clientBytesPerPixel)
                                    .CopyTo(
                                        pixelData.AsSpan()
                                        .Slice(start: (y * cursor.Width + x) * cursorBytesPerPixel, length: cursorBytesPerPixel)
                                    );

                                pixelData[(y * cursor.Width + x) * cursorBytesPerPixel + 3] = (byte)(bitMask[y * bitStride + x] ? 0xff : 0x00);
                            }
                        }

                        cursor.PixelData = pixelData;
                    }
                    break;
                case CursorWithAlphaRectangleData cursor:
                    {
                        var pixelData = new byte[cursor.Width * cursor.Height * cursorBytesPerPixel];
                        var pixelDataHandler = GCHandle.Alloc(pixelData, GCHandleType.Pinned);

                        cursor.ProcessRectangle!(
                            pixelDataHandler.AddrOfPinnedObject(),
                            pixelData.Length,
                            cursor.Width * cursorBytesPerPixel,
                            new RectangleInfo()
                            {
                                X = 0,
                                Y = 0,
                                Width = (ushort)cursor.Width,
                                Height = (ushort)cursor.Height,
                                Encoding = cursor.Encoding
                            },
                            cursor.CursorData!,
                            cursorBytesPerPixel,
                            32
                        );

                        cursor.PixelData = pixelData;
                        pixelDataHandler.Free();
                    }
                    break;
                case XCursorRectangleData cursor:
                    {
                        if (cursor.Width == 0 || cursor.Height == 0)
                        {
                            break;
                        }

                        var pixelData = new byte[cursor.Width * cursor.Height * cursorBytesPerPixel];

                        var bitStride = (cursor.Width + 7) / 8 * 8;

                        var bitMask = cursor.BitMask!
                            .SelectMany(b =>
                                Enumerable.Range(0, 8)
                                .Select(i => ((b >> (8 - (i + 1))) & 0b00000001) == 1)
                            )
                            .ToArray();

                        var bitMap = cursor.BitMap!
                            .SelectMany(b =>
                                Enumerable.Range(0, 8)
                                .Select(i => ((b >> (8 - (i + 1))) & 0b00000001) == 1)
                            )
                            .ToArray();

                        byte[][] colors = [
                            [ cursor.PrimaryColor.Blue, cursor.PrimaryColor.Green, cursor.PrimaryColor.Red ],
                                [ cursor.SecondaryColor.Blue, cursor.SecondaryColor.Green, cursor.SecondaryColor.Red ]
                        ];

                        for (int y = 0; y < cursor.Height; y++)
                        {
                            for (int x = 0; x < cursor.Width; x++)
                            {
                                colors[bitMap[y * bitStride + x] ? 0 : 1]
                                    .AsSpan()
                                    .CopyTo(
                                        pixelData.AsSpan()
                                        .Slice(start: (y * cursor.Width + x) * cursorBytesPerPixel, length: cursorBytesPerPixel)
                                    );

                                pixelData[(y * cursor.Width + x) * cursorBytesPerPixel + 3] = (byte)(bitMask[y * bitStride + x] ? 0xff : 0x00);
                            }
                        }

                        cursor.PixelData = pixelData;
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
