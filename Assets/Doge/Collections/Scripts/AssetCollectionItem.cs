namespace Doge.Collections
{
    [System.Serializable]
	public class AssetCollectionItem<Tkey, TValue>
	{
		public string Tag;
		public Tkey Key;
		public TValue Value;
	}
}
