using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireParticle : MonoBehaviour
{
    public void OnParticleSystemStopped()
    {
        GameManager.Instance.HitEffects.Return(gameObject);
    }
}
