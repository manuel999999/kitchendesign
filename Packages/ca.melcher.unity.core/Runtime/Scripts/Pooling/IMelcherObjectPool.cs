namespace Melcher
{
    using System.Collections.Generic;

    /// <summary>
    ///     An object pool is a optimization feature that allows a fixed amount
    ///     of objects to be pre-allocated and then managed via the pool. The
    ///     pool is created somewhere at a higher level, and then interested
    ///     users can borrow objects from it and return them when they're
    ///     finished.
    ///     <br /><br />
    ///
    ///     This interface is mainly meant to be used with non-Unity types.
    ///     I.e., it is intended to pool things like Lists or Dictionaries,
    ///     however using it to pool Prefabs and ScriptableObjects is also
    ///     possible, but requires a custom `createElement` function be used.
    ///     <br /><br />
    ///
    ///     This interface makes two assumptions about <typeparamref
    ///     name="ObjectType"/>:
    ///     <br /><br />
    ///
    ///         1. It is a reference type<br />
    ///         2. It can be created with a default constructor.<br />
    ///     <br /><br />
    ///
    ///     A user will create a concrete object pool (e.g., <see
    ///     cref="MelcherObjectPoolStack{ObjectType}"/> or <see
    ///     cref="MelcherObjectPoolCyclic{ObjectType}"/>). They will borrow
    ///     objects from the pool using the <see cref="GetObject"/> method. When
    ///     the user is finished with the object, it must be returned by calling
    ///     <see cref="ReturnObject(ObjectType)"/> and passing the object.
    /// </summary>
    public interface IMelcherObjectPool<ObjectType>
        where ObjectType : class
    {
        /// <summary>
        ///     Get the next available object in the pool.
        /// </summary>
        ObjectType GetObject();

        /// <summary>
        ///     Return a previously borrowed object to the pool.
        /// </summary>
        void ReturnObject(ObjectType obj);

        /// <summary>
        ///     Return a collection of *all* objects that were instantiated in
        ///     this pool.
        /// </summary>
        IReadOnlyCollection<ObjectType> allObjects { get; }

    }   // interface IMelcherObjectPool

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-01-29
/// ===========================================================================
