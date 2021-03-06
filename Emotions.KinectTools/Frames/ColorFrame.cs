﻿using System;
using System.IO;
using System.Windows;
using Microsoft.Kinect;

namespace Emotions.KinectTools.Frames
{
    public class ColorFrame : IFrame
    {
        public int FrameNumber { get; private set; }
        public long TimeStamp { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int BytesPerPixel { get; private set; }
        public ColorImageFormat Format { get; private set; }
        public int PixelDataLength { get; private set; }
        public byte[] Data { get; private set; }

        public ColorFrame(ColorImageFrame frame)
        {
            Format = frame.Format;
            BytesPerPixel = frame.BytesPerPixel;
            FrameNumber = frame.FrameNumber;
            TimeStamp = frame.Timestamp;
            Width = frame.Width;
            Height = frame.Height;
            PixelDataLength = frame.PixelDataLength;
            Data = new byte[frame.PixelDataLength];
            frame.CopyPixelDataTo(Data);
        }

        public ColorFrame()
        {
            FrameNumber = 0;
        }

        public ColorFrame(byte[] data, ColorImageFormat format, int frameNumber, long timeStamp)
        {
            Data = From24bbpTo32bpp(data);
            Format = format;
            FrameNumber = frameNumber;
            TimeStamp = timeStamp;

            switch (format)
            {
                case ColorImageFormat.RgbResolution640x480Fps30:
                    Width = 640;
                    Height = 480;
                    BytesPerPixel = 4; // 32bbp
                    break;
                case ColorImageFormat.RgbResolution1280x960Fps12:
                    Width = 1280;
                    Height = 960;
                    BytesPerPixel = 4; // 32bbp
                    break;
                case ColorImageFormat.Undefined:
                    throw new Exception("Format is not defined");
                default:
                    throw new Exception("Unsupported");
            }
            
            PixelDataLength = Width*Height*BytesPerPixel;

            if(Data.Length != PixelDataLength)
                throw new Exception("Data.Length != PixelDataLength.");
        }

        public void ToStream(BinaryWriter writer)
        {
            writer.Write(FrameNumber);
            writer.Write(TimeStamp);
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(BytesPerPixel);
            writer.Write((int) Format);
            writer.Write(PixelDataLength);
            writer.Write(Data);
        }

        public void FromStream(BinaryReader reader)
        {
            FrameNumber = reader.ReadInt32();
            TimeStamp = reader.ReadInt64();
            Width = reader.ReadInt32();
            Height = reader.ReadInt32();
            BytesPerPixel = reader.ReadInt32();
            Format = (ColorImageFormat)reader.ReadInt32();
            PixelDataLength = reader.ReadInt32();
            Data = reader.ReadBytes(PixelDataLength);
        }

        public byte[] From24bbpTo32bpp(byte[] source)
        {
            var pixelLen = (source.Length/3);
            var bytes = new byte[pixelLen * 4]; 
            for (var pixelIndex = 0; pixelIndex < pixelLen; pixelIndex++)
            {
                bytes[4 * pixelIndex + 0] = source[3 * pixelIndex + 0];
                bytes[4 * pixelIndex + 1] = source[3 * pixelIndex + 1];
                bytes[4 * pixelIndex + 2] = source[3 * pixelIndex + 2];
                bytes[4 * pixelIndex + 3] = 0;
            }
            return bytes;
        }

        public int GetOffset(int x, int y)
        {
            return (y*Width + x) * BytesPerPixel;
        }

        public byte[] GetImageData(int x, int y, int width, int height)
        {
            var data = new byte[width*height*BytesPerPixel];
            var idx = 0;
            for (var j = y; j < y + height; j++)
            for (var i = x; i < x + width; i++)
            {
                var offset = GetOffset(i, j);
                data[idx++] = Data[offset + 0]; // R
                data[idx++] = Data[offset + 1]; // G
                data[idx++] = Data[offset + 2]; // B
                data[idx++] = Data[offset + 3]; // A
            }
            return data;
        }

        public void SetImageData(byte[] data, int x, int y, int width, int height)
        {
            var idx = 0;
            for (var j = y; j < y + height; j++)
            for (var i = x; i < x + width; i++)
            {
                var offset = GetOffset(i, j);
                Data[offset + 0] = data[idx++]; // R
                Data[offset + 1] = data[idx++]; // G
                Data[offset + 2] = data[idx++]; // B
                Data[offset + 3] = data[idx++]; // A
            }
        }
    }
}
