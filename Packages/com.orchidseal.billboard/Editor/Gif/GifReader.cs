// #define STOPWATCH_ON
using System;
using System.IO;

#if UNITY_ASSERTIONS
using Debug = UnityEngine.Debug;
#else
using Debug = System.Diagnostics.Debug;
#endif

namespace OrchidSeal.Billboard.Gif
{
    public class FileFormatException : Exception
    {
        public FileFormatException() {}
        public FileFormatException(string message) : base(message) {}
        public FileFormatException(string message, Exception inner) : base(message, inner) {}
    }
    
    /// <summary>
    /// Reads a GIF file.
    /// </summary>
    /// <see href="https://www.w3.org/Graphics/GIF/spec-gif89a.txt">GIF89a specification</see>
    public class GifReader : IDisposable
    {
        public int FrameCount { get; private set; }
        public int FrameDelayMilliseconds { get; private set; }
        public bool HasAlpha { get; private set; }
        public int Height { get; private set; }
        public int RepeatCount { get; private set; }
        public int Width { get; private set; }

        private byte[] activeColorTable;
        private byte[] applicationExtensionAuthCode = new byte[3];
        private int backgroundColorIndex;
        private BinaryReader binaryReader;
        private CodeEntry[] codeEntries = new CodeEntry[4096];
        private DisposalMethod disposalMethod;
        private int drawPixelIndex;
        private long fileStartOffset;
        private byte[][] frameBuffers = new byte[2][];
        private byte[] globalColorTable = new byte[3 * 256];
        private int globalColorTableCount;
        private bool hasGraphicControlExtension;
        private byte[] imageBlock = new byte[256];
        private uint imageBlockBits;
        private byte imageBlockBitsLeft;
        private int imageBlockByteIndex;
        private byte imageBlockSize;
        private int imageHeight;
        private int imageLeftPosition;
        private int imageTopPosition;
        private int imageWidth;
        private bool isInterlaced;
        private byte[] localColorTable = new byte[3 * 256];
        private int transparentColorIndex;
        
        private struct CodeEntry
        {
            public short Rest { get; set; }
            public byte Last { get; set; }
        }

        private enum DisposalMethod
        {
            Unspecified = 0,
            DoNotDispose = 1,
            RestoreToBackground = 2,
            RestoreToPrevious = 3,
        }

        public static GifReader FromFile(string path)
        {
            var hadError = true;
            FileStream fileStream = null;
            try
            {
                #if STOPWATCH_ON
                var stopWatch = System.Diagnostics.Stopwatch.StartNew();
                #endif
                
                fileStream = File.OpenRead(path);
                var binaryReader = new BinaryReader(fileStream);
                var gifReader = new GifReader(binaryReader);
                gifReader.ReadFileStart();
                gifReader.CheckFormat();
                gifReader.ResetToDraw();
                hadError = false;
                
                #if STOPWATCH_ON
                stopWatch.Stop();
                UnityEngine.Debug.Log($"GifReader: Check format time {(stopWatch.ElapsedTicks * 1000000000 / System.Diagnostics.Stopwatch.Frequency):n0}ns");
                #endif
                
                return gifReader;
            }
            finally
            {
                if (hadError) fileStream?.Dispose();
            }
        }
        
        private GifReader(BinaryReader binaryReader)
        {
            this.binaryReader = binaryReader;
        }

        private void CheckExtension()
        {
            var label = ReadByte();

            switch (label)
            {
                // Plain text
                case 0x01:
                    SkipPlainTextExtension();
                    break;
                // Graphic
                case 0xF9:
                    CheckGraphicControlExtension();
                    hasGraphicControlExtension = true;
                    break;
                // Comment
                case 0xFE:
                    SkipDataSubBlocks();
                    break;
                // Application
                case 0xFF:
                    SkipApplicationExtension();
                    break;
            }
        }

        private void CheckFormat()
        {
            while (true)
            {
                var blockType = ReadByte();
                switch (blockType)
                {
                    // Extension
                    case 0x21:
                        CheckExtension();
                        break;
                    // Image
                    case 0x2C:
                        SkipImage();
                        FrameCount++;
                        break;
                    // Trailer
                    case 0x3B:
                        return;
                    default:
                        throw new FileFormatException($"Found an invalid block type. {blockType}");
                }
            }
        }

