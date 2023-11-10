# NTDLS.StreamFraming

📦 Be sure to check out the NuGet pacakge: https://www.nuget.org/packages/NTDLS.StreamFraming

NTDLS.StreamFraming is a set of extension methods for a Stream (typically TCPIP/NetworkStream) that
enables reliable framing, compression, optional encryption, two-way communication and support for
asynchronous query/reply. Messages are guaranteed to received in their entirety and in the order
they were sent.

## Sending a notification frame:
> Here we are using an established TcpClient connection, getting its stream and then calling
> WriteNotification() to pacakge the payload "MyMessage" and write it to the stream.
> As per TCP/IP protocol, this can and will be received fragmented and/or concatenated with
> other bytes that are sent in the stream... but don't worry thats what NTDLS.StreamFraming
> is here to solve.
```csharp
using (var tcpStream = tcpClient.GetStream())
{
    while (tcpClient.Connected)
    {
        string text = $"This is a message that was sent at {DateTime.Now.ToLongTimeString()}.";

        //Assemble a message frame and write it to the stream:
        tcpStream.WriteNotification(new MyMessage(text));

        Thread.Sleep(1000);
    }
    tcpStream.Close();
}
```

## Receiving a notification frame:
> Here we are using an established TcpClient connection, getting its stream and then calling
> ReadAndProcessFrames(). ReadAndProcessFrames will read from the stream and determine if the
> bytes that are received (if any) are a full frame, only a fragment or a concatenation of
> multiple packets and fragments. Any full frames will be split, validated, decompressed,
> deserialized and then the callback "ProcessFrameNotificationCallback" will be called for each valid frame.
```csharp
var frameBuffer = new FrameBuffer();

using (var tcpStream = _tcpClient.GetStream())
{
    while (_tcpClient.Connected)
    {
        //Read from the stream, assemble the bytes into the original messages and call
        //  the handler for each message that was received.
        if (tcpStream.ReadAndProcessFrames(frameBuffer, ProcessFrameNotificationCallback) == false)
        {
            //If ReadAndProcessFrames() returns false then we have been disconnected.
            break;
        }
    }
    tcpStream.Close();
}

//This is the handler for a frame that was written to the stream with a call to WriteNotification().
//Note that the origianl class is received and we use patter matching to determine what we are receiving.
private void ProcessFrameNotificationCallback(IFrameNotification payload)
{
    //We recevied a message, see if it is of the type "MyMessage".
    if (payload is MyMessage myMessage)
    {
        Console.WriteLine($"Received from server: '{myMessage.Text}'");
    }
}
```


## Sending a query frame:
> Here we are using an established TcpClient connection, getting its stream and then calling
> WriteQuery() to pacakge the payload "MyQuery" and write it to the stream. We will then "wait"
> asynchronously or the client to receive and reply to the query. 
```csharp
using (var tcpStream = tcpClient.GetStream())
{
    while (tcpClient.Connected)
    {
        tcpStream.WriteQuery<MyQueryReply>(new MyQuery("Hello client!")).ContinueWith((o) =>
            {
                if (o.IsCompletedSuccessfully && o.Result != null)
                {
                    Console.WriteLine($"Received [QueryReply] from client: '{o.Result.Text}')");
                }
            });

        //In this example, we have to call ReadAndProcessFrames() even though we are not
        //  supplying it with any callbacks because it receives the replies from the query
        //  that is sent above and routes them to the correct query task handler.
        if (tcpStream.ReadAndProcessFrames(frameBuffer) == false)
        {
            break; //The client disconnected.
        }

        Thread.Sleep(1000);
    }
    tcpStream.Close();
}
```

## Receiving a query frame and replying:
> Here we are using an established TcpClient connection, getting its stream and then calling
> ReadAndProcessFrames(). ReadAndProcessFrames will read from the stream and determine if the
> bytes that are received are a full frame. Any full frames will be deserialized and routed
> to the callbacks "ProcessFrameNotificationCallback" or "ProcessFrameQueryCallback" based
> on their types. "ProcessFrameNotificationCallback" expects a return value to be of the type
> specified on the original call to WriteQuery(). The value returned from ProcessFrameQueryCallback
> will be framed and sent back to the originator and then routed to the appropriate waiting
> asynchronous task.
```csharp
var frameBuffer = new FrameBuffer();

using (var tcpStream = _tcpClient.GetStream())
{
    while (_tcpClient.Connected)
    {
        //Read from the stream, assemble the bytes into the original messages and call
        //  the handler for each message that was received.
        if (tcpStream.ReadAndProcessFrames(frameBuffer, ProcessFrameNotificationCallback, ProcessFrameQueryCallback) == false)
        {
            //If ReadAndProcessFrames() returns false then we have been disconnected.
            break;
        }
    }
    tcpStream.Close();
}

//This is the handler for a frame that was written to the stream with a call to WriteNotification().
//Note that the origianl class is received and we use patter matching to determine what we are receiving.
private void ProcessFrameNotificationCallback(IFrameNotification payload)
{
    //We recevied a message, see if it is of the type "MyMessage".
    if (payload is MyMessage myMessage)
    {
        Console.WriteLine($"Received from server: '{myMessage.Text}'");
    }
}

private IFrameQueryReply ProcessFrameQueryCallback(IFrameQuery payload)
{
    //We recevied a message, see if it is of the type "MyQuery", if so reply with the type of MyQueryReply.
    if (payload is MyQuery myQuery)
    {
        Console.WriteLine($"Received [Query] from server: '{myQuery.Text}'");

        return new MyQueryReply("Hello Server!");
    }

    throw new Exception("The query type was unhandled.");
}
```

## Supporting Classes:
> These are the payload classes that are used in the examples:

**Used in the examples to send a one-way notification**
```csharp
    internal class MyMessage : IFrameNotification
    {
        public string Text { get; set; }

        public MyMessage(string text)
        {
            Text = text;
        }
    }
```

**Used in the examples to send a query that expects a reply:**
```csharp
internal class MyQuery : IFrameQuery
{
    public string Text { get; set; }

    public MyQuery(string text)
    {
        Text = text;
    }
}
```

**Used in the examples to reply to a received query:**
```csharp
internal class MyQueryReply : IFrameQueryReply
{
    public string Text { get; set; }

    public MyQueryReply(string text)
    {
        Text = text;
    }
}
```
