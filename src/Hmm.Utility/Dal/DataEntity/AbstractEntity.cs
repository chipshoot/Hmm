namespace Hmm.Utility.Dal.DataEntity
{
    /// <summary>
    /// The base class of domain entity
    /// </summary>
    public abstract class AbstractEntity<TIdentity> : IGenericEntity<TIdentity>
    {
        #region private fields

        private int? _requestedHashCode;

        #endregion private fields

        #region implementation of IGenericEntity{TIdentity}

        /// <summary>
        /// Gets the id of the domain entity. the setting should be protected, we open it here
        /// for unit testing purpose
        /// </summary>
        /// <value>
        /// The id of the domain entity.
        /// </value>
        public virtual TIdentity Id { get; set; }

        #endregion implementation of IGenericEntity{TIdentity}

        #region Implementation of IEquatable<TIdentity>

        /// <summary>
        /// Compare equality trough Id
        /// </summary>
        /// <param name="other">Entity to compare.</param>
        /// <returns>true is are equals</returns>
        /// <remarks>
        /// Two entities are equals if they are of the same hierarchy tree/sub-tree
        /// and has same id.
        /// </remarks>
        public virtual bool Equals(IGenericEntity<TIdentity> other)
        {
            if (other == null || !GetType().IsInstanceOfType(other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            var otherIsTransient = Equals(other.Id, default(TIdentity));
            var thisIsTransient = IsTransient();
            if (otherIsTransient && thisIsTransient)
            {
                return ReferenceEquals(other, this);
            }

            return other.Id != null && other.Id.Equals(Id);
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
        /// </returns>
        /// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>. </param><filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            var that = obj as IGenericEntity<TIdentity>;
            return Equals(that);
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            if (!_requestedHashCode.HasValue)
            {
                _requestedHashCode = IsTransient() ? base.GetHashCode() : Id.GetHashCode();
            }

            return _requestedHashCode.Value;
        }

        #endregion Implementation of IEquatable<TIdentity>

        #region protected methods

        /// <summary>
        /// Determines whether this instance is transient. If it isn't transient, then we can
        /// compare its id to see if two instances are equal, otherwise we need a reference compare
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance is transient; otherwise, <c>false</c>.
        /// </returns>
        private bool IsTransient()
        {
            return Equals(Id, default(TIdentity));
        }

        #endregion protected methods
    }
}