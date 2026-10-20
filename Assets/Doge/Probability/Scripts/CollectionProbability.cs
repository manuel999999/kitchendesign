using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Doge.Probability
{
    public class CollectionProbability : MonoBehaviour
    {
		public CollectionProbability(params int[] percent) {
			_min = 0;
			_max = percent.Sum();
            _percents = percent;
            
			if(_max != 0) {
				Create();
			}
		}

		private HashSet<int>[] _collection;
		private int _min;
		private int _max;
        private int[] _percents;

        public int Max {
			get {
				return _max;
			}
		}

		private void Create() {
			if(_collection != null) {
                Array.Clear(_collection, 0, _collection.Length);
            }

            _collection = new HashSet<int>[_percents.Length];

            // create random collection
            System.Random r = new System.Random();
			var randoms = Enumerable.Range(_min, _max).OrderBy(x => r.Next()).ToArray();

            var randIndex = 0;  // randoms index
            var colIndex = 0;   // collection index

            while(randIndex < randoms.Length && colIndex < _percents.Length) {
                var len = _percents[colIndex];  // count of how many numbers to add in a hashset
                _collection[colIndex] = new HashSet<int>(); // initialize hashset in collection

                for (int i = 0; i < len; i++) {
                    _collection[colIndex].Add(randoms[randIndex]);  // add the random numbers
                    randIndex++;
                }

                colIndex++;
            }
		}

		private int Result() {
            if(_collection == null || _percents.Length == 0) {
            	return -1;
            }

            int result = UnityEngine.Random.Range(_min, _max);

            for (int i = 0; i < _collection.Length; i++) {
                if(_collection[i].Contains(result)) {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Get random params index.
        /// </summary>
        /// <returns>Random params index.</returns>
		public int Hit() {
			return Result();
		}

        /// <summary>
        /// Get random params index after creating new random numbers.
        /// </summary>
        /// <returns>Random params index.</returns>
		public int HitAsNew() {
			Create();
			return Result();
		}
    }
}
