using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hider : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return new WaitForSeconds(1f);

        gameObject.SetActive(false);
    }
}
