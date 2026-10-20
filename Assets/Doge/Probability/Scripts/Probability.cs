using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Doge.Probability
{
    public class Probability
	{
		public Probability (int percent) :
		this(percent, 100) { }

		public Probability(int percent, int max) {
			_min = 0;
			_max = max;
			_percent = Mathf.Clamp(percent, 0, max);

			if(percent != _max && _percent != 0) {
				Create();
			}
		}

		private HashSet<int> _collection;
		private int _min;
		private int _max;
		private int _percent;

		public int Min {
			get {
				return _min;
			}
		}

		public int Max {
			get {
				return _max;
			}
		}

		public int Percent {
			get {
				return _percent;
			}
		}

		private void Create() {
			if(_collection != null) {
                _collection.Clear();
            }
			
			_collection = new HashSet<int>();

			System.Random r = new System.Random();
			var randoms = Enumerable.Range(_min, _max).OrderBy(x => r.Next()).ToArray();

			for(int i = 0; i < _percent; i++) {
                _collection.Add(randoms[i]);
            }
		}

		private bool Result() {
			if(_percent == _max) {
				return true;
			}

			if(_collection == null || _percent == 0) {
				return false;
			}

			int result = Random.Range(_min, _max);
			if(_collection.Contains(result)) {
				return true;
			} else {
				return false;
			}
		}

		public bool Hit() {
			return Result();
		}

		/// <summary>
        /// Recreate the whole random table
        /// </summary>
		public void Refresh() {
            Create();
        }
	}
}
