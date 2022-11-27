using System;
using UnityEngine;

public class LeaderboardInitializer : MonoBehaviour
{
    private LeaderboardClient client;

    // Example: b0f31142-fb53-4150-b048-2a7b7542276d
    public string SessionSecret = "";

    // Start is called before the first frame update
    public void Start()
    {
        if (Application.isEditor)
        {
            throw new InvalidProgramException("This script is editor only");
        }

        client = LeaderboardClient.GetClient();
        StartCoroutine(client.CheckServerHealth((isHealthy) =>
        {
            if (isHealthy && !string.IsNullOrWhiteSpace(SessionSecret))
            {
                StartCoroutine(client.ConnectAsEditor(SessionSecret.Trim(), null));
            }
            else
            {
                StartCoroutine(client.Connect(null));
            }
        }));
    }
}
