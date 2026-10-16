using System;
using System.Threading.Tasks;
using UnityEngine;

public static class TaskObservation
{
    // Event-driven operations run independently, but their failures must remain visible.
    public static async void Observe(this Task task)
    {
        try { await task; }
        catch (OperationCanceledException) { }
        catch (Exception exception) { Debug.LogException(exception); }
    }
}
