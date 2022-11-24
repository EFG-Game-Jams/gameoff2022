using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class HubRaceEntranceScreenLeaderboard : HubRaceEntranceScreen
{
    [SerializeField] protected LeaderboardRecord[] records;

    public abstract void Refresh(string levelName, bool force = false);

    protected static IEnumerator RunThrowingIterator(
        IEnumerator enumerator,
        Action<Exception> done
    )
    {
        while (true)
        {
            object current;
            try
            {
                if (enumerator.MoveNext() == false)
                {
                    break;
                }
                current = enumerator.Current;
            }
            catch (Exception ex)
            {
                done(ex);
                yield break;
            }
            yield return current;
        }
        done(null);
    }
}
