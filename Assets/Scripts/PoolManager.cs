using System;
using System.Collections.Generic;
using MonsterLove.Collections;

/// <summary>
/// @Changes: 
///     - Changed to support UnityEngine.Object to make it pool ScriptableObject either of GameObject type.
///     
/// </summary>

namespace UnityEngine
{
    public class PoolManager : Singleton<PoolManager>
    {
        public bool logStatus;
        public Transform root;

        private Dictionary<Object, ObjectPool<Object>> prefabLookup;
        private Dictionary<Object, ObjectPool<Object>> instanceLookup;

        private bool dirty = false;

        void Awake()
        {
            prefabLookup = new Dictionary<Object, ObjectPool<Object>>();
            instanceLookup = new Dictionary<Object, ObjectPool<Object>>();
        }

        void Update()
        {
            if (logStatus && dirty)
            {
                PrintStatus();
                dirty = false;
            }
        }

        public void warmPool(Object o, int size)
        {
            if (prefabLookup.ContainsKey(o))
            {
                throw new Exception("Pool for prefab " + o.name + " has already been created");
            }
            var pool = new ObjectPool<Object>(() => { return InstantiatePrefab(o); }, size);
            prefabLookup[o] = pool;

            dirty = true;
        }

        public void warmPool(GameObject o, int size)
        {
            if (prefabLookup.ContainsKey(o))
            {
                throw new Exception("Pool for prefab " + o.name + " has already been created");
            }
            var pool = new ObjectPool<Object>(() => { 
                GameObject go = InstantiatePrefab(o);

                // sleep at the warm it up, it is reactivated when it is spawned.
                go.SetActive(false);
                return go;
            }, size);
            prefabLookup[o] = pool;

            dirty = true;
        }

        public GameObject spawnObject(GameObject prefab)
        {
            return spawnObject(prefab, Vector3.zero, Quaternion.identity);
        }

        public Object spawnObject(Object o)
        {
            if (!prefabLookup.ContainsKey(o))
            {
                WarmPool(o, 1);
            }

            var pool = prefabLookup[o];

            var clone = pool.GetItem();

            instanceLookup.Add(clone, pool);
            dirty = true;
            return clone;
        }

        public GameObject spawnObject(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!prefabLookup.ContainsKey(prefab))
            {
                WarmPool(prefab, 1);
            }

            var pool = prefabLookup[prefab];

            var clone = pool.GetItem() as GameObject;
            clone.transform.position = position;
            clone.transform.rotation = rotation;

            // Reactivate when it is spawned.
            clone.SetActive(true);

            instanceLookup.Add(clone, pool);
            dirty = true;
            return clone;
        }

        public void releaseObject(Object clone)
        {
            //clone.SetActive(false);

            if (instanceLookup.ContainsKey(clone))
            {
                instanceLookup[clone].ReleaseItem(clone);
                instanceLookup.Remove(clone);
                dirty = true;
            }
            else
            {
                Debug.LogWarning("No pool contains the object: " + clone.name);
            }
        }

        public void releaseObject(GameObject clone)
        {
            clone.SetActive(false);

            if (instanceLookup.ContainsKey(clone))
            {
                instanceLookup[clone].ReleaseItem(clone);
                instanceLookup.Remove(clone);
                dirty = true;
            }
            else
            {
                Debug.LogWarning("No pool contains the object: " + clone.name);
            }
        }


        private Object InstantiatePrefab(Object o)
        {
            return Instantiate(o) as Object;
        }

        private GameObject InstantiatePrefab(GameObject o)
        {
            var go = GameObject.Instantiate(o);
            if (root != null)
            {
                go.transform.parent = root;
            }
            else
            {
                // To prevent being destroyed when a scene is loaded or unloaded.
                go.transform.parent = this.gameObject.transform;
            }

            return go;
        }

        public void PrintStatus()
        {
            foreach (KeyValuePair<Object, ObjectPool<Object>> keyVal in prefabLookup)
            {
                Debug.Log(string.Format("Object Pool for Prefab: {0} In Use: {1} Total {2}", keyVal.Key.name, keyVal.Value.CountUsedItems, keyVal.Value.Count));
            }
        }

        #region Static API

        public static void WarmPool(Object o, int size)
        {
            Instance.warmPool(o, size);
        }

        public static void WarmPool(GameObject o, int size)
        {
            Instance.warmPool(o, size);
        }

        public static Object SpawnObject(Object o)
        {
            return Instance.spawnObject(o);
        }

        public static GameObject SpawnObject(GameObject go)
        {
            return Instance.spawnObject(go, Vector3.zero, Quaternion.identity);
        }

        public static GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return Instance.spawnObject(prefab, position, rotation);
        }

        public static void ReleaseObject(Object clone)
        {
            Instance.releaseObject(clone);
        }

        public static void ReleaseObject(GameObject clone)
        {
            Instance.releaseObject(clone);
        }
        #endregion
    }
}

