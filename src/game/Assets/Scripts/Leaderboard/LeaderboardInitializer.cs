using UnityEngine;

public class LeaderboardInitializer : MonoBehaviour
{
    private LeaderboardClient client;

    // Example: b0f31142-fb53-4150-b048-2a7b7542276d
    public string SessionSecret = "";

    // Start is called before the first frame update
    public void Start()
    {
        client = LeaderboardClient.GetClient();
        if (Application.isEditor && !string.IsNullOrWhiteSpace(SessionSecret))
        {
            StartCoroutine(client.ConnectAsEditor(SessionSecret.Trim()));
        }
        else if (!client.IsOffline)
        {
            StartCoroutine(client.Connect());
        }
    }

    // Update is called once per frame
    public void Update()
    {
    }
}
