﻿using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Celeste.Mod.Ghost.Net {
    /// <summary>
    /// Session "status" chunk sent on connection and on room change.
    /// Used when teleporting.
    /// The player can receive this with an MPlayer chunk to change the session.
    /// </summary>
    public struct GhostChunkNetMSession {

        public const string Chunk = "nMS";
        public bool IsValid;

        public bool InSession;

        public Vector2? RespawnPoint;
        public PlayerInventory Inventory;
        public HashSet<string> Flags;
        public HashSet<string> LevelFlags;
        public HashSet<EntityID> Strawberries;
        public HashSet<EntityID> DoNotLoad;
        public HashSet<EntityID> Keys;
        public List<Session.Counter> Counters;
        public string FurthestSeenLevel;
        public string StartCheckpoint;
        public string ColorGrade;
        public bool[] SummitGems;
        public bool FirstLevel;
        public bool Cassette;
        public bool HeartGem;
        public bool Dreaming;
        public bool GrabbedGolden;
        public bool HitCheckpoint;
        public float LightingAlphaAdd;
        public float BloomBaseAdd;
        public float DarkRoomAlpha;
        public Session.CoreModes CoreMode;

        public void Read(BinaryReader reader) {
            IsValid = true;

            if (!reader.ReadBoolean())
                return;

            byte bools;
            int count;

            if (reader.ReadBoolean())
                RespawnPoint = new Vector2(reader.ReadSingle(), reader.ReadSingle());

            Inventory = new PlayerInventory();
            bools = reader.ReadByte();
            Inventory.Backpack = UnpackBool(bools, 0);
            Inventory.DreamDash = UnpackBool(bools, 1);
            Inventory.NoRefills = UnpackBool(bools, 2);
            Inventory.Dashes = reader.ReadByte();

            Flags = new HashSet<string>();
            count = reader.ReadByte();
            for (int i = 0; i < count; i++)
                Flags.Add(reader.ReadNullTerminatedString());

            LevelFlags = new HashSet<string>();
            count = reader.ReadByte();
            for (int i = 0; i < count; i++)
                LevelFlags.Add(reader.ReadNullTerminatedString());

            Strawberries = new HashSet<EntityID>();
            count = reader.ReadByte();
            for (int i = 0; i < count; i++)
                Strawberries.Add(new EntityID(reader.ReadNullTerminatedString(), reader.ReadInt32()));

            DoNotLoad = new HashSet<EntityID>();
            count = reader.ReadByte();
            for (int i = 0; i < count; i++)
                DoNotLoad.Add(new EntityID(reader.ReadNullTerminatedString(), reader.ReadInt32()));

            Keys = new HashSet<EntityID>();
            count = reader.ReadByte();
            for (int i = 0; i < count; i++)
                Keys.Add(new EntityID(reader.ReadNullTerminatedString(), reader.ReadInt32()));

            Counters = new List<Session.Counter>();
            count = reader.ReadByte();
            for (int i = 0; i < count; i++)
                Counters.Add(new Session.Counter {
                    Key = reader.ReadNullTerminatedString(),
                    Value = reader.ReadInt32()
                });

            FurthestSeenLevel = reader.ReadNullTerminatedString();
            StartCheckpoint = reader.ReadNullTerminatedString();
            ColorGrade = reader.ReadNullTerminatedString();

            count = reader.ReadByte();
            SummitGems = new bool[count];
            for (int i = 0; i < count; i++) {
                if ((i % 8) == 0)
                    bools = reader.ReadByte();
                SummitGems[i] = UnpackBool(bools, i % 8);
            }

            bools = reader.ReadByte();
            FirstLevel = UnpackBool(bools, 0);
            Cassette = UnpackBool(bools, 1);
            HeartGem = UnpackBool(bools, 2);
            Dreaming = UnpackBool(bools, 3);
            GrabbedGolden = UnpackBool(bools, 4);
            HitCheckpoint = UnpackBool(bools, 5);

            LightingAlphaAdd = reader.ReadSingle();
            BloomBaseAdd = reader.ReadSingle();
            DarkRoomAlpha = reader.ReadSingle();

            CoreMode = (Session.CoreModes) reader.ReadByte();
        }

        public void Write(BinaryWriter writer) {
            if (!InSession) {
                writer.Write(false);
                return;
            }
            writer.Write(true);

            byte bools;

            if (RespawnPoint != null) {
                writer.Write(true);
                writer.Write(RespawnPoint.Value.X);
                writer.Write(RespawnPoint.Value.Y);

            } else {
                writer.Write(false);
            }

            writer.Write(PackBools(Inventory.Backpack, Inventory.DreamDash, Inventory.NoRefills));
            writer.Write((byte) Inventory.Dashes);

            writer.Write((byte) Flags.Count);
            foreach (string value in Flags)
                writer.WriteNullTerminatedString(value);

            writer.Write((byte) LevelFlags.Count);
            foreach (string value in LevelFlags)
                writer.WriteNullTerminatedString(value);

            writer.Write((byte) Strawberries.Count);
            foreach (EntityID value in Strawberries) {
                writer.WriteNullTerminatedString(value.Level);
                writer.Write(value.ID);
            }

            writer.Write((byte) DoNotLoad.Count);
            foreach (EntityID value in DoNotLoad) {
                writer.WriteNullTerminatedString(value.Level);
                writer.Write(value.ID);
            }

            writer.Write((byte) Keys.Count);
            foreach (EntityID value in Keys) {
                writer.WriteNullTerminatedString(value.Level);
                writer.Write(value.ID);
            }

            writer.Write((byte) Counters.Count);
            foreach (Session.Counter value in Counters) {
                writer.WriteNullTerminatedString(value.Key);
                writer.Write(value.Value);
            }

            writer.WriteNullTerminatedString(FurthestSeenLevel);
            writer.WriteNullTerminatedString(StartCheckpoint);
            writer.WriteNullTerminatedString(ColorGrade);

            writer.Write((byte) SummitGems.Length);
            bools = 0;
            for (int i = 0; i < SummitGems.Length; i++) {
                bools = PackBool(bools, i % 8, SummitGems[i]);
                if (((i + 1) % 8) == 0) {
                    writer.Write(bools);
                    bools = 0;
                }
            }
            if (SummitGems.Length % 8 != 0)
                writer.Write(bools);

            writer.Write(PackBools(FirstLevel, Cassette, HeartGem, Dreaming, GrabbedGolden, HitCheckpoint));

            writer.Write(LightingAlphaAdd);
            writer.Write(BloomBaseAdd);
            writer.Write(DarkRoomAlpha);

            writer.Write((byte) CoreMode);
        }

        public byte PackBool(byte value, int index, bool set) {
            int mask = 1 << index;
            return set ? (byte) (value | mask) : (byte) (value & ~mask);
        }

        public bool UnpackBool(byte value, int index) {
            int mask = 1 << index;
            return (value & mask) == mask;
        }

        public byte PackBools(bool a = false, bool b = false, bool c = false, bool d = false, bool e = false, bool f = false, bool g = false, bool h = false) {
            byte value = 0;
            value = PackBool(value, 0, a);
            value = PackBool(value, 1, b);
            value = PackBool(value, 2, c);
            value = PackBool(value, 3, d);
            value = PackBool(value, 4, e);
            value = PackBool(value, 5, f);
            value = PackBool(value, 6, g);
            value = PackBool(value, 7, h);
            return value;
        }

    }
}
