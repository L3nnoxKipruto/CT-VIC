using DataEntryApp.Models;

namespace DataEntryApp.Messages;

public class EditEntryMessage
{
    public Entry Entry { get; }

    public EditEntryMessage(Entry entry)
    {
        Entry = entry;
    }
}
