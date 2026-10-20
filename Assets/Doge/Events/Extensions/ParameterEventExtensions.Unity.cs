using UnityEngine;

namespace Doge.Events
{
    public static partial class ParameterEventExtensions
    {
        /// <summary>
        /// Creates a GameObjectEventParameter for your GameObject input.
        /// </summary>
        public static void Raise(this ParameterEvent e, GameObject value) {
            e.Raise(new ParamGameObject(value));
        }

        /// <summary>
        /// Creates a TransformEventParameter for your Transform input.
        /// </summary>
        public static void Raise(this ParameterEvent e, Transform value) {
            e.Raise(new ParamTransform(value));
        }

        /// <summary>
        /// Creates a Vector3EventParameter for your Vector3 input.
        /// </summary>
        public static void Raise(this ParameterEvent e, Vector3 value) {
            e.Raise(new ParamVector3(value));
        }

        /// <summary>
        /// Creates a Vector2EventParameter for your Vector2 input.
        /// </summary>
        public static void Raise(this ParameterEvent e, Vector2 value) {
            e.Raise(new ParamVector2(value));
        }

        /// <summary>
        /// Creates a generic SpriteEventParameter for your Sprite input.
        /// </summary>
        public static void Raise(this ParameterEvent e, Sprite value) {
            e.Raise(new ParamSprite(value));
        }
    }
}
