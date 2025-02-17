using System;
using System.Collections.Generic;

public static class EventBus
{
    // Stores event names and their corresponding subscription methods
    private static Dictionary<string, Action<object>> eventDictionary = new Dictionary<string, Action<object>>();

    // Subscribe to an event
    public static void Subscribe(string eventName, Action<object> listener)
    {
        if (eventDictionary.ContainsKey(eventName))
            eventDictionary[eventName] += listener;
        else
            eventDictionary.Add(eventName, listener);
    }

    // Unsubscribe from an event
    public static void Unsubscribe(string eventName, Action<object> listener)
    {
        if (eventDictionary.ContainsKey(eventName))
            eventDictionary[eventName] -= listener;
    }

    // Publish an event, optionally with parameters
    public static void Publish(string eventName, object param = null)
    {
        if (eventDictionary.ContainsKey(eventName))
            eventDictionary[eventName]?.Invoke(param);
    }
}
