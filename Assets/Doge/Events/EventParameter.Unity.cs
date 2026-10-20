using UnityEngine;

namespace Doge.Events
{
    public class ParamGameObject : IDogeParam {
        public ParamGameObject(GameObject input) {
            this.Value = input;
        }

        public GameObject Value { get; }
    }

    public class ParamTransform : IDogeParam {
        public ParamTransform(Transform input) {
            this.Value = input;
        }

        public Transform Value { get; }
    }

    public class ParamVector3 : IDogeParam {
        public ParamVector3(Vector3 input) {
            this.Value = input;
        }

        public Vector3 Value { get; }
    }

    public class ParamVector2 : IDogeParam {
        public ParamVector2(Vector2 input) {
            this.Value = input;
        }

        public Vector2 Value { get; }
    }

    public class ParamSprite : IDogeParam {
        public ParamSprite(Sprite input) {
            this.Value = input;
        }

        public Sprite Value { get; }
    }
}
