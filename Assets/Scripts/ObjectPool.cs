using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [SerializeField] private GameObject prf;
    [SerializeField] private int size = 20;
    Queue<GameObject> pool = new Queue<GameObject>();

    private void Awake()
    {
        for (int i = 0; i < size; ++i)
            pool.Enqueue(Create());
    }

    private GameObject Create()
    {
        var obj = Instantiate(prf, transform);
        obj.SetActive(false);
        return obj;
    }

    public GameObject Get(Vector3 pos, Quaternion rot)
    {
        if (pool.Count == 0)
            pool.Enqueue(Create());

        var obj = pool.Dequeue();
        obj.transform.position = pos;
        obj.transform.rotation = rot;
        obj.SetActive(true);

        return obj;
    }

    public void Return(GameObject obj)
    {
        if (obj.activeSelf == false) return;
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
