using UnityEngine;

public class LeaderboardObject : MonoBehaviour
{
    private LeaderboardClient client;

    // Start is called before the first frame update
    public void Start()
    {
        client = LeaderboardClient.GetClient();
        if (!client.IsOffline)
        {
            StartCoroutine(client.Connect());
        }
    }

    // Update is called once per frame
    public void Update()
    {
    }
}
