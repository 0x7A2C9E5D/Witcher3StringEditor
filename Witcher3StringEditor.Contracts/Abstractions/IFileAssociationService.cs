namespace Witcher3StringEditor.Contracts.Abstractions;

/// <summary>
///     Defines a contract for managing the file association of the application
///     Provides access to register or unregister the association that lets
///     <c>.w3strings</c> files be opened with this application
/// </summary>
public interface IFileAssociationService
{
    /// <summary>
    ///     Gets a value indicating whether the file association is currently registered
    /// </summary>
    public bool IsRegistered { get; }

    /// <summary>
    ///     Registers the file association so that <c>.w3strings</c> files can be opened with this application
    /// </summary>
    public void Register();

    /// <summary>
    ///     Removes the file association previously registered by this application
    /// </summary>
    public void Unregister();
}