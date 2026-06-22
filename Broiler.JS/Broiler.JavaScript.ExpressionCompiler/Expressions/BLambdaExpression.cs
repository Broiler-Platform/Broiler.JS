#nullable enable
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Broiler.JavaScript.ExpressionCompiler.Expressions;


public class BLambdaExpression: BExpression
{
    public readonly FunctionName Name;
    public readonly BExpression Body;
    public new readonly BParameterExpression[] Parameters;
    public readonly Type ReturnType;

    [AllowNull]
    public BParameterExpression This { get; private set; }

    internal BExpression<T> As<T>() => new(Name, Body, This, Parameters, ReturnType);


    public readonly Type[] ParameterTypes;

    public Type[] ParameterTypesWithThis {
        get {
            var l = new List<Type> { This!.Type };
            l.AddRange(ParameterTypes);
            return l.ToArray();
        }
    }

    internal readonly BExpression? Repository;
        

    public BLambdaExpression(
        Type delegateType,
        in FunctionName name, 
        BExpression body, 
        BParameterExpression? @this,
        BParameterExpression[]? parameters,
        Type? returnType = null,
        BExpression? repository = null)
        : base(BExpressionType.Lambda, delegateType)
    {
        Name = name;
        Body = body;
        This = @this;
        ReturnType = returnType ?? body.Type;
        if (parameters != null)
            Parameters = parameters;
        else
            Parameters = [];
        ParameterTypes = Parameters.Select(x => x.Type).ToArray();
        Repository = repository;
    }
    public override void Print(IndentedTextWriter writer)
    {
        writer.Write('(');
        writer.Write(string.Join(", ", Parameters.Select(p => $"{p.Type.GetFriendlyName()} {p.Name}") ));
        writer.Write(") => ");

        Body.Print(writer);
    }

    internal void SetupAsClosure()
    {
        This ??= Parameter(typeof(Closures), "this");
    }

    internal BLambdaExpression WithThis(Type type)
    {
        if (This != null)
            throw new ArgumentOutOfRangeException();
        var @this = Parameter(type, "this");

        return new BLambdaExpression(Type, Name, Body, @this, Parameters, ReturnType, Repository);
    }
}