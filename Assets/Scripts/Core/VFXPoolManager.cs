using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace ProjectZ.Core
{
    /// <summary>
    /// Zero-Allocation Object Pooler for VFX.
    /// Manages particles, bullet holes, and shell casings.
    /// Designed for zero-damage integration into existing codebase.
    /// </summary>
    public class VFXPoolManager : MonoBehaviour
    {
        private static VFXPoolManager _instance;
        public static VFXPoolManager Instance 
        { 
            get 
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("[CORE] VFX_PoolManager");
                    _instance = go.AddComponent<VFXPoolManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            } 
            private set { _instance = value; }
        }

        private Dictionary<int, IObjectPool<GameObject>> _pools = new Dictionary<int, IObjectPool<GameObject>>();
        private Dictionary<int, GameObject> _prefabMap = new Dictionary<int, GameObject>();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;
            
            int id = prefab.GetInstanceID();
            
            if (!_pools.ContainsKey(id))
            {
                _prefabMap[id] = prefab;
                _pools[id] = new ObjectPool<GameObject>(
                    createFunc: () => Instantiate(_prefabMap[id], transform),
                    actionOnGet: (obj) => { obj.transform.position = position; obj.transform.rotation = rotation; obj.SetActive(true); },
                    actionOnRelease: (obj) => obj.SetActive(false),
                    actionOnDestroy: (obj) => Destroy(obj),
                    collectionCheck: false,
                    defaultCapacity: 20,
                    maxSize: 150
                );
            }
            
            GameObject instance = _pools[id].Get();
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            
            return instance;
        }

        public void Release(GameObject instance, GameObject prefab, float delay)
        {
            if (delay > 0)
                StartCoroutine(ReleaseRoutine(instance, prefab, delay));
            else
                ReleaseNow(instance, prefab);
        }

        private System.Collections.IEnumerator ReleaseRoutine(GameObject instance, GameObject prefab, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReleaseNow(instance, prefab);
        }

        private void ReleaseNow(GameObject instance, GameObject prefab)
        {
            if (instance == null || prefab == null) return;
            int id = prefab.GetInstanceID();
            if (_pools.TryGetValue(id, out var pool))
            {
                if (instance.activeSelf) 
                    pool.Release(instance);
            }
            else
            {
                Destroy(instance); // Fallback if unregistered
            }
        }
    }
}
