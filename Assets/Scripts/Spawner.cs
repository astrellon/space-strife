using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbits
{
    public class Spawner : MonoBehaviour
    {
        public GameObject Prefab;
        public int MaxShips;
        public GameObject PrefillHit;
        public int PrefillHitCount;

        // Start is called before the first frame update
        void Start()
        {
            var list = new List<GameObject>(this.PrefillHitCount);
            for (var i = 0; i < this.PrefillHitCount; i++)
            {
                list.Add(GameObjectPools.Instance.Spawn(this.PrefillHit));
            }
            foreach (var item in list)
            {
                GameObjectPools.Instance.Release(this.PrefillHit, item);
            }

            for (var i = 0; i < this.MaxShips; i++)
            {
                var x = Random.value * 100.0f - 50.0f;
                var z = Random.value * 100.0f - 50.0f;
                Instantiate(this.Prefab, new Vector3(x, 0.0f, z), Quaternion.identity);
            }
        }
    }
}
