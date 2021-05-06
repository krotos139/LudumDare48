using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    List<GameObject> pool = new List<GameObject>();
    public GameObject prefab;

    public GameObject GetFromPool()
    {
        GameObject go;
        if (pool.Count != 0)
        {
            go = pool[0];
            pool.RemoveAt(0);
        }
        else
        {
            go = Instantiate(prefab);
        }
        go.SetActive(true);
        return go;
    }

    public void RevertToPool(GameObject go)
    {
        go.SetActive(false);
        pool.Add(go);
    }

}
