// Unity ECS very simple example with IJobProcessComponentDataWithEntity and IBufferElementData.
 
// Requires official Unity samples
// https://github.com/Unity-Technologies/EntityComponentSystemSamples
// Or own bootstrap.
// Just copy this script to the project
// Based on SimpleRotation / RotationSpeedSystem.cs
 
// 2018.11.16
 
// Tested with
// Entities 0.0.12-preview 12
// Burst 0.2.4-preview.37
// IncrementalCompiler 0.0.42-preview.24
// Jobs 0.0.7-Preview.5
// Mathematics 0.0.12.preview.19
 
// Entities 0.0.12-preview 20 will require replacement of 
// protected override void OnCreateManager ( int capacity )
// to 
// protected override void OnCreateManager ( )

using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
// using Unity.Mathematics;
// using Unity.Transforms;
using UnityEngine;
 
namespace ECS.Test
{
 
    struct Instance : IComponentData
    {
        public float f ;
        // public DynamicBuffer <int> db_a ;
    }
 
    struct SomeBufferElement : IBufferElementData
    {
        public int i ;
    }
 
    public class BufferWithJobSystem : JobComponentSystem
    {
        [BurstCompile]
        [RequireComponentTag ( typeof (SomeBufferElement ) ) ]
        struct Job: IJobProcessComponentDataWithEntity <Instance>
        {
            public float dt;
 
            // Allow buffer read write in parralel jobs
            // Ensure, no two jobs can write to same entity, at the same time.
            // !! "You are somehow completely certain that there is no race condition possible here, because you are absolutely certain that you will not be writing to the same Entity ID multiple times from your parallel for job. (If you do thats a race condition and you can easily crash unity, overwrite memory etc) If you are indeed certain and ready to take the risks.
            // https://forum.unity.com/threads/how-can-i-improve-or-jobify-this-system-building-a-list.547324/#post-3614833
            [NativeDisableParallelForRestriction]
            public BufferFromEntity <SomeBufferElement> someBufferElement ;
 
            public void Execute( Entity entity, int index, ref Instance tester )
            {
                tester.f = 10 * dt ;
 
                DynamicBuffer <SomeBufferElement> someDynamicBuffer = someBufferElement [entity] ;
 
                SomeBufferElement buffer = someDynamicBuffer [0] ;
 
                // Uncomment as needed
                // buffer.i = 99 ;
 
                // someDynamicBuffer [0] = buffer ;
 
                // Debug Will throw errors in Job system
                // Debug.Log ( "#" + index + "; " + someDynamicBuffer [0].i + "; " + someDynamicBuffer [1].i ) ;
 
            }
        }
 
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new Job () {
                dt = Time.deltaTime,
                someBufferElement = GetBufferFromEntity <SomeBufferElement> (false)
         
                } ;
            return job.Schedule(this, inputDeps) ;
        }
 
        // protected override void OnCreateManager ( ) // for Entities 0.0.12 preview 20
        protected override void OnCreateManager ( int capacity )
        {
            base.OnCreateManager ( capacity );
 
            Instance instance = new Instance () ;
 
            Entity entity = EntityManager.CreateEntity ( typeof (Instance) ) ;
     
            EntityManager.SetComponentData ( entity, instance ) ;
            EntityManager.AddBuffer <SomeBufferElement> ( entity ) ;
 
            var bufferFromEntity = EntityManager.GetBufferFromEntity <SomeBufferElement> ();
            var buffer = bufferFromEntity [entity];
 
            SomeBufferElement someBufferElement = new SomeBufferElement () ;
            someBufferElement.i = 6 ;
            buffer.Add ( someBufferElement ) ;
            someBufferElement.i = 7 ;
            buffer.Add ( someBufferElement ) ;
        }
    }
}
