using CommunityToolkit.Mvvm.ComponentModel;

namespace Witcher3StringEditor.Dialogs.ViewModels;

/// <summary>
///     A base class for view models that need to be disposed.
/// </summary>
public abstract class DisposableViewModel : ObservableObject, IDisposable
{
    private bool disposedValue; // To detect redundant calls

    /// <summary>
    ///     Disposes the view model.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Disposes the view model.
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposedValue) return;
        disposedValue = true;
    }
}