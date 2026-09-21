using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyItem : MonoBehaviour
{
    [SerializeField] private BodyParts bodyParts;
    [SerializeField] private int partIndex;

    public int PartIndex => partIndex;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            bodyParts.ActualizeParts(partIndex);
            Destroy(this.gameObject);
        }
    }
}
