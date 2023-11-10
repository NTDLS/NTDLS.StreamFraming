﻿using Newtonsoft.Json;
using NTDLS.Semaphore;
using NTDLS.StreamFraming.Payloads;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static NTDLS.StreamFraming.Defaults;

namespace NTDLS.StreamFraming
{
    /// <summary>
    /// Stream packets (especially TCP/IP) can be fragmented or combined. Framing rebuilds what was
    /// originally written to the stream while also providing compression, CRC checking and optional encryption.
    /// </summary>
    public static class Framing
    {
        /// <summary>
        /// The callback that is used to notify of the receipt of a notification frame.
        /// </summary>
        /// <param name="payload">The notification payload.</param>
        public delegate void ProcessFrameNotificationCallback(IFrameNotification payload);

        /// <summary>
        /// The callback that is used to notify of the receipt of a query frame. A return of type IFrameQueryReply
        ///  is  expected and will be routed to the originator and the appropriate waiting asynchronous task.
        /// </summary>
        /// <param name="payload">The query payload</param>
        /// <returns>The reply payload to return to the originator.</returns>
        public delegate IFrameQueryReply ProcessFrameQueryCallback(IFrameQuery payload);

        private static readonly PessimisticSemaphore<Dictionary<string, MethodInfo>> _reflectioncache = new();
        private static readonly List<QueryAwaitingReply> _queriesAwaitingReplies = new();

        /// <summary>
        /// Delegate use to provide manipulate the payload bytes after they are compressed but before they are framed.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public delegate byte[] EncryptionProviderCallback(byte[] buffer);

        #region Extension methods.