        private void CheckGraphicControlExtension()
        {
            var blockSize = ReadByte();
            if (blockSize != 4) throw new FileFormatException($"Graphic control extension has an invalid block size.");
            var packedFields = ReadByte();
            if (FrameCount == 0) HasAlpha = (packedFields & 0b0000_0001) != 0;
            SeekRelative(3);
            var blockTerminator = ReadByte();
            if (blockTerminator != 0) throw new FileFormatException($"Graphic control extension had no block terminator.");
        }
        
        // From https://github.com/lecram/gifdec
        private static int Deinterlace(int h, int y)
        {
            var p = (h - 1) / 8 + 1;
            if (y < p) return y * 8;
            
            y -= p;
            p = (h - 5) / 8 + 1;
            if (y < p) return y * 8 + 4;
            
            y -= p;
            p = (h - 3) / 4 + 1;
            if (y < p) return y * 4 + 2;
            
            y -= p;
            return y * 2 + 1;
        }

        public void Dispose()
        {
            binaryReader?.Dispose();
        }

        private void DisposeImage()
        {
            switch (disposalMethod)
            {
                default:
                case DisposalMethod.Unspecified:
                case DisposalMethod.DoNotDispose:
                    Buffer.BlockCopy(frameBuffers[0], 0, frameBuffers[1], 0, frameBuffers[1].Length);
                    break;
                case DisposalMethod.RestoreToBackground:
                {
                    Buffer.BlockCopy(frameBuffers[0], 0, frameBuffers[1], 0, frameBuffers[1].Length);

                    Debug.Assert(globalColorTableCount > 0);

                    byte r, g, b, a;
                    if (transparentColorIndex != -1)
                    {
                        r = 0;
                        g = 0;
                        b = 0;
                        a = 0;
                    }
                    else
                    {
                        var colorByteIndex = 3 * backgroundColorIndex;
                        r = globalColorTable[colorByteIndex];
                        g = globalColorTable[colorByteIndex + 1];
                        b = globalColorTable[colorByteIndex + 2];
                        a = 0xff;
                    }
                    
                    var frameColors = frameBuffers[0];
                    var startY = Height - 1 - imageTopPosition - (imageHeight - 1);
                    var imageBottomPosition = startY + imageHeight;
                    var imageRightPosition = imageLeftPosition + imageWidth;
                    
                    for (var y = startY; y < imageBottomPosition; y++)
                    {
                        for (var x = imageLeftPosition; x < imageRightPosition; x++)
                        {
                            var i = 4 * (y * Width + x);
                            frameColors[i] = r;
                            frameColors[i + 1] = g;
                            frameColors[i + 2] = b;
                            frameColors[i + 3] = a;
                        }
                    }
                    break;
                }
                case DisposalMethod.RestoreToPrevious:
                {
                    var frameColors = frameBuffers[0];
                    var priorFrameColors = frameBuffers[1];
                    var imageBottomPosition = imageTopPosition + imageHeight;
                    var imageRightPosition = imageLeftPosition + imageWidth;
                    
                    for (var y = imageTopPosition; y < imageBottomPosition; y++)
                    {
                        for (var x = imageLeftPosition; x < imageRightPosition; x++)
                        {
                            var i = 4 * (y * Width + x);
                            frameColors[i] = priorFrameColors[i];
                            frameColors[i + 1] = priorFrameColors[i + 1];
                            frameColors[i + 2] = priorFrameColors[i + 2];
                            frameColors[i + 3] = priorFrameColors[i + 3];
                        }
                    }
                    
                    Buffer.BlockCopy(frameBuffers[0], 0, frameBuffers[1], 0, frameBuffers[1].Length);
                    break;
                }
            }
        }
        
        private byte FindFirstEntry(int c)
        {
            byte last;
            do
            {
                last = codeEntries[c].Last;
                c = codeEntries[c].Rest;
            } while (c != -1);
            return last;
        }
        
        public byte[] GetNextFrame()
        {
            while (true)
            {
                var blockType = ReadByte();
                switch (blockType)
                {
                    // Extension
                    case 0x21:
                        if (hasGraphicControlExtension) throw new FileFormatException("Image must follow a graphic control extension.");
                        ReadExtension();
                        break;
                    // Image
                    case 0x2C:
                    {
                        DisposeImage();
                        DrawImage();
                        ResetGraphicControls();
                        return frameBuffers[0];
                    }
                    // Trailer
                    case 0x3B:
                        return frameBuffers[0];
                    default:
                        throw new FileFormatException($"Found an invalid block type. {blockType}");
                }
            }
        }
        
        private static bool IsPowerOfTwo(ulong x)
        {
            return (x & (x - 1)) == 0;
        }

