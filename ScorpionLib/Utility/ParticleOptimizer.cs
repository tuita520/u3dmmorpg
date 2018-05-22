using UnityEngine;
using System.Collections;

public class ParticleOptimizer : MonoBehaviour
{

    private ParticleSystem mParticleSystem;

    private void Start()
    {
        mParticleSystem = transform.GetComponent<ParticleSystem>();
    }

    private void OnBecameVisible()
    {
        if (mParticleSystem == null)
        {
            return;
        }
        mParticleSystem.Play();
    }

    private void OnBecameInvisible()
    {
        if (mParticleSystem == null)
        {
            return;
        }
        mParticleSystem.Pause();
    }
}
