using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Hmm.Core.NoteSerializer
{
    public class DefaultXmlNoteContentManager : INoteContentManager
    {
        private readonly Dictionary<Type, TypeStoreItem> _store;

        public string GetNoteSerializationText(object entity)
        {
            throw new System.NotImplementedException();
        }

        private sealed class TypeStoreItem : StoreItem
        {
            private Type _type;

            internal TypeStoreItem(Type type, IEnumerable<Attribute> attributes) : base(attributes)
            {
                _type = type;
            }

            internal Type PropertyType { get; }
        }

        private sealed class PropertyStoreItem : StoreItem
        {
            internal PropertyStoreItem(Type propertyType, IEnumerable<Attribute> attributes) : base(attributes)
            {
                Debug.Assert(propertyType != null);
                PropertyType = propertyType;
            }

            internal Type PropertyType { get; }
        }

        private abstract class StoreItem
        {
            internal StoreItem(IEnumerable<Attribute> attributes)
            {
            }
        }
    }
}