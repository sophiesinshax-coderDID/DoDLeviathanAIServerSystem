using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;

public class NetworkManager : MonoBehaviour, INetEventListener
{
    private NetManager _netManager;
    private bool _isServerStarted = false;
    
    [SerializeField] private int port = 9050;
    [SerializeField] private string connectionKey = "CorrosiveSurvival_SecretKey";

    public bool IsServer => _isServerStarted;

    void Start()
    {
        _netManager = new NetManager(this)
        {
            AutoRecycle = true
        };
    }

    void Update()
    {
        if (_netManager != null && _netManager.IsRunning)
        {
            _netManager.PollEvents();
        }
    }

    public void StartServer()
    {
        _netManager.Start(port);
        _isServerStarted = true;
        Debug.Log($"[Network]: Server started on port {port}. Waiting for connections...");
    }

    public void ConnectToServer(string ipAddress)
    {
        _isServerStarted = false;
        _netManager.Start(); 
        NetDataWriter writer = new NetDataWriter();
        writer.Put(connectionKey);
        
        _netManager.Connect(ipAddress, port, writer);
        Debug.Log($"[Network]: Attempting connection to {ipAddress}:{port}...");
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        NetDataReader reader = request.Data;
        string key = reader.GetString();

        if (key == connectionKey)
        {
            request.Accept();
            Debug.Log("[Network]: Incoming player connection accepted.");
        }
        else
        {
            request.Reject();
            Debug.LogWarning("[Network]: Connection rejected due to mismatched key.");
        }
    }

    public void OnPeerConnected(NetPeer peer)
	{
		Debug.Log("[Network]: Peer connected successfully!");
	}

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Debug.Log($"[Network]: Peer disconnected. Reason: {disconnectInfo.Reason}");
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Debug.LogError($"[Network]: Network error {socketError} at {endPoint}");
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

    void OnDisable()
    {
        if (_netManager != null)
        {
            _netManager.Stop();
            _isServerStarted = false;
        }
    }
}