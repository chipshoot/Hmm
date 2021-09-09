using System;

namespace Hmm.Utility.HmmNoteContentMap
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class NoteSerializerInstructorAttribute : NoteSerializableAttribute
    {
        public NoteSerializerInstructorAttribute(bool mapOnlyFlagProperty = false)
        {
            MapOnlyFlagProperty = mapOnlyFlagProperty;
        }

        public bool MapOnlyFlagProperty { get; }
    }
}