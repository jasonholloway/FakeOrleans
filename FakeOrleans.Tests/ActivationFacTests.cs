using FakeOrleans;
using FakeOrleans.Grains;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockOrleans.Tests
{

    public class ActivationFac
    {
        readonly TaskScheduler _outerScheduler;
        readonly RequestRunner _outerRunner;
        readonly ExceptionSink _exceptions;
        readonly GrainFac _grainFac;

        public ActivationFac(TaskScheduler outerScheduler, RequestRunner outerRunner, ExceptionSink exceptions, GrainFac grainFac) {
            _outerScheduler = outerScheduler;
            _exceptions = exceptions;
            _grainFac = grainFac;
        }


        public IActivation Create(GrainPlacement placement) 
        {
            //create activation-specific resources
            //and wire them up here

            var scheduler = new GrainTaskScheduler(_outerScheduler, _exceptions);
            var runner = new RequestRunner(scheduler, _exceptions, _outerRunner);
            
            var timer = new MockTimerRegistry(null);
            var streamReceivers = new StreamReceiverRegistry(null);
            
            //grainruntime too - but this is to be used by grainfac

            var act = new Activation(_grainFac, runner);
            act.Init(placement);

            return act;
        }


    }




    [TestFixture]
    public class ActivationFacTests
    {



    }
}
