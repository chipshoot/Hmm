using System;

namespace Hmm.Utility.HmmNoteContentMap
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class NoteContentAttribute : NoteSerializableAttribute
    {
        public string DisplayName { get; set; }
    }
}