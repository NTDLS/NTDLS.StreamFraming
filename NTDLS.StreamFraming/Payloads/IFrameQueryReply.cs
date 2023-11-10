namespace NTDLS.StreamFraming.Payloads
{
    /// <summary>
    /// All querry reply frames must in herit from this interface and be json serializable.
    /// </summary>
    public interface IFrameQueryReply : IStreamFrame
    {
    }
}
