module ExprHelpers
open System.Reflection
open FSharp.Quotations
open System
open System.Collections.Generic
open Abstractions
open FSharp.Quotations.Evaluator

let getMethodInfo (e : Expr<'T>) : MethodInfo =
  match e with
  | Patterns.Call (_, mi, _) -> mi
  | _ -> failwithf "Expression has the wrong shape, expected Call (_, _, _) instead got: %A" e

let genericMethodInfo (e : Expr<'T>) : MethodInfo = let typedInfo = getMethodInfo e
                                                    typedInfo.GetGenericMethodDefinition ()

let makeGenType types (m:Type) = m.MakeGenericType (types|>List.toArray)

let fsFuncType = typedefof<int->int>
let csFuncType = typedefof<Func<int,int>>
let kvPairType = typedefof<KeyValuePair<obj,obj>>

let keyInfo (t:Type) = t.GetProperty("Key")

let valueInfo (t:Type) = t.GetProperty("Value")

type 'info ExprContext = {
    obj: Option<Expr>
    info: 'info
    }

let Call (mi:MethodInfo) () =
    {info = mi;obj = Option.None}

let On obj ctx =
    {ctx with obj = Some(obj)}

let With args (ctx:ExprContext<MethodInfo>) = 
    match ctx.obj with
        |Some(o) -> Expr.Call(o,ctx.info,args)
        |Option.None -> Expr.Call(ctx.info,args)

let WithConst args ctx =
    let values = args |> List.map (fun v->Expr.Value(v))
    With values ctx

let MakeGen types (ctx:ExprContext<MethodInfo>) =
    let typesArr = types|>List.toArray
    match ctx.info.IsGenericMethodDefinition with
        |true ->let gDef = ctx.info.MakeGenericMethod typesArr
                {ctx with info = gDef}
        |false -> let def= ctx.info.GetGenericMethodDefinition ()
                  let gDef = def.MakeGenericMethod typesArr
                  {ctx with info  = gDef}

let SetProp (pi:PropertyInfo) () =
    {info= pi;obj=Option.None}


let To value (ctx:ExprContext<PropertyInfo>) = 
    match ctx.obj with
        |Some(o) -> Expr.PropertySet(o,ctx.info,value)
        |Option.None -> Expr.PropertySet(ctx.info,value)

let Val constant = Expr.Value constant


type 'a ExpressionContext = {
    lambdaExpr: Expr -> Expr
    bindings: Expr list
    output : Expr
    input: Expr
    defaultUT : Option<'a>
    }

let finallize (factories:Dictionary<Type,obj>) ctx = 
    let bindings = ctx.bindings |> List.reduce (fun p n -> Expr.Sequential(p,n))
    let lambda = Expr.Sequential(bindings,ctx.output) |> ctx.lambdaExpr
    let factory = lambda.CompileUntyped()
    factories.Add(ctx.output.Type,factory)
    factory |> Success