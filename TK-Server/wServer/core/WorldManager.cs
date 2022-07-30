﻿using common;
using common.resources;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using wServer.core.worlds;
using wServer.core.worlds.logic;

namespace wServer.core
{
    public sealed class WorldManager
    {
        public CoreServerManager CoreServerManager;

        private int NextWorldId = 0;

        public TickThreadSingle NexusThread { get; private set; }
        public List<TickThreadSingle> RealmThreads { get; private set; }

        public NexusWorld Nexus => (Worlds[-2] as NexusWorld);

        private readonly Dictionary<int, World> Worlds = new Dictionary<int, World>();
        private readonly Dictionary<int, VaultWorld> Vaults = new Dictionary<int, VaultWorld>();
        private readonly Dictionary<int, World> Guilds = new Dictionary<int, World>();
        private readonly Dictionary<int, int> WorldToGuildId = new Dictionary<int, int>();

        public IEnumerable<World> GetWorlds() => Worlds.Values;
        public IEnumerable<VaultWorld> GetVaultInstances() => Vaults.Values;
        public IEnumerable<World> GetGuildInstances() => Guilds.Values;

        public void WorldsBroadcastAsParallel(Action<World> action)
        {
            var worlds = GetWorlds();
            worlds.AsParallel().Select(_ =>
            {
                action.Invoke(_);
                return _;
            }).ToArray();
        }

        public WorldManager(CoreServerManager coreServerManager)
        {
            CoreServerManager = coreServerManager;
            NexusThread = new TickThreadSingle(this);
            RealmThreads = new List<TickThreadSingle>();
        }

        public void Initialize()
        {
            var nexus = CreateNewWorld("Nexus", -2, null);
            NexusThread.Attach(nexus);

            _ = CreateNewRealm();

            //(nexus as Nexus).RealmManager.CreateNewRealm();

            // add portal monitor to nexus and initialize worlds
        }

        public World CreateNewRealm()
        {
            Console.WriteLine("CreateNewRealm()");
            var worldResource = CoreServerManager.Resources.GameData.GetWorld("Realm of the Mad God");
            if (worldResource == null)
                return null;

            var nextId = Interlocked.Increment(ref NextWorldId);

            var world = new RealmWorld(nextId, worldResource);
            world.Manager = CoreServerManager; // todo add to ctor
            var success = world.LoadMapFromData(worldResource);
            if (!success)
                return null;
            world.Init();
            Worlds.Add(world.Id, world);

            Nexus.PortalMonitor.AddPortal(world.Id);
            
            var thread = new TickThreadSingle(this);
            thread.Attach(world);
            RealmThreads.Add(thread);
            return world;
        }

        public World CreateNewWorld(string dungeonName, int? id = null, World parent = null)
        {
            Console.WriteLine($"CreateNewWorld({dungeonName}, {id ?? null}, {(parent == null ? "null" : parent.IdName)}");
            var worldResource = CoreServerManager.Resources.GameData.GetWorld(dungeonName);
            if (worldResource == null)
                return null;
            
            var nextId = id ?? Interlocked.Increment(ref NextWorldId);

            World world;
            switch (worldResource.Instance)
            {
                case WorldResourceInstanceType.Nexus:
                    world = new NexusWorld(nextId, worldResource);
                    break;
                case WorldResourceInstanceType.Vault:
                    world = new VaultWorld(nextId, worldResource);
                    break;
                default:
                    world = new World(nextId, worldResource);
                    break; 
            }

            world.Manager = CoreServerManager; // todo add to ctor
            var success = world.LoadMapFromData(worldResource);
            if (!success)
                return null;
            world.Init();
            Worlds.Add(world.Id, world);
            parent?.WorldBranch.AddBranch(world);
            return world;
        }

        public void AddVaultInstance(int accountId, VaultWorld world)
        {
            Vaults.Add(accountId, world);
        }

        public void AddGuildInstance(int guildId, World world)
        {
            Guilds.Add(guildId, world);
            WorldToGuildId.Add(world.Id, guildId);
        }

        public World GetWorld(int id) => Worlds.TryGetValue(id, out World ret) ? ret : null;
        public VaultWorld GetVault(int accountId) => Vaults.TryGetValue(accountId, out var ret) ? ret : null;
        public World GetGuild(int guildId) => Guilds.TryGetValue(guildId, out var ret) ? ret : null;
        public int GetGuildId(int gameId) => WorldToGuildId.TryGetValue(gameId, out var ret) ? ret : -1;

        public bool RemoveWorld(World world)
        {
            if (Worlds.Remove(world.Id))
            {
                switch (world.InstanceType)
                {
                    case WorldResourceInstanceType.Vault:
                        _ = Vaults.Remove((world as VaultWorld).AccountId);
                        break;
                    case WorldResourceInstanceType.Guild:
                        var guildId = GetGuildId(world.Id);
                        _ = Guilds.Remove(guildId);
                        break;
                }
                return true;
            }
            return false;
        }

        public void Shutdown()
        {
            foreach (var realm in RealmThreads)
                realm.Stop();
            NexusThread.Stop();
        }
    }
}
