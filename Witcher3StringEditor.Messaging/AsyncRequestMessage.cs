using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Witcher3StringEditor.Messaging;

/// <summary>
///     A generic asynchronous request message that carries a request value and expects a response of a specific type
///     Extends the base AsyncRequestMessage to provide a strongly-typed request/response messaging pattern
/// </summary>
/// <typeparam name="TRequest">The type of the request value</typeparam>
/// <typeparam name="TResponse">The type of the expected response</typeparam>
/// <param name="request">The request value to be passed with the message</param>
public class AsyncRequestMessage<TRequest, TResponse>(TRequest request) : AsyncRequestMessage<TResponse>
{
    /// <summary>
    ///     Gets the request value associated with this message
    /// </summary>
    public TRequest Request { get; } = request;
}