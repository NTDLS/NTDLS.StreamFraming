using NTDLS.StreamFraming.Payloads;

namespace TestHarness.Payloads
{
    internal class MyMessage : IFramePayloadNotification
    {
        public string Text { get; set; }

        public MyMessage(string text)
        {
            Text = text;
        }
    }
}
