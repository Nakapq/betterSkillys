﻿using wServer.core;
using wServer.core.objects;
using wServer.networking.packets.outgoing;


namespace wServer.logic.behaviors
{
    class ChangeMusicOnDeath : Behavior
    {
        private readonly string _music;

        public ChangeMusicOnDeath(string file)
        {
            _music = file;
        }

        protected internal override void Resolve(State parent)
        {
            parent.Death += (sender, e) =>
            {
                if (e.Host.World.Music != _music)
                {
                    var owner = e.Host.World;

                    owner.Music = _music;

                    var i = 0;
                    foreach (var plr in owner.Players.Values)
                    {
                        owner.Timers.Add(new WorldTimer(100 * i, (w, t) =>
                        {
                            if (plr == null)
                                return;

                            plr.Client.SendPacket(new SwitchMusic()
                            {
                                Music = _music
                            });
                        }));
                        i++;
                    }
                }
            };
        }
        protected override void TickCore(Entity host, TickTime time, ref object state)
        { }
    }
}
