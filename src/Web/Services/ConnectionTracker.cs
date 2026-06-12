using System.Collections.Concurrent;

namespace PairCode.Web.Services;

public class ConnectionTracker
{
    private readonly ConcurrentDictionary<string, (string UserId, HashSet<string> Rooms)> _connections = new();

    public void AddConnection(string connectionId, string userId)
    {
        _connections.TryAdd(connectionId, (userId, []));
    }

    public void JoinRoom(string connectionId, string roomId)
    {
        if (_connections.TryGetValue(connectionId, out var entry))
        {
            lock (entry.Rooms)
            {
                entry.Rooms.Add(roomId);
            }
        }
    }

    public void LeaveRoom(string connectionId, string roomId)
    {
        if (_connections.TryGetValue(connectionId, out var entry))
        {
            lock (entry.Rooms)
            {
                entry.Rooms.Remove(roomId);
            }
        }
    }

    public void RemoveConnection(string connectionId)
    {
        _connections.TryRemove(connectionId, out _);
    }

    public IEnumerable<string> GetUserRooms(string connectionId)
    {
        if (_connections.TryGetValue(connectionId, out var entry))
        {
            lock (entry.Rooms)
            {
                return entry.Rooms.ToArray();
            }
        }
        return [];
    }
}
