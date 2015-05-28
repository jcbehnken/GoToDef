using System;

namespace GoToDef
{
    public interface IKeyState
    {
        bool Enabled { get; set; }
        event EventHandler<EventArgs> KeyStateChanged;
    }
}
