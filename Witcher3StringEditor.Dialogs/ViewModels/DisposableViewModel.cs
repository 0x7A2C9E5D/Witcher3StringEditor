using CommunityToolkit.Mvvm.ComponentModel;

namespace Witcher3StringEditor.Dialogs.ViewModels;

public abstract class DisposableViewModel : ObservableObject, IDisposable
{
    private bool disposedValue;
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (disposedValue) return;
        disposedValue = true;
    }
}