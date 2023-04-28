﻿using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class PhysicalObjectState : OnlineState
    {
        ChunkState[] chunkStates;
        private int collisionLayer;

        public PhysicalObjectState() { }
        public PhysicalObjectState(OnlineEntity onlineEntity)
        {
            chunkStates = onlineEntity.entity.realizedObject.bodyChunks.Select(c => new ChunkState(c)).ToArray();
            collisionLayer = onlineEntity.entity.realizedObject.collisionLayer;
        }

        public override StateType stateType => StateType.PhysicalObjectState;

        public virtual void ReadTo(OnlineEntity onlineEntity)
        {
            var po = (PhysicalObject)onlineEntity.entity.realizedObject;
            
            if (chunkStates.Length == po.bodyChunks.Length)
            {
                for (int i = 0; i < chunkStates.Length; i++)
                {
                    chunkStates[i].ReadTo(po.bodyChunks[i]);
                }
            }

            po.collisionLayer = collisionLayer;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref chunkStates);
            serializer.Serialize(ref collisionLayer);
        }
    }

    public class ChunkState // : OnlineState // no need for serializing its type, its just always the same data
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
}