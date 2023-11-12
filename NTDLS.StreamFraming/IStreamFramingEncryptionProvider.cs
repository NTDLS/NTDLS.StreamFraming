namespace NTDLS.StreamFraming
{
    /// <summary>
    /// Use to provide manipulate the payload bytes after they are compressed but before they are framed.
    /// </summary>
    public interface IStreamFramingEncryptionProvider
    {
        /// <summary>
        /// Encrypt the frame payload before it is sent.
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        public byte[] EncryptPayload(byte[] payload);

        /// <summary>
        /// Decrypt the frame payload after it is received.
        /// </summary>
        /// <param name="encryptedPayload"></param>
        /// <returns></returns>
        public byte[] DecryptPayload(byte[] encryptedPayload);
    }
}
