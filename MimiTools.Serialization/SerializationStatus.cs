using System;

namespace MimiTools.Serialization
{
    public class SerializationStatus
    {
        /// <summary>
        /// Signals a successful deserialization, providing the deserialized object.
        /// </summary>
        /// <param name="o">The deserialized object</param>
        /// <returns>A status representing the object has been deserialized</returns>
        public static SerializationStatus Deserialized(object o)
            => new SerializationStatus(SerializationResult.Deserialized, obj: o);

        /// <summary>
        /// Signals a Deserialization, a Fix, or a Serialization failed. Use this if you couldn't complete any of these
        /// </summary>
        public static SerializationStatus Failure { get; } = new SerializationStatus(SerializationResult.Failure);

        /// <summary>
        /// Signals a delayed deserialization, returning a partially deserialized object for fixing later when the specified IDs are present
        /// </summary>
        /// <param name="o">The partially deserialized object</param>
        /// <param name="fix">The action to run once all the missing objects are deserialized</param>
        /// <param name="missing_ids">The missing object IDs, the fixup will be run once these are initially deserialized</param>
        /// <returns>A status stating deserialization couldn't be completed, but is in-progress</returns>
        public static SerializationStatus FixupLater(object o, SerializationFixup fix, ulong[] missing_ids)
        {
            if (fix == null)
                throw new ArgumentNullException(nameof(fix));

            missing_ids = missing_ids ?? Array.Empty<ulong>();

            return new SerializationStatus(SerializationResult.NeedsFixing, obj: o, fix: fix, missing_ids: missing_ids);
        }

        /// <summary>
        /// Signals a Serialization or Fixup Success, use these if you complete either successfully
        /// </summary>
        public static SerializationStatus Success { get; } = new SerializationStatus(SerializationResult.Success);

        /// <summary>
        /// Signals a Serialization or Deserialization Failure, with suggestions for what to treat the object as, the serialization instance will take that into account
        /// </summary>
        /// <param name="t">The suggested type</param>
        /// <returns>A status suggesting the instance try serializing/deserializing the object as the specified type</returns>
        public static SerializationStatus UseAlternativeType(Type t)
            => new SerializationStatus(SerializationResult.UseAlternative, alt: t);

        private SerializationStatus(SerializationResult result, object obj = null, Type alt = null, SerializationFixup fix = null, ulong[] missing_ids = null)
        {
            AltType = alt;
            Fix = fix;
            Object = obj;
            RequiredIDs = missing_ids;
            Result = result;
        }

        internal readonly Type AltType;
        internal readonly SerializationFixup Fix;
        internal readonly object Object;
        internal readonly SerializationResult Result;
        internal readonly ulong[] RequiredIDs;
    }

    /// <summary>
    /// A delegate that gets called when all objects are available for fixups. Please keep in mind that some objects might not be fully deserialized when this function is called
    /// </summary>
    /// <param name="instance">The serialization instance responsible for deserializing this object</param>
    /// <param name="obj">The partially deserialized object that needs fixing</param>
    /// <param name="ids">The missing object IDs that are being provided</param>
    /// <param name="values">The formerly missing values, again these might not be fully deserialized</param>
    /// <returns>A SerializationStatus indicating the fix's success or failure</returns>
    public delegate SerializationStatus SerializationFixup(SerializationInstance instance, object obj, ulong[] ids, object[] values);

    internal enum SerializationResult
    {
        Deserialized, Failure, NeedsFixing, Success, UseAlternative,
    }
}
