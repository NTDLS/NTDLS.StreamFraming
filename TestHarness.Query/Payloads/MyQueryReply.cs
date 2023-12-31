﻿using NTDLS.StreamFraming.Payloads;

namespace TestHarness.Payloads
{
    internal class MyQueryReply : IFramePayloadQueryReply
    {
        public string Text { get; set; }

        public MyQueryReply(string text)
        {
            Text = text;
        }
    }
}
