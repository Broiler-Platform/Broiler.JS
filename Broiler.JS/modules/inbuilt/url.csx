#r "nuget: BroilerJS.Core,1.2.1"
using System;
using System.Linq;
using BroilerJS.Core;
using BroilerJS.Core.Clr;


[Export]
public class JSUrl {

    private Uri uri;

    public JSUrl(in Arguments a) {
        this.uri = new Uri(a.Get1().ToString());
    }

    public string Host => uri.Host;

}