        private void ReadApplicationExtension()
        {
            var blockSize = ReadByte();
            if (blockSize != 11) throw new FileFormatException($"Application extension has an invalid block size.");
            var appId = ReadString(8);
            var readCount = ReadBytes(applicationExtensionAuthCode, 0, 3);
            if (readCount != 3) throw new FileFormatException($"Application extension has an invalid auth code.");
            var c = applicationExtensionAuthCode;
            
            if (appId == "NETSCAPE" && c[0] == '2' && c[1] == '.' && c[2] == '0')
            {
                while(true)
                {
                    blockSize = ReadByte();
                    if (blockSize == 0) return;
                    var subBlockId = ReadByte();
                    switch (subBlockId)
                    {
                        // Looping Application Extension
                        // https://web.archive.org/web/20231003013710/https://www.vurdalakov.net/misc/gif/netscape-looping-application-extension
                        case 0x01:
                            RepeatCount = ReadUInt16();
                            break;
                        // Buffering Application Extension
                        // https://web.archive.org/web/20230203155325/http://www.vurdalakov.net/misc/gif/netscape-buffering-application-extension
                        // case 0x02: break;
                        default:
                            SeekRelative(blockSize - 1);
                            break;
                    }
                }
            }
            
            SkipDataSubBlocks();
        }

        private void ReadExtension()
        {
            var label = ReadByte();

            switch (label)
            {
                // Plain text
                case 0x01:
                    SkipPlainTextExtension();
                    break;
                // Graphic
                case 0xF9:
                    ReadGraphicControlExtension();
                    break;
                // Comment
                case 0xFE:
                    SkipDataSubBlocks();
                    break;
                // Application
                case 0xFF:
                    ReadApplicationExtension();
                    break;
            }
        }

        private void ReadFileStart()
        {
            // Read the Header.
            var signature = ReadString(3);
            if (signature != "GIF") throw new FileFormatException("The file signature is invalid.");
            
            var version = ReadString(3);
            if (version != "87a" && version != "89a") throw new FileFormatException("The file version is invalid.");
            
            // Read the Logical Screen Descriptor.
            Width = ReadUInt16();
            Height = ReadUInt16();
            var packedFields = ReadByte();
            backgroundColorIndex = ReadByte();
            // Ignore the Pixel Aspect Ratio.
            ReadByte();

            var hasGlobalColorTable = (packedFields & 0b1000_0000) != 0;
            // var colorResolutionBitCount = ((packedFields & 0b0111_0000) >> 4) + 1;
            // var isGlobalColorTableSorted = (packedFields & 0b0000_1000) != 0;
            globalColorTableCount = 2 << (packedFields & 0b0000_0111);

            // Read the Global Color Table.
            if (hasGlobalColorTable)
            {
                var readCount = ReadBytes(globalColorTable, 0, 3 * globalColorTableCount);
                if (readCount != 3 * globalColorTableCount) throw new FileFormatException("Failed to read the Global Color Table.");
            }
            
            fileStartOffset = binaryReader.BaseStream.Position;
        }
        
        private void ReadGraphicControlExtension()
        {
            var blockSize = ReadByte();
            if (blockSize != 4) throw new FileFormatException($"Graphic control extension has an invalid block size.");
            
            var packedFields = ReadByte();
            var disposalMethodByte = (packedFields & 0b0001_1100) >> 2;
            if (disposalMethodByte > 3) throw new FileFormatException($"Graphic control extension has an invalid disposal method.");
            disposalMethod = (DisposalMethod) disposalMethodByte;
            // var isUserInputExpected = (packedFields & 0b0000_0010) != 0;
            var hasTransparentColorIndex = (packedFields & 0b0000_0001) != 0;

            FrameDelayMilliseconds = 10 * ReadUInt16();
            var transparentColorIndexByte = ReadByte();
            transparentColorIndex = hasTransparentColorIndex ? transparentColorIndexByte : -1;
            
            var blockTerminator = ReadByte();
            if (blockTerminator != 0) throw new FileFormatException($"Graphic control extension had no block terminator.");

            hasGraphicControlExtension = true;
        }

