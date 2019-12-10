// Unity ECS very simple example with IJobForEachWithEntity, IJobForEach_BC and IBufferElementData.
 
// 2019.12.10
 
// Requires Unity 2020.1.0a15+

// Tested with
// Burst 1.2.0-preview.10
// Collections 0.2.0-Preview.13
// Entities 0.2.0-preview.18
// Jobs 0.2.1-Preview.3
// Mathematics 1.1.0.preview.1
 
using Unity.Collections ;
using Unity.Entities ;
using Unity.Jobs ;
using Unity.Burst ; // See commented out [BurstCompile] lines, above jobs.

using UnityEngine ;
 
namespace ECS.Test
{
 
    struct Instance : IComponentData
    {
        public float f ;
    }
 
    // For testing in Job_BC
    struct SomeBufferElement : IBufferElementData
    {
        public int i ;
    }

    // For testing in JobWithEntity
    struct SomeFromEntityBufferElement : IBufferElementData
    {
        public int i ;
    }
 
    public class BufferWithJobSystem : JobComponentSystem
    {
        
        // protected override void OnCreateManager ( int capacity ) // Obsolete
        protected override void OnCreate ( )
        {
            base.OnCreate ( ) ;

            Debug.LogWarning ( "Burst is disabled, to use Debug.Log in jobs." ) ;
            Debug.LogWarning ( "Jobs are executed approx every second." ) ;
 
            Instance instance = new Instance () ;
 
            Entity entity = EntityManager.CreateEntity ( typeof (Instance) ) ;
     
            EntityManager.SetComponentData ( entity, instance ) ;
            EntityManager.AddBuffer <SomeBufferElement> ( entity ) ;
  
            DynamicBuffer <SomeBufferElement> someBuffer = EntityManager.GetBuffer <SomeBufferElement> ( entity ) ;

            // Add two elements to dynamic buffer.
            SomeBufferElement someBufferElement = new SomeBufferElement () ;
            someBufferElement.i = 100000 ;
            someBuffer.Add ( someBufferElement ) ;
            someBufferElement.i = 200000 ;
            someBuffer.Add ( someBufferElement ) ;

            EntityManager.Instantiate ( entity ) ; // Clone entity.

            
            entity = EntityManager.CreateEntity ( typeof (Instance) ) ;
     
            EntityManager.SetComponentData ( entity, instance ) ;
            EntityManager.AddBuffer <SomeFromEntityBufferElement> ( entity ) ;

            DynamicBuffer <SomeFromEntityBufferElement> someFromEntityBuffer = EntityManager.GetBuffer <SomeFromEntityBufferElement> ( entity ) ;

            // Add two elements to dynamic buffer.
            SomeFromEntityBufferElement someFromEntityBufferElement = new SomeFromEntityBufferElement () ;
            someFromEntityBufferElement.i = 1000 ;
            someFromEntityBuffer.Add ( someFromEntityBufferElement ) ;
            someFromEntityBufferElement.i = 10 ;
            someFromEntityBuffer.Add ( someFromEntityBufferElement ) ;

            EntityManager.Instantiate ( entity ) ; // Clone entity.

        }
        
        float previoudTime = 0 ;

        protected override JobHandle OnUpdate ( JobHandle inputDeps )
        {
            
            float time = Time.time ;

            // Execute approx every second.
            if ( time > previoudTime + 1 )
            {
                previoudTime = time ;                
            }
            else
            {
                return inputDeps ;
            }

            JobHandle job_withEntity = new Job_WithEntity () 
            {
                time = time,
                someBuffer = GetBufferFromEntity <SomeFromEntityBufferElement> ( false ) // Read and write
         
            }.ScheduleSingle ( this, inputDeps ) ; // Using instead job.Schedule ( this, inputDeps ), for single threaded test and debug.
            // }.Schedule ( this, inputDeps ) ; // Allow execute job in parallel, if there is enough entities.
            
            JobHandle job_BC = new Job_BC () 
            {
                time = time,
                
            }.ScheduleSingle ( this, job_withEntity ) ; // Using instead job.Schedule ( this, inputDeps ), for single threaded test and debug.
            // }.Schedule ( this, jobWithEntity ) ; // Allow execute job in parallel, if there is enough entities.

            return job_BC ;
        }

        // [BurstCompile] // Disbaled burst, as Debug.Log is used.
        [RequireComponentTag ( typeof (SomeFromEntityBufferElement ) ) ]
        // struct Job: IJobProcessComponentDataWithEntity <Instance> // Obsolete
        struct Job_WithEntity : IJobForEachWithEntity <Instance>
        {
            public float time;
 
            // Allow buffer read write in parralel jobs
            // Ensure, no two jobs can write to same entity, at the same time.
            // !! "You are somehow completely certain that there is no race condition possible here, because you are absolutely certain that you will not be writing to the same Entity ID multiple times from your parallel for job. (If you do thats a race condition and you can easily crash unity, overwrite memory etc) If you are indeed certain and ready to take the risks.
            // https://forum.unity.com/threads/how-can-i-improve-or-jobify-this-system-building-a-list.547324/#post-3614833
            [NativeDisableParallelForRestriction]
            public BufferFromEntity <SomeFromEntityBufferElement> someBuffer ;
 
            public void Execute( Entity entity, int index, ref Instance tester )
            {
                tester.f = time ;
 
                DynamicBuffer <SomeFromEntityBufferElement> dynamicBuffer = someBuffer [entity] ;
 
                SomeFromEntityBufferElement bufferElement = dynamicBuffer [0] ; 
                bufferElement.i ++ ; // Increment.
                dynamicBuffer [0] = bufferElement ; // Set back.
                
                // Console will throw error when using debug and burst is enabled.
                // Comment out Debug, when using burst.
                Debug.Log ( "T: " + tester.f + " IJobForEachWIthEntity " + " #" + index + "; entity: " + entity + "; " + dynamicBuffer [0].i + "; " + dynamicBuffer [1].i ) ;
 
            }

        }
 
        // [BurstCompile] // Disbaled burst, as Debug.Log is used.
        struct Job_BC : IJobForEach_BC <SomeBufferElement, Instance>
        {
            public float time;
 
            // Allow buffer read write in parralel jobs
            // Ensure, no two jobs can write to same entity, at the same time.
            public void Execute( DynamicBuffer <SomeBufferElement> dynamicBuffer, ref Instance tester )
            {
                tester.f = time ;
 
 
                SomeBufferElement bufferElement = dynamicBuffer [0] ;
                bufferElement.i ++ ; // Increment.
                dynamicBuffer [0] = bufferElement ; // Set back.
                
                // Console will throw error when using debug and burst is enabled.
                // Comment out Debug, when using burst.
                Debug.Log ( "T: " + tester.f + " IJobForEach_BC (Buffer, Component) " + "; "  + dynamicBuffer [0].i + "; " + dynamicBuffer [1].i ) ;
 
            }

        }

    }
}
