using NTDLS.StreamFraming.Payloads;

namespace TestHarness.Payloads
{
    internal class MyQuery : IFrameQuery
    {
        public string Text { get; set; }

        public MyQuery(string text)
        {
            Text = text;
        }
    }
}