        private void DrawPixels(int c)
        {
            // Measure how far to backtrack.
            var i = 0;
            var rest = c;
            while (rest != -1)
            {
                rest = codeEntries[rest].Rest;
                i++;
            }
            
            // Draw backwards
            var j = drawPixelIndex + i - 1;
            if (j >= imageWidth * imageHeight) throw new FileFormatException("Decoded image data doesn't match the image size.");
            rest = c;
            var frameColors = frameBuffers[0];

            if (isInterlaced)
            {
                while (rest != -1)
                {
                    var colorIndex = codeEntries[rest].Last;
                    var colorByteIndex = 3 * colorIndex;
                    var x = j % imageWidth + imageLeftPosition;
                    var y = Height - 1 - Deinterlace(imageHeight, j / imageWidth) - imageTopPosition;
                    var drawByteIndex = 4 * (y  * Width + x);
                    Debug.Assert(x >= 0 && x < Width && y >= 0 && y < Height);

                    if (colorIndex != transparentColorIndex)
                    {
                        frameColors[drawByteIndex] = activeColorTable[colorByteIndex];
                        frameColors[drawByteIndex + 1] = activeColorTable[colorByteIndex + 1];
                        frameColors[drawByteIndex + 2] = activeColorTable[colorByteIndex + 2];
                        frameColors[drawByteIndex + 3] = 0xff;
                    }
                
                    j--;
                    rest = codeEntries[rest].Rest;
                }    
            }
            else
            {
                while (rest != -1)
                {
                    var colorIndex = codeEntries[rest].Last;
                    var colorByteIndex = 3 * colorIndex;
                    var x = j % imageWidth + imageLeftPosition;
                    var y = Height - 1 - j / imageWidth - imageTopPosition;
                    var drawByteIndex = 4 * (y  * Width + x);
                    Debug.Assert(x >= 0 && x < Width && y >= 0 && y < Height);

                    if (colorIndex != transparentColorIndex)
                    {
                        frameColors[drawByteIndex] = activeColorTable[colorByteIndex];
                        frameColors[drawByteIndex + 1] = activeColorTable[colorByteIndex + 1];
                        frameColors[drawByteIndex + 2] = activeColorTable[colorByteIndex + 2];
                        frameColors[drawByteIndex + 3] = 0xff;
                    }
                
                    j--;
                    rest = codeEntries[rest].Rest;
                }
            }
            
            drawPixelIndex = j + i + 1;
        }

        private int ReadBlockBits(byte bitCount)
        {
            Debug.Assert(bitCount >= 3 && bitCount <= 12);

            while (imageBlockBitsLeft < bitCount)
            {
                if (imageBlockByteIndex >= imageBlockSize)
                {
                    imageBlockSize = binaryReader.ReadByte();
                    if (imageBlockSize == 0) return 0;
                    var readCount = binaryReader.Read(imageBlock, 0, imageBlockSize);
                    if (readCount != imageBlockSize) throw new FileFormatException("Missing image data in a block.");
                    imageBlockByteIndex = 0;
                }
                
                imageBlockBits = ((uint) imageBlock[imageBlockByteIndex] << imageBlockBitsLeft) | imageBlockBits;
                imageBlockByteIndex++;
                imageBlockBitsLeft += 8;
            }
            
            Debug.Assert(imageBlockBits <= int.MaxValue);
            var result = ((1 << bitCount) - 1) & (int) imageBlockBits;
            imageBlockBits >>= bitCount;
            imageBlockBitsLeft -= bitCount;

            return result;
        }
        
