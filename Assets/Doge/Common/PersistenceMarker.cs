using UnityEngine;

namespace Doge
{
    public class PersistenceMarker : MonoBehaviour
    {
    	private void Awake() {
            DontDestroyOnLoad(this.gameObject);
        }
    }
}