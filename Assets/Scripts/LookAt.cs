using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAt : MonoBehaviour
{
    [SerializeField] Transform lookAt;

    void Update()
    {
        if (lookAt != null)
        {
            transform.LookAt(lookAt);
        }
    }
}
