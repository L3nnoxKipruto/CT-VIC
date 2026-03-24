using CommunityToolkit.Mvvm.Messaging.Messages;
using DataEntryApp.Models;

namespace DataEntryApp.Messages;

public class EntryCreatedMessage : ValueChangedMessage<Entry>
{
    public EntryCreatedMessage(Entry entry) : base(entry)
    {
    }
}
