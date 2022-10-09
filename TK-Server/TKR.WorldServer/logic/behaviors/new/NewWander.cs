﻿using System;
using TKR.WorldServer.core.miscfile.thread;
using TKR.WorldServer.core.objects;

namespace TKR.WorldServer.logic.behaviors
{
    public sealed class NewWander : Behavior
    {
        private readonly float Speed;

        public NewWander() => Speed = 0.4f;
        public NewWander(float speed) => Speed = speed;
        public NewWander(double speed) => Speed = (float)speed;

        protected override bool TickCoreOrdered(Entity host, TickTime time, ref object state)
        {
            var targetX = host.X + (Random.NextDouble() > 0.5 ? -Speed : Speed);
            var targetY = host.Y + (Random.NextDouble() > 0.5 ? -Speed : Speed);
            _ = host.MoveToward(targetX, targetY, Speed * time.BehaviourTickTime);
            return true;
        }
    }
}
