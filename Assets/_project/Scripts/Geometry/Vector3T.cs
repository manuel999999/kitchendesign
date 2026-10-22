namespace KitchenDesign.Geometry
{
    using Melcher;
    /// <summary>
    ///     Intended to allow arbitrary data to be grouped in a convenient
    ///     Vector3 format. For example, this is useful for associating boolean
    ///     or enum values with each channel in a Vector3.
    /// </summary>
    [System.Serializable]
    public struct Vector3T<ValueType>
    {
        // ====================================================================
        // Constructors
        // ====================================================================

        public Vector3T(ValueType defaultValue)
            : this(defaultValue, defaultValue, defaultValue)
        {
        }

        public Vector3T(ValueType x, ValueType y, ValueType z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public void Set(ValueType x, ValueType y, ValueType z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        // ====================================================================
        // Methods
        // ====================================================================

        public ValueType this[int index]
        {
            get
            {
                return
                    index switch
                    {
                        0 => x,
                        1 => y,
                        2 => z,
                        _ => throw new System.ArgumentOutOfRangeException(nameof(index))
                    };
            }

            set
            {
                switch (index)
                {
                    case 0:
                        x = value;
                        break;

                    case 1:
                        y = value;
                        break;

                    case 2:
                        z = value;
                        break;

                    default:
                        throw new System.ArgumentOutOfRangeException(nameof(index));
                };
            }
        }

        // ====================================================================
        // Public fields
        // ====================================================================

        public ValueType x;
        public ValueType y;
        public ValueType z;

    }   // struct Vector3T

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-10-11
/// ===========================================================================
