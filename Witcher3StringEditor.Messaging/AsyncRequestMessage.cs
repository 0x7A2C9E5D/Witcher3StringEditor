using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Witcher3StringEditor.Messaging;

/// <summary>
///     An asynchronous request message that carries a request value and expects a response of a specific type
///     Extends the base <see cref="AsyncRequestMessage{TResponse}" /> to provide a strongly-typed
///     request/response messaging pattern
/// </summary>
/// <typeparam name="TRequest">The type of the request value</typeparam>
/// <typeparam name="TResponse">The type of the expected response</typeparam>
public class TypedAsyncRequestMessage<TRequest, TResponse> : AsyncRequestMessage<TResponse>
{
    /// <summary>
    ///     Initializes a new instance of the TypedAsyncRequestMessage class
    /// </summary>
    /// <param name="request">The request value to be passed with the message</param>
    /// <exception cref="ArgumentNullException"><paramref name="request" /> is null</exception>
    public TypedAsyncRequestMessage(TRequest request)
    {
        // A null payload would only surface later as a NullReferenceException inside the handler
        ArgumentNullException.ThrowIfNull(request);
        Request = request;
    }

    /// <summary>
    ///     Gets the request value associated with this message
    /// </summary>
    public TRequest Request { get; }
}