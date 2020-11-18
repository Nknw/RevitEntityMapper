module ExprHelpers
open System.Reflection
open FSharp.Quotations
open System

let getMethodInfo (e : Expr<'T>) : MethodInfo =
  match e with
  | Patterns.Call (_, mi, _) -> mi
  | _ -> failwithf "Expression has the wrong shape, expected Call (_, _, _) instead got: %A" e

let genericMethodInfo (e : Expr<'T>) : MethodInfo = let typedInfo = getMethodInfo e
                                                    typedInfo.GetGenericMethodDefinition ()

let fsFuncType = typedefof<int->int>

let pipeRInfo = <@1 |> ignore @> |> genericMethodInfo

type 'info ExprContext = {
    obj: Option<Expr>
    info: 'info
    }

let Call (mi:MethodInfo) () =
    {info = mi;obj = None}

let On obj ctx =
    {ctx with obj = Some(obj)}

let With args (ctx:ExprContext<MethodInfo>) = 
    match ctx.obj with
        |Some(o) -> Expr.Call(o,ctx.info,args)
        |None -> Expr.Call(ctx.info,args)

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
    {info= pi;obj=None}


let To value (ctx:ExprContext<PropertyInfo>) = 
    match ctx.obj with
        |Some(o) -> Expr.PropertySet(o,ctx.info,value)
        |None -> Expr.PropertySet(ctx.info,value)

let Val constant = Expr.Value constant

