﻿using System.IO;

namespace Emotions.KinectTools
{
    public interface IStreamable
    {
        void ToStream(BinaryWriter writer);
        void FromStream(BinaryReader reader);
    }
}
