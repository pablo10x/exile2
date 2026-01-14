using LiteNetLib.Utils;
using UnityEngine;

namespace ExileSurvival.Networking.Data
{
    // --- Core Packets ---

    public struct JoinRequestPacket : INetSerializable
    {
        public string UserName;
        public string Version;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(UserName);
            writer.Put(Version);
        }

        public void Deserialize(NetDataReader reader)
        {
            UserName = reader.GetString();
            Version = reader.GetString();
        }
    }

    public struct JoinAcceptPacket : INetSerializable
    {
        public int ClientId; // Changed to int to match NetPeer.Id
        public uint ServerTick;
        public string MapName; // Added MapName so client knows what to load

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ClientId);
            writer.Put(ServerTick);
            writer.Put(MapName);
        }

        public void Deserialize(NetDataReader reader)
        {
            ClientId = reader.GetInt();
            ServerTick = reader.GetUInt();
            MapName = reader.GetString();
        }
    }

    public struct ClientReadyPacket : INetSerializable
    {
        public void Serialize(NetDataWriter writer) { }
        public void Deserialize(NetDataReader reader) { }
    }

    public struct SpawnPacket : INetSerializable
    {
        public int EntityId;
        public int OwnerId;
        public byte TypeId; // 0 = Player, 1 = Enemy, 2 = Container, etc.
        public Vector3 Position;
        public Quaternion Rotation;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(EntityId);
            writer.Put(OwnerId);
            writer.Put(TypeId);
            writer.Put(Position.x); writer.Put(Position.y); writer.Put(Position.z);
            writer.Put(Rotation.x); writer.Put(Rotation.y); writer.Put(Rotation.z); writer.Put(Rotation.w);
        }

        public void Deserialize(NetDataReader reader)
        {
            EntityId = reader.GetInt();
            OwnerId = reader.GetInt();
            TypeId = reader.GetByte();
            Position = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            Rotation = new Quaternion(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
        }
    }

    public struct EntityDestroyPacket : INetSerializable
    {
        public int EntityId;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(EntityId);
        }

        public void Deserialize(NetDataReader reader)
        {
            EntityId = reader.GetInt();
        }
    }

    // --- Movement / Prediction Packets ---

    public struct PlayerInputPacket : INetSerializable
    {
        public uint Tick;
        public float Horizontal;
        public float Vertical;
        public bool Jump;
        public bool Crouch;
        public float CameraYaw; // Optimized rotation (only yaw usually needed for movement)

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Tick);
            writer.Put(Horizontal);
            writer.Put(Vertical);
            writer.Put(Jump);
            writer.Put(Crouch);
            writer.Put(CameraYaw);
        }

        public void Deserialize(NetDataReader reader)
        {
            Tick = reader.GetUInt();
            Horizontal = reader.GetFloat();
            Vertical = reader.GetFloat();
            Jump = reader.GetBool();
            Crouch = reader.GetBool();
            CameraYaw = reader.GetFloat();
        }
    }

    public struct PlayerStatePacket : INetSerializable
    {
        public int PlayerId; // ID of the player this state belongs to
        public uint Tick; // The tick this state represents
        public Vector3 Position;
        public Vector3 Velocity;
        public Quaternion Rotation;
        public byte StateId; // Enum mapped to byte

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(PlayerId);
            writer.Put(Tick);
            writer.Put(Position.x); writer.Put(Position.y); writer.Put(Position.z);
            writer.Put(Velocity.x); writer.Put(Velocity.y); writer.Put(Velocity.z);
            writer.Put(Rotation.x); writer.Put(Rotation.y); writer.Put(Rotation.z); writer.Put(Rotation.w);
            writer.Put(StateId);
        }

        public void Deserialize(NetDataReader reader)
        {
            PlayerId = reader.GetInt();
            Tick = reader.GetUInt();
            Position = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            Velocity = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            Rotation = new Quaternion(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
            StateId = reader.GetByte();
        }
    }
}