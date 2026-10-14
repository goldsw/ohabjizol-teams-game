using System;

namespace Ohabjizol.Interview
{
    public interface IManagedWindow
    {
        DocumentKey DocumentKey { get; }
        WindowTaskbarDescriptor TaskbarInfo { get; }
        bool IsVisible { get; }
        bool IsClosed { get; }
        bool IsActive { get; }
        event Action<IManagedWindow> StateChanged;
        event Action<IManagedWindow> Closed;
        event Action<IManagedWindow> FocusRequested;
        void Minimize();
        void Maximize();
        void Close();
    }
}
