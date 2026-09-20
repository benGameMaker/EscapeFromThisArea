using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchNuclearExplosion : MonoBehaviour
{
    public GameObject nuclearExplosionPrefab;
    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.M))
        {
            LaunchExplosion();
        }
    }

    void LaunchExplosion()
    {
        if (nuclearExplosionPrefab != null)
        {
            Instantiate(nuclearExplosionPrefab, transform.position, transform.rotation);
        }
    }
}
