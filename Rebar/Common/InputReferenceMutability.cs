namespace Rebar.Common
{
    /// <summary>
    /// Enum representing the various behaviors of a group of input reference parameters.
    /// </summary>
    internal enum InputReferenceMutability
    {
        /// <summary>
        /// The input references are allowed to be, and will be auto-borrowed as, immutable
        /// </summary>
        AllowImmutable,

        /// <summary>
        /// The input references are required to be, and will be auto-borrowed as, mutable
        /// </summary>
        RequireMutable,

        /// <summary>
        /// The input references will be auto-borrowed as mutable if possible and immutable otherwise, and
        /// this mutability will be transfered to any related output types.
        /// </summary>
        Polymorphic
    }
}
