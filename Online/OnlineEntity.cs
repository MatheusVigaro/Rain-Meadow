﻿using System;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    // Welcome to polymorphism hell
    public class OnlineEntity
    {
        public AbstractPhysicalObject entity;
        public OnlinePlayer owner;
        public int id;
        public WorldCoordinate initialPos;
        internal RoomSession lastInRoom;
        internal int ticksSinceSeen;

        public OnlineEntity(AbstractPhysicalObject entity, OnlinePlayer owner, int id, WorldCoordinate pos, RoomSession lastInRoom)
        {
            this.entity = entity;
            this.owner = owner;
            this.id = id;
            this.initialPos = pos;
            this.lastInRoom = lastInRoom;
        }

        public override string ToString()
        {
            return $"{entity}:{id} from {owner}";
        }

        internal EntityState GetState(ulong ts)
        {
            if(entity is AbstractCreature)
            {
                return new CreatureEntityState(this, ts);
            }
            return new PhysicalObjectEntityState(this, ts);
        }

        internal void ReadState(EntityState entityState, ulong tick)
        {
            // todo easing??
            entityState.ReadTo(this);
        }

        public abstract class EntityState : OnlineState
        {
            public OnlineEntity onlineEntity;

            protected EntityState() : base () { }
            protected EntityState(OnlineEntity onlineEntity, ulong ts) : base(ts)
            {
                this.onlineEntity = onlineEntity;
            }

            public override void CustomSerialize(Serializer serializer)
            {
                base.CustomSerialize(serializer);
                serializer.Serialize(ref onlineEntity);
            }

            public abstract void ReadTo(OnlineEntity onlineEntity);
        }

        internal class PhysicalObjectEntityState : EntityState
        {
            public WorldCoordinate pos;
            public OnlineState realizedState;

            public PhysicalObjectEntityState() : base() { }
            public PhysicalObjectEntityState(OnlineEntity onlineEntity, ulong ts) : base(onlineEntity, ts)
            {
                if(onlineEntity != null)
                {
                    this.pos = onlineEntity.entity.pos;
                    this.realizedState = GetRealizedState();
                }
            }

            protected virtual RealizedObjectState GetRealizedState()
            {
                if (onlineEntity.entity.realizedObject == null) return null;
                return new RealizedObjectState(onlineEntity);
            }

            public override StateType stateType => StateType.PhysicalObjectEntityState;

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                onlineEntity.entity.pos = pos;
                (realizedState as RealizedObjectState)?.ReadTo(onlineEntity);
            }

            public override void CustomSerialize(Serializer serializer)
            {
                base.CustomSerialize(serializer);
                serializer.SerializeNoStrings(ref pos);
                serializer.SerializeNullable(ref realizedState);
            }
        }

        internal class CreatureEntityState : PhysicalObjectEntityState
        {
            // what do I even put here for AbstractCreature? inDen?
            public CreatureEntityState() : base() { }
            public CreatureEntityState(OnlineEntity onlineEntity, ulong ts) : base(onlineEntity, ts)
            {
                
            }

            protected override RealizedObjectState GetRealizedState()
            {
                if (onlineEntity.entity.realizedObject == null) return null;
                if (onlineEntity.entity.realizedObject is Player) return new RealizedPlayerState(onlineEntity);
                if (onlineEntity.entity.realizedObject is Creature) return new RealizedCreatureState(onlineEntity);
                return base.GetRealizedState();
            }

            public override StateType stateType => StateType.CreatureEntityState;
        }

        internal class RealizedObjectState : OnlineState
        {
            ChunkState[] chunkStates;
            public RealizedObjectState(OnlineEntity onlineEntity)
            {
                if(onlineEntity!=null)
                {
                    chunkStates = onlineEntity.entity.realizedObject.bodyChunks.Select(c=>new ChunkState(c)).ToArray();
                }
            }

            public override StateType stateType => StateType.RealizedObjectState;

            internal virtual void ReadTo(OnlineEntity onlineEntity)
            {
                if(onlineEntity.entity.realizedObject is PhysicalObject po)
                {
                    if (chunkStates.Length == po.bodyChunks.Length)
                    {
                        for (int i = 0; i < chunkStates.Length; i++)
                        {
                            chunkStates[i].ReadTo(po.bodyChunks[i]);
                        }
                    }
                }
            }

            public override void CustomSerialize(Serializer serializer)
            {
                base.CustomSerialize(serializer);
                serializer.Serialize(ref chunkStates);
            }
        }

        public class ChunkState// : OnlineState
        {
            private Vector2 pos;
            private Vector2 vel;

            public ChunkState(BodyChunk c)
            {
                if (c != null)
                {
                    pos = c.pos;
                    vel = c.vel;
                }
            }

            public void CustomSerialize(Serializer serializer)
            {
                serializer.Serialize(ref pos);
                serializer.Serialize(ref vel);
            }

            public void ReadTo(BodyChunk c)
            {
                c.pos = pos;
                c.vel = vel;
            }
        }

        internal class RealizedCreatureState : RealizedObjectState
        {
            public RealizedCreatureState(OnlineEntity onlineEntity) : base(onlineEntity)
            {
            }
            public override StateType stateType => StateType.RealizedCreatureState;
        }

        internal class RealizedPlayerState : RealizedCreatureState
        {
            public RealizedPlayerState(OnlineEntity onlineEntity) : base(onlineEntity)
            {
            }
            public override StateType stateType => StateType.RealizedPlayerState;
        }
    }
}