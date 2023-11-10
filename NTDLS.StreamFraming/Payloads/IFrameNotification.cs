namespace NTDLS.StreamFraming.Payloads
{
    /// <summary>
    /// All simple notifications frames must in herit from this interface and be json serializable.
    /// </summary>
    public interface IFrameNotification : IStreamFrame
    {
    }
}
