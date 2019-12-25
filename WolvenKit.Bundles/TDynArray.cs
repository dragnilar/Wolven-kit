﻿using System;
using System.Collections.Generic;
using System.IO;

namespace WolvenKit.Bundles
{
    internal class TDynArray<T> : List<T>, ISerializable where T : ISerializable, new()
    {
        public void Deserialize(BinaryReader reader)
        {
            Clear();
            var count = reader.ReadVLQInt32();
            if (count == 0)
                return;
            for (var i = 0; i < count; i++)
            {
                var item = new T();
                item.Deserialize(reader);
                Add(item);
            }

            Console.WriteLine(new T().GetType().Name + " - Reader is at: " + reader.BaseStream.Position + "[0x" +
                              reader.BaseStream.Position.ToString("X") + "] left: " +
                              ((int) reader.BaseStream.Length - reader.BaseStream.Position) + "[0x" +
                              ((int) reader.BaseStream.Length - reader.BaseStream.Position).ToString("X") + "]");
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.WriteVLQInt32(Count);
            if (Count == 0)
                return;
            foreach (var item in this) item.Serialize(writer);
        }
    }

    public static class brext
    {
        public static int ReadVLQInt32(this BinaryReader br)
        {
            var b1 = br.ReadByte();
            var sign = (b1 & 128) == 128;
            var next = (b1 & 64) == 64;
            var size = b1 % 128 % 64;
            var offset = 6;
            while (next)
            {
                var b = br.ReadByte();
                size = ((b % 128) << offset) | size;
                next = (b & 128) == 128;
                offset += 7;
            }

            return sign ? size * -1 : size;
        }

        public static void WriteVLQInt32(this BinaryWriter bw, int value)
        {
            var negative = value < 0;
            value = Math.Abs(value);
            var b = (byte) (value & 0x3F);
            value >>= 6;
            if (negative) b |= 0x80;
            var cont = value != 0;
            if (cont) b |= 0x40;
            bw.Write(b);
            while (cont)
            {
                b = (byte) (value & 0x7F);
                value >>= 7;
                cont = value != 0;
                if (cont) b |= 0x80;
                bw.Write(b);
            }
        }
    }
}