        /// <summary>
        /// Waits on bytes to become available on the stream, reads those bytes then parses the available frames (if any) and calls the appropriate callbacks.
        /// </summary>
        /// <param name="stream">The open stream that should be read from</param>
        /// <param name="frameBuffer">The frame buffer that will be used to receive bytes from the stream.</param>
        /// <param name="processNotificationCallback">Optional callback to call when a notification frame is received.</param>
        /// <param name="processFrameQueryCallback">Optional callback to call when a query frame is received.</param>
        /// <param name="encryptionProvider">An optional callback that is called to allow for manipulation of bytes after they are received.</param>
        /// <returns>Returns true if the stream is healthy, returns false if disconnected.</returns>
        /// <exception cref="Exception"></exception>
        public static bool ReadAndProcessFrames(this Stream stream, FrameBuffer frameBuffer,
            ProcessFrameNotificationCallback? processNotificationCallback = null, ProcessFrameQueryCallback? processFrameQueryCallback = null,
            EncryptionProviderCallback? encryptionProvider = null)
        {
            if (stream == null)
            {
                throw new Exception("ReceiveAndProcessStreamFrames: stream can not be null.");
            }

            if (frameBuffer.ReadStream(stream))
            {
                ProcessFrameBuffer(stream, frameBuffer, processNotificationCallback, processFrameQueryCallback, encryptionProvider);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Writes a query to the stream, expects a reply.
        /// </summary>
        /// <typeparam name="T">The type of the expected reply payload.</typeparam>
        /// <param name="stream">The open stream that will be written to.</param>
        /// <param name="payload">The query payload that will be written to the stream.</param>
        /// <param name="queryTimeout">The number of milliseconds to wait on a reply to the query.</param>
        /// <param name="encryptionProvider">An optional callback that is called to allow for manipulation of bytes before they are framed.</param>
        /// <returns>Returns the reply payload that is written to the stream from the recipient of the query.</returns>
        /// <exception cref="Exception"></exception>
        public static async Task<T> WriteQuery<T>(this Stream stream, IFrameQuery payload, int queryTimeout = -1, EncryptionProviderCallback? encryptionProvider = null)
        {
            if (stream == null)
            {
                throw new Exception("SendStreamFramePayload stream can not be null.");
            }

            var cmd = new Frame()
            {
                EnclosedPayloadType = payload.GetType()?.AssemblyQualifiedName ?? string.Empty,
                Payload = JsonConvert.SerializeObject(payload)
            };

            var queryAwaitingReply = new QueryAwaitingReply()
            {
                FrameId = cmd.Id,
            };

            _queriesAwaitingReplies.Add(queryAwaitingReply);

            return await Task.Run(() =>
            {
                var frameBytes = AssembleFrame(cmd, encryptionProvider);
                stream.Write(frameBytes, 0, frameBytes.Length);

                //Wait for a reply. When a reply is received, it will be routed to the correct query via ApplyQueryReply().
                //ApplyQueryReply() will apply the payload data to queryAwaitingReply and trigger the wait event.
                if (queryAwaitingReply.WaitEvent.WaitOne(queryTimeout) == false)
                {
                    _queriesAwaitingReplies.Remove(queryAwaitingReply);
                    throw new Exception("Query timeout expired while waiting on reply.");
                }

                _queriesAwaitingReplies.Remove(queryAwaitingReply);

                if (queryAwaitingReply.ReplyPayload == null)
                {
                    throw new Exception("The reply payload can not be null.");
                }

                return (T)queryAwaitingReply.ReplyPayload;
            });
        }

        /// <summary>
        /// Writes a reply to the stream in reply to a stream query.
        /// </summary>
        /// <param name="stream">The open stream that will be written to.</param>
        /// <param name="queryFrame">The query frame that was received and that we are responding to.</param>
        /// <param name="payload">The query reply payload.</param>
        /// <param name="encryptionProvider">An optional callback that is called to allow for manipulation of bytes before they are framed.</param>
        /// <exception cref="Exception"></exception>
        public static void WriteReply(this Stream stream, Frame queryFrame, IFrameQueryReply payload, EncryptionProviderCallback? encryptionProvider = null)
        {
            if (stream == null)
            {
                throw new Exception("SendStreamFramePayload stream can not be null.");
            }
            var cmd = new Frame()
            {
                Id = queryFrame.Id,
                EnclosedPayloadType = payload.GetType()?.AssemblyQualifiedName ?? string.Empty,
                Payload = JsonConvert.SerializeObject(payload)
            };

            var frameBytes = AssembleFrame(cmd, encryptionProvider);
            stream.Write(frameBytes, 0, frameBytes.Length);
        }

        /// <summary>
        /// Sends a one-time fire-and-forget notification to the stream.
        /// </summary>
        /// <param name="stream">The open stream that will be written to.</param>
        /// <param name="payload">The notification payload that will be written to the stream.</param>
        /// <param name="encryptionProvider">An optional callback that is called to allow for manipulation of bytes before they are framed.</param>
        /// <exception cref="Exception"></exception>
        public static void WriteNotification(this Stream stream, IFrameNotification payload, EncryptionProviderCallback? encryptionProvider = null)
        {
            if (stream == null)
            {
                throw new Exception("SendStreamFramePayload stream can not be null.");
            }
            var cmd = new Frame()
            {
                EnclosedPayloadType = payload.GetType()?.AssemblyQualifiedName ?? string.Empty,
                Payload = JsonConvert.SerializeObject(payload)
            };

            var frameBytes = AssembleFrame(cmd, encryptionProvider);
            stream.Write(frameBytes, 0, frameBytes.Length);
        }

        #endregion

        private static byte[] AssembleFrame(Frame frame, EncryptionProviderCallback? encryptionProvider)
        {
            var frameBody = Utility.SerializeToByteArray(frame);
            var frameBytes = Utility.Compress(frameBody);

            if (encryptionProvider != null)
            {
                frameBytes = encryptionProvider(frameBytes);
            }

            var grossFrameSize = frameBytes.Length + NtFrameDefaults.FRAME_HEADER_SIZE;
            var grossFrameBytes = new byte[grossFrameSize];
            var frameCrc = CRC16.ComputeChecksum(frameBytes);

            Buffer.BlockCopy(BitConverter.GetBytes(NtFrameDefaults.FRAME_DELIMITER), 0, grossFrameBytes, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(grossFrameSize), 0, grossFrameBytes, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(frameCrc), 0, grossFrameBytes, 8, 2);
            Buffer.BlockCopy(frameBytes, 0, grossFrameBytes, NtFrameDefaults.FRAME_HEADER_SIZE, frameBytes.Length);

            return grossFrameBytes;
        }

        private static void SkipFrame(ref FrameBuffer frameBuffer)
        {
            var frameDelimiterBytes = new byte[4];

            for (int offset = 1; offset < frameBuffer.FrameBuilderLength - frameDelimiterBytes.Length; offset++)
            {
                Buffer.BlockCopy(frameBuffer.FrameBuilder, offset, frameDelimiterBytes, 0, frameDelimiterBytes.Length);

                var value = BitConverter.ToInt32(frameDelimiterBytes, 0);

                if (value == NtFrameDefaults.FRAME_DELIMITER)
                {
                    Buffer.BlockCopy(frameBuffer.FrameBuilder, offset, frameBuffer.FrameBuilder, 0, frameBuffer.FrameBuilderLength - offset);
                    frameBuffer.FrameBuilderLength -= offset;
                    return;
                }
            }
            Array.Clear(frameBuffer.FrameBuilder, 0, frameBuffer.FrameBuilder.Length);
            frameBuffer.FrameBuilderLength = 0;
        }

        private static void ProcessFrameBuffer(this Stream stream, FrameBuffer frameBuffer,
            ProcessFrameNotificationCallback? processNotificationCallback,
            ProcessFrameQueryCallback? processFrameQueryCallback,
            EncryptionProviderCallback? encryptionProvider = null)
        {
            if (frameBuffer.FrameBuilderLength + frameBuffer.ReceiveBufferUsed >= frameBuffer.FrameBuilder.Length)
            {
                Array.Resize(ref frameBuffer.FrameBuilder, frameBuffer.FrameBuilderLength + frameBuffer.ReceiveBufferUsed);
            }

            Buffer.BlockCopy(frameBuffer.ReceiveBuffer, 0, frameBuffer.FrameBuilder, frameBuffer.FrameBuilderLength, frameBuffer.ReceiveBufferUsed);

            frameBuffer.FrameBuilderLength = frameBuffer.FrameBuilderLength + frameBuffer.ReceiveBufferUsed;

            while (frameBuffer.FrameBuilderLength > NtFrameDefaults.FRAME_HEADER_SIZE) //[FrameSize] and [CRC16]
            {
                var frameDelimiterBytes = new byte[4];
                var frameSizeBytes = new byte[4];
                var expectedCRC16Bytes = new byte[2];

                Buffer.BlockCopy(frameBuffer.FrameBuilder, 0, frameDelimiterBytes, 0, frameDelimiterBytes.Length);
                Buffer.BlockCopy(frameBuffer.FrameBuilder, 4, frameSizeBytes, 0, frameSizeBytes.Length);
                Buffer.BlockCopy(frameBuffer.FrameBuilder, 8, expectedCRC16Bytes, 0, expectedCRC16Bytes.Length);

                var frameDelimiter = BitConverter.ToInt32(frameDelimiterBytes, 0);
                var grossFrameSize = BitConverter.ToInt32(frameSizeBytes, 0);
                var expectedCRC16 = BitConverter.ToUInt16(expectedCRC16Bytes, 0);

                if (frameDelimiter != NtFrameDefaults.FRAME_DELIMITER || grossFrameSize < 0)
                {
                    //Possible corrupt frame.
                    SkipFrame(ref frameBuffer);
                    continue;
                }

                if (frameBuffer.FrameBuilderLength < grossFrameSize)
                {
                    //We have data in the buffer, but it's not enough to make up
                    //  the entire message so we will break and wait on more data.
                    break;
                }

                if (CRC16.ComputeChecksum(frameBuffer.FrameBuilder, NtFrameDefaults.FRAME_HEADER_SIZE, grossFrameSize - NtFrameDefaults.FRAME_HEADER_SIZE) != expectedCRC16)
                {
                    //Corrupt frame.
                    SkipFrame(ref frameBuffer);
                    continue;
                }

                var netFrameSize = grossFrameSize - NtFrameDefaults.FRAME_HEADER_SIZE;
                var frameBytes = new byte[netFrameSize];
                Buffer.BlockCopy(frameBuffer.FrameBuilder, NtFrameDefaults.FRAME_HEADER_SIZE, frameBytes, 0, netFrameSize);

                if (encryptionProvider != null)
                {
                    frameBytes = encryptionProvider(frameBytes);
                }

                var frameBody = Utility.Decompress(frameBytes);
                var frame = Utility.DeserializeToObject<Frame>(frameBody);

                //Zero out the consumed portion of the frame buffer - more for fun than anything else.
                Array.Clear(frameBuffer.FrameBuilder, 0, grossFrameSize);

                Buffer.BlockCopy(frameBuffer.FrameBuilder, grossFrameSize, frameBuffer.FrameBuilder, 0, frameBuffer.FrameBuilderLength - grossFrameSize);
                frameBuffer.FrameBuilderLength -= grossFrameSize;

                var payload = ExtractFramePayload(frame);

                if (payload is IFrameQuery query)
                {
                    if (processFrameQueryCallback == null)
                    {
                        throw new Exception("ProcessFrameBuffer: A query handler was not supplied.");
                    }
                    var replyPayload = processFrameQueryCallback(query);
                    stream.WriteReply(frame, replyPayload);
                }
                else if (payload is IFrameQueryReply reply)
                {
                    // A reply to a query was received, we need to find the waiting query - set the reply payload data and trigger the wait event.
                    var waitingQuery = _queriesAwaitingReplies.Where(o => o.FrameId == frame.Id).Single();
                    waitingQuery.ReplyPayload = reply;
                    waitingQuery.WaitEvent.Set();
                }
                else if (payload is IFrameNotification notification)
                {
                    if (processNotificationCallback == null)
                    {
                        throw new Exception("ProcessFrameBuffer: A notification handler was not supplied.");
                    }
                    processNotificationCallback(notification);
                }
                else
                {
                    throw new Exception("ProcessFrameBuffer: Encountered undefined frame payload type.");
                }
            }
        }

        private static IStreamFrame ExtractFramePayload(Frame frame)
        {
            var genericToObjectMethod = _reflectioncache.Use((o) =>
            {
                if (o.TryGetValue(frame.EnclosedPayloadType, out var method))
                {
                    return method;
                }
                return null;
            });

            if (genericToObjectMethod != null)
            {
                return (IStreamFrame?)genericToObjectMethod.Invoke(null, new object[] { frame.Payload })
                    ?? throw new Exception($"ExtractFramePayload: Payload can not be null.");
            }

            var genericType = Type.GetType(frame.EnclosedPayloadType)
                ?? throw new Exception($"ExtractFramePayload: Unknown payload type {frame.EnclosedPayloadType}.");

            var toObjectMethod = typeof(Utility).GetMethod("JsonDeserializeToObject")
                ?? throw new Exception($"ExtractFramePayload: Could not find JsonDeserializeToObject().");

            genericToObjectMethod = toObjectMethod.MakeGenericMethod(genericType);

            _reflectioncache.Use((o) => o.TryAdd(frame.EnclosedPayloadType, genericToObjectMethod));

            return (IStreamFrame?)genericToObjectMethod.Invoke(null, new object[] { frame.Payload })
                ?? throw new Exception($"ExtractFramePayload: Payload can not be null.");
        }
    }
}
