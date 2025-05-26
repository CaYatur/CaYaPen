using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace AdvancedScreenPen
{
    public class CollaborationManager : IDisposable
    {
        private bool isConnected = false;
        private bool isServer = false;
        private string serverAddress = "localhost";
        private int serverPort = 8080;
        private string username = "CaYatur";
        private string sessionId = Guid.NewGuid().ToString();

        private TcpClient tcpClient;
        private TcpListener tcpListener;
        private NetworkStream networkStream;
        private Thread listenerThread;
        private CancellationTokenSource cancellationTokenSource;

        private Dictionary<string, RemoteUser> connectedUsers = new Dictionary<string, RemoteUser>();
        private Queue<CollaborationMessage> messageQueue = new Queue<CollaborationMessage>();
        private readonly object messageLock = new object();

        // Events
        public event EventHandler<RemoteStrokeEventArgs> RemoteStrokeReceived;
        public event EventHandler<UserEventArgs> UserJoined;
        public event EventHandler<UserEventArgs> UserLeft;
        public event EventHandler<MessageEventArgs> MessageReceived;
        public event EventHandler<ConnectionEventArgs> ConnectionStatusChanged;
        public event EventHandler<ErrorEventArgs> ErrorOccurred;

        // Connection settings
        public int ConnectionTimeout { get; set; } = 10000; // 10 seconds
        public int HeartbeatInterval { get; set; } = 30000; // 30 seconds
        public bool AutoReconnect { get; set; } = true;
        public int MaxReconnectAttempts { get; set; } = 5;

        // Collaboration features
        public bool EnableVoiceChat { get; set; } = false;
        public bool EnableScreenSharing { get; set; } = false;
        public bool EnableRealTimeSync { get; set; } = true;
        public string ProjectName { get; set; } = "Untitled";

        public bool IsConnected => isConnected;
        public string Username => username;
        public string SessionId => sessionId;
        public IReadOnlyDictionary<string, RemoteUser> ConnectedUsers => connectedUsers;

        public CollaborationManager()
        {
            cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task<bool> ConnectAsClientAsync(string address, int port, string user)
        {
            try
            {
                serverAddress = address;
                serverPort = port;
                username = user;
                isServer = false;

                tcpClient = new TcpClient();

                var connectTask = tcpClient.ConnectAsync(IPAddress.Parse(serverAddress), serverPort);
                if (await Task.WhenAny(connectTask, Task.Delay(ConnectionTimeout)) == connectTask)
                {
                    networkStream = tcpClient.GetStream();
                    isConnected = true;

                    // Send join message
                    await SendJoinMessageAsync();

                    // Start listening for messages
                    StartListening();

                    // Start heartbeat
                    StartHeartbeat();

                    OnConnectionStatusChanged(true, "Connected to server");
                    return true;
                }
                else
                {
                    tcpClient?.Close();
                    throw new TimeoutException("Connection timeout");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to connect: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> StartServerAsync(int port, string user)
        {
            try
            {
                serverPort = port;
                username = user;
                isServer = true;

                tcpListener = new TcpListener(IPAddress.Any, serverPort);
                tcpListener.Start();

                isConnected = true;

                // Start accepting clients
                StartAcceptingClients();

                OnConnectionStatusChanged(true, $"Server started on port {serverPort}");
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to start server: {ex.Message}");
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                isConnected = false;

                // Send leave message
                if (networkStream != null)
                {
                    await SendLeaveMessageAsync();
                }

                // Cancel all operations
                cancellationTokenSource?.Cancel();

                // Close connections
                networkStream?.Close();
                tcpClient?.Close();
                tcpListener?.Stop();

                // Clear users
                connectedUsers.Clear();

                OnConnectionStatusChanged(false, "Disconnected");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error during disconnect: {ex.Message}");
            }
        }

        public async Task SendStrokeAsync(AdvancedStroke stroke)
        {
            if (!isConnected || stroke == null) return;

            try
            {
                var message = new CollaborationMessage
                {
                    Type = MessageType.Stroke,
                    SenderId = username,
                    SessionId = sessionId,
                    Timestamp = DateTime.UtcNow,
                    Data = SerializeStroke(stroke)
                };

                await SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to send stroke: {ex.Message}");
            }
        }

        public async Task SendTextMessageAsync(string text)
        {
            if (!isConnected || string.IsNullOrEmpty(text)) return;

            try
            {
                var message = new CollaborationMessage
                {
                    Type = MessageType.Text,
                    SenderId = username,
                    SessionId = sessionId,
                    Timestamp = DateTime.UtcNow,
                    Data = text
                };

                await SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to send text message: {ex.Message}");
            }
        }

        public async Task SendClearCommandAsync()
        {
            if (!isConnected) return;

            try
            {
                var message = new CollaborationMessage
                {
                    Type = MessageType.Clear,
                    SenderId = username,
                    SessionId = sessionId,
                    Timestamp = DateTime.UtcNow,
                    Data = "clear_all"
                };

                await SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to send clear command: {ex.Message}");
            }
        }

        public async Task SendCursorPositionAsync(PointF position)
        {
            if (!isConnected || !EnableRealTimeSync) return;

            try
            {
                var message = new CollaborationMessage
                {
                    Type = MessageType.Cursor,
                    SenderId = username,
                    SessionId = sessionId,
                    Timestamp = DateTime.UtcNow,
                    Data = JsonConvert.SerializeObject(new { X = position.X, Y = position.Y })
                };

                await SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                // Cursor updates are frequent, don't report errors for these
            }
        }

        private async Task SendJoinMessageAsync()
        {
            var userInfo = new
            {
                Username = username,
                SessionId = sessionId,
                JoinTime = DateTime.UtcNow,
                ProjectName = ProjectName,
                Version = "2.0"
            };

            var message = new CollaborationMessage
            {
                Type = MessageType.Join,
                SenderId = username,
                SessionId = sessionId,
                Timestamp = DateTime.UtcNow,
                Data = JsonConvert.SerializeObject(userInfo)
            };

            await SendMessageAsync(message);
        }

        private async Task SendLeaveMessageAsync()
        {
            var message = new CollaborationMessage
            {
                Type = MessageType.Leave,
                SenderId = username,
                SessionId = sessionId,
                Timestamp = DateTime.UtcNow,
                Data = "User left"
            };

            await SendMessageAsync(message);
        }

        private async Task SendHeartbeatAsync()
        {
            var message = new CollaborationMessage
            {
                Type = MessageType.Heartbeat,
                SenderId = username,
                SessionId = sessionId,
                Timestamp = DateTime.UtcNow,
                Data = "ping"
            };

            await SendMessageAsync(message);
        }

        private async Task SendMessageAsync(CollaborationMessage message)
        {
            if (networkStream == null) return;

            try
            {
                var json = JsonConvert.SerializeObject(message);
                var data = Encoding.UTF8.GetBytes(json);
                var length = BitConverter.GetBytes(data.Length);

                // Send message length first, then message data
                await networkStream.WriteAsync(length, 0, 4);
                await networkStream.WriteAsync(data, 0, data.Length);
                await networkStream.FlushAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to send message: {ex.Message}");

                if (AutoReconnect)
                {
                    _ = Task.Run(AttemptReconnectAsync);
                }
            }
        }

        private void StartListening()
        {
            listenerThread = new Thread(async () =>
            {
                try
                {
                    while (isConnected && !cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        var message = await ReceiveMessageAsync();
                        if (message != null)
                        {
                            await ProcessMessageAsync(message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnErrorOccurred($"Listening error: {ex.Message}");

                    if (AutoReconnect && isConnected)
                    {
                        _ = Task.Run(AttemptReconnectAsync);
                    }
                }
            })
            {
                IsBackground = true,
                Name = "CollaborationListener"
            };

            listenerThread.Start();
        }

        private void StartAcceptingClients()
        {
            Task.Run(async () =>
            {
                try
                {
                    while (isConnected && !cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        var client = await tcpListener.AcceptTcpClientAsync();
                        _ = Task.Run(() => HandleClientAsync(client));
                    }
                }
                catch (Exception ex)
                {
                    if (isConnected) // Only report error if we're supposed to be connected
                    {
                        OnErrorOccurred($"Error accepting clients: {ex.Message}");
                    }
                }
            });
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            var clientId = Guid.NewGuid().ToString();

            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    while (client.Connected && isConnected)
                    {
                        var message = await ReceiveMessageFromStreamAsync(stream);
                        if (message != null)
                        {
                            // Broadcast message to all other clients
                            await BroadcastMessageAsync(message, clientId);
                            await ProcessMessageAsync(message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Client handling error: {ex.Message}");
            }
            finally
            {
                // Remove user when client disconnects
                var userToRemove = connectedUsers.Values.FirstOrDefault(u => u.ConnectionId == clientId);
                if (userToRemove != null)
                {
                    connectedUsers.Remove(userToRemove.Username);
                    OnUserLeft(userToRemove);
                }
            }
        }

        private async Task<CollaborationMessage> ReceiveMessageAsync()
        {
            return await ReceiveMessageFromStreamAsync(networkStream);
        }

        private async Task<CollaborationMessage> ReceiveMessageFromStreamAsync(NetworkStream stream)
        {
            try
            {
                // Read message length
                var lengthBytes = new byte[4];
                await stream.ReadAsync(lengthBytes, 0, 4);
                var messageLength = BitConverter.ToInt32(lengthBytes, 0);

                if (messageLength <= 0 || messageLength > 1024 * 1024) // Max 1MB per message
                    return null;

                // Read message data
                var messageBytes = new byte[messageLength];
                var totalRead = 0;

                while (totalRead < messageLength)
                {
                    var read = await stream.ReadAsync(messageBytes, totalRead, messageLength - totalRead);
                    if (read == 0) break; // Connection closed
                    totalRead += read;
                }

                var json = Encoding.UTF8.GetString(messageBytes, 0, totalRead);
                return JsonConvert.DeserializeObject<CollaborationMessage>(json);
            }
            catch (Exception)
            {
                return null; // Connection lost or invalid data
            }
        }

        private async Task ProcessMessageAsync(CollaborationMessage message)
        {
            if (message == null || message.SenderId == username) return; // Don't process our own messages

            try
            {
                switch (message.Type)
                {
                    case MessageType.Join:
                        await HandleUserJoinAsync(message);
                        break;

                    case MessageType.Leave:
                        await HandleUserLeaveAsync(message);
                        break;

                    case MessageType.Stroke:
                        await HandleStrokeMessageAsync(message);
                        break;

                    case MessageType.Text:
                        OnMessageReceived(message);
                        break;

                    case MessageType.Clear:
                        OnMessageReceived(message);
                        break;

                    case MessageType.Cursor:
                        await HandleCursorMessageAsync(message);
                        break;

                    case MessageType.Heartbeat:
                        // Update user's last seen time
                        if (connectedUsers.ContainsKey(message.SenderId))
                        {
                            connectedUsers[message.SenderId].LastSeen = DateTime.UtcNow;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error processing message: {ex.Message}");
            }
        }

        private async Task HandleUserJoinAsync(CollaborationMessage message)
        {
            try
            {
                var userInfo = JsonConvert.DeserializeObject<dynamic>(message.Data);

                var user = new RemoteUser
                {
                    Username = userInfo.Username,
                    SessionId = userInfo.SessionId,
                    JoinTime = userInfo.JoinTime,
                    LastSeen = DateTime.UtcNow,
                    CursorColor = GetRandomColor(),
                    IsDrawing = false,
                    ConnectionId = message.SessionId
                };

                connectedUsers[user.Username] = user;
                OnUserJoined(user);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error handling user join: {ex.Message}");
            }
        }

        private async Task HandleUserLeaveAsync(CollaborationMessage message)
        {
            if (connectedUsers.ContainsKey(message.SenderId))
            {
                var user = connectedUsers[message.SenderId];
                connectedUsers.Remove(message.SenderId);
                OnUserLeft(user);
            }
        }

        private async Task HandleStrokeMessageAsync(CollaborationMessage message)
        {
            try
            {
                var stroke = DeserializeStroke(message.Data);
                if (stroke != null)
                {
                    stroke.UserId = message.SenderId;
                    OnRemoteStrokeReceived(stroke, message.SenderId);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error handling stroke message: {ex.Message}");
            }
        }

        private async Task HandleCursorMessageAsync(CollaborationMessage message)
        {
            try
            {
                var position = JsonConvert.DeserializeObject<dynamic>(message.Data);

                if (connectedUsers.ContainsKey(message.SenderId))
                {
                    var user = connectedUsers[message.SenderId];
                    user.LastPosition = new PointF((float)position.X, (float)position.Y);
                    user.LastSeen = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error handling cursor message: {ex.Message}");
            }
        }

        private async Task BroadcastMessageAsync(CollaborationMessage message, string excludeClientId = null)
        {
            // This would be implemented for server mode to broadcast to all connected clients
            // For now, this is a placeholder
        }

        private void StartHeartbeat()
        {
            Task.Run(async () =>
            {
                try
                {
                    while (isConnected && !cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        await Task.Delay(HeartbeatInterval, cancellationTokenSource.Token);

                        if (isConnected)
                        {
                            await SendHeartbeatAsync();

                            // Check for disconnected users
                            CheckForDisconnectedUsers();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                }
                catch (Exception ex)
                {
                    OnErrorOccurred($"Heartbeat error: {ex.Message}");
                }
            });
        }

        private void CheckForDisconnectedUsers()
        {
            var timeout = TimeSpan.FromMilliseconds(HeartbeatInterval * 3); // 3 missed heartbeats
            var disconnectedUsers = connectedUsers.Values
                .Where(u => DateTime.UtcNow - u.LastSeen > timeout)
                .ToList();

            foreach (var user in disconnectedUsers)
            {
                connectedUsers.Remove(user.Username);
                OnUserLeft(user);
            }
        }

        private async Task AttemptReconnectAsync()
        {
            if (isServer) return; // Servers don't reconnect

            int attempts = 0;

            while (attempts < MaxReconnectAttempts && !isConnected)
            {
                attempts++;

                try
                {
                    await Task.Delay(5000 * attempts); // Exponential backoff

                    OnConnectionStatusChanged(false, $"Reconnecting... (Attempt {attempts}/{MaxReconnectAttempts})");

                    if (await ConnectAsClientAsync(serverAddress, serverPort, username))
                    {
                        OnConnectionStatusChanged(true, "Reconnected successfully");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    OnErrorOccurred($"Reconnection attempt {attempts} failed: {ex.Message}");
                }
            }

            OnConnectionStatusChanged(false, "Reconnection failed - maximum attempts reached");
        }

        private string SerializeStroke(AdvancedStroke stroke)
        {
            try
            {
                var strokeData = new
                {
                    Points = stroke.Points.Select(p => new { X = p.X, Y = p.Y }).ToList(),
                    Color = stroke.Color.ToArgb(),
                    Width = stroke.Width,
                    Tool = stroke.Tool.ToString(),
                    Effect = stroke.Effect.ToString(),
                    Opacity = stroke.Opacity,
                    Timestamp = stroke.Timestamp,
                    Pressure = stroke.Pressure,
                    Tilt = stroke.Tilt,
                    LayerId = stroke.LayerId
                };

                return JsonConvert.SerializeObject(strokeData);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error serializing stroke: {ex.Message}");
                return null;
            }
        }

        private AdvancedStroke DeserializeStroke(string data)
        {
            try
            {
                var strokeData = JsonConvert.DeserializeObject<dynamic>(data);

                var stroke = new AdvancedStroke
                {
                    Color = Color.FromArgb((int)strokeData.Color),
                    Width = (float)strokeData.Width,
                    Opacity = (float)strokeData.Opacity,
                    Timestamp = (DateTime)strokeData.Timestamp,
                    Pressure = (float)strokeData.Pressure,
                    Tilt = (float)strokeData.Tilt,
                    LayerId = (int)strokeData.LayerId
                };

                // Parse tool and effect
                Enum.TryParse<DrawingTool>((string)strokeData.Tool, out var tool);
                stroke.Tool = tool;

                Enum.TryParse<SpecialEffect>((string)strokeData.Effect, out var effect);
                stroke.Effect = effect;

                // Parse points
                foreach (var point in strokeData.Points)
                {
                    stroke.Points.Add(new PointF((float)point.X, (float)point.Y));
                }

                return stroke;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error deserializing stroke: {ex.Message}");
                return null;
            }
        }

        private Color GetRandomColor()
        {
            var colors = new Color[]
            {
                Color.Red, Color.Blue, Color.Green, Color.Orange, Color.Purple,
                Color.Cyan, Color.Magenta, Color.Yellow, Color.Pink, Color.Lime
            };

            var random = new Random();
            return colors[random.Next(colors.Length)];
        }

        // Event handlers
        protected virtual void OnRemoteStrokeReceived(AdvancedStroke stroke, string userId)
        {
            RemoteStrokeReceived?.Invoke(this, new RemoteStrokeEventArgs { Stroke = stroke, UserId = userId });
        }

        protected virtual void OnUserJoined(RemoteUser user)
        {
            UserJoined?.Invoke(this, new UserEventArgs { User = user });
        }

        protected virtual void OnUserLeft(RemoteUser user)
        {
            UserLeft?.Invoke(this, new UserEventArgs { User = user });
        }

        protected virtual void OnMessageReceived(CollaborationMessage message)
        {
            MessageReceived?.Invoke(this, new MessageEventArgs { Message = message });
        }

        protected virtual void OnConnectionStatusChanged(bool connected, string message)
        {
            ConnectionStatusChanged?.Invoke(this, new ConnectionEventArgs { IsConnected = connected, Message = message });
        }

        protected virtual void OnErrorOccurred(string error)
        {
            ErrorOccurred?.Invoke(this, new ErrorEventArgs { ErrorMessage = error, Timestamp = DateTime.UtcNow });
        }

        public void Dispose()
        {
            cancellationTokenSource?.Cancel();

            networkStream?.Dispose();
            tcpClient?.Close();
            tcpListener?.Stop();

            cancellationTokenSource?.Dispose();
        }
    }

    // Supporting classes and enums
    public class RemoteUser
    {
        public string Username { get; set; }
        public string SessionId { get; set; }
        public string ConnectionId { get; set; }
        public Color CursorColor { get; set; }
        public PointF LastPosition { get; set; }
        public bool IsDrawing { get; set; }
        public DateTime JoinTime { get; set; }
        public DateTime LastSeen { get; set; }
        public Dictionary<string, object> UserProperties { get; set; } = new Dictionary<string, object>();

        public override string ToString()
        {
            return $"{Username} - {(IsOnline ? "Online" : "Offline")}";
        }

        public bool IsOnline => DateTime.UtcNow - LastSeen < TimeSpan.FromMinutes(2);
    }

    public class CollaborationMessage
    {
        public MessageType Type { get; set; }
        public string SenderId { get; set; }
        public string SessionId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Data { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public enum MessageType
    {
        Join,
        Leave,
        Stroke,
        Text,
        Clear,
        Cursor,
        Heartbeat,
        VoiceData,
        ScreenShare,
        FileTransfer,
        ProjectSync
    }

    // Event argument classes
    public class RemoteStrokeEventArgs : EventArgs
    {
        public AdvancedStroke Stroke { get; set; }
        public string UserId { get; set; }
    }

    public class UserEventArgs : EventArgs
    {
        public RemoteUser User { get; set; }
    }

    public class MessageEventArgs : EventArgs
    {
        public CollaborationMessage Message { get; set; }
    }

    public class ConnectionEventArgs : EventArgs
    {
        public bool IsConnected { get; set; }
        public string Message { get; set; }
    }

    public class ErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; }
    }
}