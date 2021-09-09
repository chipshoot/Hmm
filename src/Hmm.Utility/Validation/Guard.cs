using System;
using System.Reflection;

namespace Hmm.Utility.Validation
{
    /// <summary>
    /// Provides utility methods to guard parameter and local variables.
    /// <remarks>
    /// The piece of code is borrowed from NCommon project which is created by RiteshRao.
    /// The project can be found from http://ncommon.codeplex.com/releases/view/21867
    /// </remarks>
    /// </summary>
    public class Guard
    {
        /// <summary>
        /// Throws an exception of type <see cref="TException"/> with the specified message
        /// when the assertion statement is true.
        /// </summary>
        /// <typeparam name="TException">The type of exception to throw.</typeparam>
        /// <param name="assertion">The assertion to evaluate. If true then the <see cref="TException"/> exception is thrown.</param>
        /// <param name="message">string. The exception message to throw.</param>
        public static void Against<TException>(bool assertion, string message) where TException : Exception
        {
            if (assertion)
            {
                throw (TException)Activator.CreateInstance(typeof(TException), message);
            }
        }

        /// <summary>
        /// Throws an exception of type <see cref="TException"/> with the specified message
        /// when the assertion
        /// </summary>
        /// <typeparam name="TException"></typeparam>
        /// <param name="assertion"></param>
        /// <param name="message"></param>
        public static void Against<TException>(Func<bool> assertion, string message) where TException : Exception
        {
            //Execute the lambda and if it evaluates to true then throw the exception.
            if (assertion())
            {
                throw (TException)Activator.CreateInstance(typeof(TException), message);
            }
        }

        /// <summary>
        /// Throws a <see cref="InvalidOperationException"/> when the specified object
        /// instance does not inherit from <see cref="TBase"/> type.
        /// </summary>
        /// <typeparam name="TBase">The base type to check for.</typeparam>
        /// <param name="instance">The object to check if it inherits from <see cref="TBase"/> type.</param>
        /// <param name="message">string. The exception message to throw.</param>
        public static void InheritsFrom<TBase>(object instance, string message) where TBase : Type
        {
            InheritsFrom<TBase>(instance.GetType(), message);
        }

        /// <summary>
        /// Throws a <see cref="InvalidOperationException"/> when the specified type does not
        /// inherit from the <see cref="TBase"/> type.
        /// </summary>
        /// <typeparam name="TBase">The base type to check for.</typeparam>
        /// <param name="type">The <see cref="Type"/> to check if it inherits from <see cref="TBase"/> type.</param>
        /// <param name="message">string. The exception message to throw.</param>
        public static void InheritsFrom<TBase>(Type type, string message)
        {
            var findParent = false;
            var curBaseType = type.GetTypeInfo().BaseType;
            while (curBaseType != null)
            {
                if (curBaseType == typeof(TBase))
                {
                    findParent = true;
                    break;
                }

                curBaseType = curBaseType.GetTypeInfo().BaseType;
            }

            if (!findParent)
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Throws a <see cref="InvalidOperationException"/> when the specified object
        /// instance does not implement the <see cref="TInterface"/> interface.
        /// </summary>
        /// <typeparam name="TInterface">The interface type the object instance should implement.</typeparam>
        /// <param name="instance">The object instance to check if it implements the <see cref="TInterface"/> interface</param>
        /// <param name="message">string. The exception message to throw.</param>
        public static void Implements<TInterface>(object instance, string message)
        {
            Implements<TInterface>(instance.GetType(), message);
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> when the specified type does not
        /// implement the <see cref="TInterface"/> interface.
        /// </summary>
        /// <typeparam name="TInterface">The interface type that the <paramref name="type"/> should implement.</typeparam>
        /// <param name="type">The <see cref="Type"/> to check if it implements from <see cref="TInterface"/> interface.</param>
        /// <param name="message">string. The exception message to throw.</param>
        public static void Implements<TInterface>(Type type, string message)
        {
            if (!typeof(TInterface).IsAssignableFrom(type))
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> when the specified object instance is
        /// not of the specified type.
        /// </summary>
        /// <typeparam name="TType">The Type that the <paramref name="instance"/> is expected to be.</typeparam>
        /// <param name="instance">The object instance whose type is checked.</param>
        /// <param name="message">The message of the <see cref="InvalidOperationException"/> exception.</param>
        public static void TypeOf<TType>(object instance, string message)
        {
            if (!(instance is TType))
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Throws an exception if an instance of an object is not equal to another object instance.
        /// </summary>
        /// <typeparam name="TException">The type of exception to throw when the guard check evaluates false.</typeparam>
        /// <param name="compare">The comparison object.</param>
        /// <param name="instance">The object instance to compare with.</param>
        /// <param name="message">string. The message of the exception.</param>
        public static void IsEqual<TException>(object compare, object instance, string message) where TException : Exception
        {
            if (compare != instance)
            {
                throw (TException)Activator.CreateInstance(typeof(TException), message);
            }
        }
    }
}