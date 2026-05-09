#r "nuget: BroilerJS.Core,1.2.1"
#r "nuget: BroilerJS.NodePollyfill,1.1.107"
using System;
using System.Linq;
using System.Collections.Generic;
using BroilerJS.Core;
using BroilerJS.Core.Clr;
using BroilerJS.Core.Core.Storage;


[Export]
public class EventEmitter: BroilerJS.NodePollyfill.EventEmitter {

}
