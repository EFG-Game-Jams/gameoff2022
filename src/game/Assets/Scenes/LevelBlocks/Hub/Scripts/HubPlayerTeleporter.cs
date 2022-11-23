using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HubPlayerTeleporter : MonoBehaviour
{
    [SerializeField] PlayerTrigger[] triggers;

    private void Start()
    {
        foreach (var trigger in triggers)
            trigger.onTriggerEnter.AddListener(OnTriggerEntered);
    }

    private void OnTriggerEntered(GameObject player, double _)
    {
        StartCoroutine(CoTeleportPlayer(player));
    }

    private IEnumerator CoTeleportPlayer(GameObject playerGo)
    {
        yield return null;
        SatriProtoPlayer player = playerGo.GetComponent<SatriProtoPlayer>();
        player.Teleport(transform.position, transform.rotation);

        yield return null;
        foreach (var trigger in triggers)
            trigger.ResetTrigger();
    }
}