        // This was based on technoblogy's Minimal GIF Decoder.
        // https://github.com/technoblogy/minimal-gif-decoder/blob/main/minimal-gif-decoder-flash.ino
        private void DrawImage()
        {
            // Read the Image Descriptor.
            imageLeftPosition = ReadUInt16();
            imageTopPosition = ReadUInt16();
            imageWidth = ReadUInt16();
            imageHeight = ReadUInt16();
            if (imageLeftPosition + imageWidth > Width || imageTopPosition + imageHeight > Height)
            {
                throw new FileFormatException("Image position is out of bounds.");
            }
            
            var packedFields = ReadByte();
            
            var hasLocalColorTable = (packedFields & 0b1000_0000) != 0;
            isInterlaced = (packedFields & 0b0100_0000) != 0;
            
            // var isLocalColorTableSorted = (packedFields & 0b0010_0000) != 0;
            var localColorTableCount = 2 << (packedFields & 0b0000_0111);

            // Read the Local Color Table.
            
            if (hasLocalColorTable)
            {
                var readCount = ReadBytes(localColorTable, 0, 3 * localColorTableCount);
                if (readCount != 3 * localColorTableCount) throw new FileFormatException("Failed to read the Local Color Table.");
                activeColorTable = localColorTable;
            }
            else if (globalColorTable != null)
            {
                activeColorTable = globalColorTable;
            }
            else if(activeColorTable != null)
            {
                throw new FileFormatException("Missing a color table to read the image data.");
            }
            
            // Read and draw the Image Data.
            int lzwMinimumCodeSize = ReadByte();
            if (lzwMinimumCodeSize < 2 || lzwMinimumCodeSize > 8) throw new FileFormatException("Image data has an invalid LZW minimum code size.");
            
            var codeCount = 1 << lzwMinimumCodeSize;
            var clearCode = codeCount;
            var endCode = codeCount + 1;
            var availableCode = codeCount + 2;
            
            for (var c = 0; c < clearCode; c++)
            {
                codeEntries[c].Rest = -1;
                codeEntries[c].Last = (byte) c;
            }

            imageBlockBits = 0;
            imageBlockBitsLeft = 0;
            imageBlockByteIndex = 0;
            imageBlockSize = 0;
            drawPixelIndex = 0;
            var code = -1;
            var codeSize = (byte) (lzwMinimumCodeSize + 1);
            
            while(true)
            {
                var priorCode = code;
                code = ReadBlockBits(codeSize);
                Debug.Assert(code >= 0 && code < 4096);

                if (code == clearCode)
                {
                    availableCode = codeCount + 2;
                    codeSize = (byte) (lzwMinimumCodeSize + 1);
                    code = -1;
                }
                else if (code == endCode)
                {
                    break;
                }
                else if (priorCode == -1)
                {
                    DrawPixels(code);
                }
                else if (code <= availableCode)
                {
                    if (availableCode < 4096)
                    {
                        codeEntries[availableCode].Rest = (short) priorCode;
                        codeEntries[availableCode].Last = FindFirstEntry(code == availableCode ? priorCode : code);
                        availableCode++;
                        if (IsPowerOfTwo((ulong) availableCode) && (availableCode < 4096)) codeSize++;
                    }
                    
                    DrawPixels(code);
                }
            }

            if (ReadByte() != 0) throw new FileFormatException("Image data has no block terminator.");
            if (drawPixelIndex != imageWidth * imageHeight) throw new FileFormatException("Decoded image data doesn't match the image size.");
        }

        private void ResetGraphicControls()
        {
            hasGraphicControlExtension = false;
            disposalMethod = DisposalMethod.Unspecified;
            FrameDelayMilliseconds = 100;
            transparentColorIndex = -1;
        }

        private void ResetToDraw()
        {
            frameBuffers[0] = new byte[4 * Width * Height];
            frameBuffers[1] = new byte[4 * Width * Height];
            SeekBeginning(fileStartOffset);
            ResetGraphicControls();
        }
        
        private void SkipApplicationExtension()
        {
            var blockSize = ReadByte();
            if (blockSize != 11) throw new FileFormatException($"Application extension has an invalid block size.");
            SeekRelative(11);
            SkipDataSubBlocks();
        }

        private void SkipDataSubBlocks()
        {
            while(true)
            {
                var blockSize = ReadByte();
                if (blockSize == 0) return;
                SeekRelative(blockSize);
            }
        }
        
        private void SkipImage()
        {
            SeekRelative(4);
            var imageSize = ReadUInt16() * ReadUInt16();
            var packedFields = ReadByte();
            var hasLocalColorTable = (packedFields & 0b1000_0000) != 0;
            var localColorTableCount = 2 << (packedFields & 0b0000_0111);
            if (hasLocalColorTable) SeekRelative(3 * localColorTableCount);
            ReadByte();
            SkipDataSubBlocks();
        }

        private void SkipPlainTextExtension()
        {
            var blockSize = ReadByte();
            if (blockSize != 12) throw new FileFormatException($"Plain text extension has an invalid block size.");
            SeekRelative(12);
            SkipDataSubBlocks();
        }

        private byte ReadByte()
        {
            return binaryReader.ReadByte();
        }

        private int ReadBytes(byte[] values, int index, int count)
        {
            return binaryReader.Read(values, index, count);
        }
        
        private string ReadString(int length)
        {
            return System.Text.Encoding.ASCII.GetString(binaryReader.ReadBytes(length));
        }

        private ushort ReadUInt16()
        {
            return binaryReader.ReadUInt16();
        }

        private void SeekBeginning(long offset)
        {
            binaryReader.BaseStream.Seek(offset, SeekOrigin.Begin);
        }

        private void SeekRelative(long offset)
        {
            binaryReader.BaseStream.Seek(offset, SeekOrigin.Current);
        }
    }
}
