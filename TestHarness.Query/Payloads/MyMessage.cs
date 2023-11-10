using NTDLS.StreamFraming.Payloads;

namespace TestHarness.Payloads
{
    internal class MyMessage : IFrameNotification
    {
        public string Text { get; set; }

        public MyMessage(string text)
        {
            Text = text;
        }
    }
}
